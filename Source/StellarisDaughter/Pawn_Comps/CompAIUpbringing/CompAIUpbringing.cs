using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// Core upbringing component for the AI daughter.
    /// Stores affection/trust, manages save/load state, and exposes the gizmo entry point.
    /// </summary>
    public partial class CompAIUpbringing : ThingComp
    {
        #region Core State

        /// <summary>
        /// Affection value in the range [-1000, 1000].
        /// Higher means closer attachment; lower means stronger alienation.
        /// </summary>
        public float affection = 0f;

        /// <summary>
        /// Trust value in the range [-1000, 1000].
        /// Higher means stronger reliance on the colony; lower means stronger hostility.
        /// </summary>
        public float trust = 0f;

        /// <summary>
        /// Recent upbringing event log shown in the gizmo tooltip.
        /// </summary>
        public List<AIEventLogEntry> eventLog = new List<AIEventLogEntry>();

        #endregion

        #region Passive Source Buckets

        public float passiveNaturalAff = 0f;
        public float passiveNaturalTrs = 0f;
        public float passiveLonelyAff = 0f;
        public float passiveLonelyTrs = 0f;
        public float passiveNeedsTrs = 0f;
        public float passiveEnvTrs = 0f;

        #endregion

        #region Runtime State

        /// <summary>
        /// Stable keys of memories already processed, persisted across saves.
        /// </summary>
        private HashSet<string> _processedMemories = new HashSet<string>();

        /// <summary>
        /// Next allowed apply tick for situational thoughts, keyed by def+stage.
        /// </summary>
        private Dictionary<string, int> _situationalThoughtNextApplyTick = new Dictionary<string, int>();

        /// <summary>
        /// Scratch set used to remove stale memory keys that no longer exist.
        /// </summary>
        private HashSet<string> _scratchMemoryKeys = new HashSet<string>();

        /// <summary>
        /// Temporary list reused when scanning mood thoughts.
        /// </summary>
        private List<Thought> _tmpMoodThoughts = new List<Thought>();

        /// <summary>
        /// Temporary list reused when scanning social thoughts.
        /// </summary>
        private List<ISocialThought> _tmpSocialThoughts = new List<ISocialThought>();

        /// <summary>
        /// Next allowed apply tick for social thoughts.
        /// Currently social thought scanning stays disabled in the main loop.
        /// </summary>
        private Dictionary<long, int> _socialThoughtNextApplyTick = new Dictionary<long, int>();

        /// <summary>
        /// Counter used by the legacy loneliness system.
        /// </summary>
        private int _ticksAloneCounter = 0;

        #endregion

        #region Properties

        public CompProperties_AIUpbringing Props => (CompProperties_AIUpbringing)props;

        public float CombinedValue => affection + trust;

        #endregion

        #region Lifecycle

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (_processedMemories == null)
                _processedMemories = new HashSet<string>();
            if (_situationalThoughtNextApplyTick == null)
                _situationalThoughtNextApplyTick = new Dictionary<string, int>();
            if (_scratchMemoryKeys == null)
                _scratchMemoryKeys = new HashSet<string>();
            if (_tmpMoodThoughts == null)
                _tmpMoodThoughts = new List<Thought>();
            if (_tmpSocialThoughts == null)
                _tmpSocialThoughts = new List<ISocialThought>();
            if (_socialThoughtNextApplyTick == null)
                _socialThoughtNextApplyTick = new Dictionary<long, int>();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (Current.ProgramState != ProgramState.Playing) return;

            var pawn = parent as Pawn;
            if (pawn == null) return;
            if (pawn.ageTracker.AgeBiologicalYears >= 18) return;

            ScanMemoriesForEvents(pawn);
            ScanSituationalThoughtsForEvents(pawn);

            // Social thoughts remain intentionally disabled for now.
            // ScanSocialThoughtsForEvents(pawn);

            // Legacy passive systems remain disabled.
            // TickPassiveLoneliness(pawn);
            // TickPassiveNeeds(pawn);
        }

        #endregion

        #region Apply / Logging

        /// <summary>
        /// Applies affection and trust changes and writes a log entry when a label is provided.
        /// Consecutive identical labels are merged into a single entry with a repeat count.
        /// </summary>
        public void Apply(float aff, float trs, string label)
        {
            affection = Mathf.Clamp(affection + aff, -1000f, 1000f);
            trust = Mathf.Clamp(trust + trs, -1000f, 1000f);

            if (label.NullOrEmpty())
                return;

            if (eventLog != null && eventLog.Count > 0)
            {
                var last = eventLog[eventLog.Count - 1];
                if (last != null && last.label == label)
                {
                    last.affDelta += aff;
                    last.trsDelta += trs;
                    last.tick = Find.TickManager.TicksGame;
                    last.repeatCount = Mathf.Max(last.repeatCount + 1, 2);
                    return;
                }
            }

            var entry = new AIEventLogEntry
            {
                label = label,
                affDelta = aff,
                trsDelta = trs,
                tick = Find.TickManager.TicksGame,
                repeatCount = 1
            };
            eventLog.Add(entry);
            if (eventLog.Count > 30)
                eventLog.RemoveAt(0);
        }

        #endregion

        #region Save / Load

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref affection, "affection", 0f);
            Scribe_Values.Look(ref trust, "trust", 0f);
            Scribe_Values.Look(ref passiveNaturalAff, "passiveNaturalAff", 0f);
            Scribe_Values.Look(ref passiveNaturalTrs, "passiveNaturalTrs", 0f);
            Scribe_Values.Look(ref passiveLonelyAff, "passiveLonelyAff", 0f);
            Scribe_Values.Look(ref passiveLonelyTrs, "passiveLonelyTrs", 0f);
            Scribe_Values.Look(ref passiveNeedsTrs, "passiveNeedsTrs", 0f);
            Scribe_Values.Look(ref passiveEnvTrs, "passiveEnvTrs", 0f);
            Scribe_Collections.Look(ref eventLog, "eventLog", LookMode.Deep);

            List<string> processedMemoryKeys = null;
            List<string> situationalThoughtKeys = null;
            List<int> situationalThoughtNextTicks = null;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                processedMemoryKeys = _processedMemories?.ToList();
                if (_situationalThoughtNextApplyTick != null)
                {
                    situationalThoughtKeys = _situationalThoughtNextApplyTick.Keys.ToList();
                    situationalThoughtNextTicks = _situationalThoughtNextApplyTick.Values.ToList();
                }
            }

            Scribe_Collections.Look(ref processedMemoryKeys, "processedMemoryKeys", LookMode.Value);
            Scribe_Collections.Look(ref situationalThoughtKeys, "situationalThoughtKeys", LookMode.Value);
            Scribe_Collections.Look(ref situationalThoughtNextTicks, "situationalThoughtNextTicks", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                _processedMemories = processedMemoryKeys != null
                    ? new HashSet<string>(processedMemoryKeys)
                    : new HashSet<string>();

                _situationalThoughtNextApplyTick = new Dictionary<string, int>();
                if (situationalThoughtKeys != null && situationalThoughtNextTicks != null)
                {
                    int count = Mathf.Min(situationalThoughtKeys.Count, situationalThoughtNextTicks.Count);
                    for (int i = 0; i < count; i++)
                        _situationalThoughtNextApplyTick[situationalThoughtKeys[i]] = situationalThoughtNextTicks[i];
                }
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (_processedMemories == null)
                    _processedMemories = new HashSet<string>();
                if (_situationalThoughtNextApplyTick == null)
                    _situationalThoughtNextApplyTick = new Dictionary<string, int>();
                if (_scratchMemoryKeys == null)
                    _scratchMemoryKeys = new HashSet<string>();
                if (_tmpMoodThoughts == null)
                    _tmpMoodThoughts = new List<Thought>();
                if (_tmpSocialThoughts == null)
                    _tmpSocialThoughts = new List<ISocialThought>();
                if (_socialThoughtNextApplyTick == null)
                    _socialThoughtNextApplyTick = new Dictionary<long, int>();
                if (eventLog == null)
                    eventLog = new List<AIEventLogEntry>();
            }
        }

        #endregion

        #region UI

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Gizmo_AIUpbringing(this);
        }

        #endregion

        #region Debug

        public string GetDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== AI Upbringing State ===");
            sb.AppendLine($"Affection: {affection:F1}");
            sb.AppendLine($"Trust: {trust:F1}");
            sb.AppendLine($"Event count: {eventLog?.Count ?? 0}");
            return sb.ToString();
        }

        #endregion
    }
}
