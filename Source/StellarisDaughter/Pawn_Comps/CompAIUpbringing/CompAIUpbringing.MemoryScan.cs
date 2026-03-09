using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public partial class CompAIUpbringing
    {
        private const int SituationalThoughtEventCooldownTicks = 60000;
        private const int SocialThoughtEventCooldownTicks = 30000;

        private void ScanMemoriesForEvents(Pawn ai)
        {
            var memories = ai.needs?.mood?.thoughts?.memories?.Memories;
            if (memories == null) return;

            if (_scratchMemoryKeys == null)
                _scratchMemoryKeys = new HashSet<string>();

            _scratchMemoryKeys.Clear();

            foreach (var memory in memories)
            {
                if (memory?.def == null) continue;

                string key = GetStableMemoryKey(memory);
                _scratchMemoryKeys.Add(key);
                if (_processedMemories.Contains(key)) continue;
                _processedMemories.Add(key);

                if (TryBuildThoughtEvent(memory, out float affDelta, out float trustDelta, out string label))
                    Apply(affDelta, trustDelta, label);
            }

            if (_processedMemories.Count > 0)
                _processedMemories.RemoveWhere(key => !_scratchMemoryKeys.Contains(key));
        }

        private void ScanSituationalThoughtsForEvents(Pawn ai)
        {
            var thoughtHandler = ai.needs?.mood?.thoughts;
            if (thoughtHandler == null) return;

            if (_tmpMoodThoughts == null)
                _tmpMoodThoughts = new List<Thought>();
            if (_situationalThoughtNextApplyTick == null)
                _situationalThoughtNextApplyTick = new Dictionary<string, int>();

            _tmpMoodThoughts.Clear();
            thoughtHandler.GetAllMoodThoughts(_tmpMoodThoughts);
            int now = Find.TickManager.TicksGame;

            foreach (var thought in _tmpMoodThoughts)
            {
                if (!(thought is Thought_Situational situational)) continue;
                if (situational.def == null || !situational.Active) continue;

                string key = GetSituationalThoughtKey(situational);
                if (_situationalThoughtNextApplyTick.TryGetValue(key, out int nextTick) && now < nextTick)
                    continue;

                if (TryBuildThoughtEvent(situational, out float affDelta, out float trustDelta, out string label))
                {
                    Apply(affDelta, trustDelta, label);
                    _situationalThoughtNextApplyTick[key] = now + SituationalThoughtEventCooldownTicks;
                }
            }

            if (_situationalThoughtNextApplyTick.Count > 0)
            {
                var expiredKeys = new List<string>();
                foreach (var kv in _situationalThoughtNextApplyTick)
                {
                    if (now >= kv.Value)
                        expiredKeys.Add(kv.Key);
                }

                for (int i = 0; i < expiredKeys.Count; i++)
                    _situationalThoughtNextApplyTick.Remove(expiredKeys[i]);
            }
        }

        private bool TryBuildThoughtEvent(Thought thought, out float affDelta, out float trustDelta, out string label)
        {
            affDelta = 0f;
            trustDelta = 0f;
            label = null;

            if (thought?.def == null)
                return false;

            float mood = thought.MoodOffset();
            if (Mathf.Approximately(mood, 0f))
                return false;

            affDelta = Mathf.Clamp(mood * Props.moodToAffectionFactor, -Props.perThoughtDeltaAbsCap, Props.perThoughtDeltaAbsCap);
            trustDelta = Mathf.Clamp(mood * Props.moodToTrustFactor, -Props.perThoughtDeltaAbsCap, Props.perThoughtDeltaAbsCap);

            if (Mathf.Approximately(affDelta, 0f) && Mathf.Approximately(trustDelta, 0f))
                return false;

            label = thought.LabelCap;
            if (label.NullOrEmpty())
                label = thought.def.defName;

            return true;
        }

        private string GetStableMemoryKey(Thought_Memory memory)
        {
            int otherPawnId = memory.otherPawn?.thingIDNumber ?? -1;
            return $"{memory.def.shortHash}:{memory.CurStageIndex}:{otherPawnId}:{memory.moodOffset}:{memory.durationTicksOverride}:{(memory.permanent ? 1 : 0)}";
        }

        private string GetSituationalThoughtKey(Thought_Situational thought)
        {
            return $"{thought.def.shortHash}:{thought.CurStageIndex}";
        }

        private void ScanSocialThoughtsForEvents(Pawn ai)
        {
            var mapPawns = ai.Map?.mapPawns?.AllPawnsSpawned;
            if (mapPawns == null) return;
            if (_socialThoughtNextApplyTick == null)
                _socialThoughtNextApplyTick = new Dictionary<long, int>();

            int now = Find.TickManager.TicksGame;
            var socialDefs = ThoughtUtility.situationalSocialThoughtDefs;
            if (socialDefs == null || socialDefs.Count == 0) return;

            foreach (var other in mapPawns)
            {
                if (other == null || other == ai) continue;
                if (other.Destroyed) continue;
                if (!other.Spawned) continue;
                if (other.Dead) continue;
                if (!other.RaceProps.Humanlike) continue;
                if (other.story == null || other.story.traits == null) continue;
                if (other.relations == null) continue;

                for (int i = 0; i < socialDefs.Count; i++)
                {
                    var def = socialDefs[i];
                    if (def == null) continue;
                    if (def.IsMemory) continue;

                    var thought = TryCreateSocialSituationalThoughtSafe(ai, other, def);
                    if (thought == null || thought.def == null || !thought.Active) continue;

                    int stage = thought.CurStageIndex + 1;
                    int otherId = other.thingIDNumber;
                    long key = ((long)thought.def.shortHash << 40)
                             | (((long)stage & 0xFFL) << 32)
                             | (uint)otherId;

                    if (_socialThoughtNextApplyTick.TryGetValue(key, out int nextTick) && now < nextTick)
                        continue;

                    if (!TryBuildThoughtEvent(thought, out float affDelta, out float trustDelta, out string label))
                        continue;

                    Apply(affDelta, trustDelta, label);
                    _socialThoughtNextApplyTick[key] = now + SocialThoughtEventCooldownTicks;
                }
            }
        }

        private Thought_SituationalSocial TryCreateSocialSituationalThoughtSafe(Pawn ai, Pawn other, ThoughtDef def)
        {
            try
            {
                if (!ThoughtUtility.CanGetThought(ai, def))
                    return null;

                var state = def.Worker.CurrentSocialState(ai, other);
                if (!state.ActiveFor(def))
                    return null;

                if (!def.socialTargetDevelopmentalStageFilter.HasAny(other.DevelopmentalStage))
                    return null;
                if (def.ignoreSubhumans && other.IsSubhuman)
                    return null;

                if (!(ThoughtMaker.MakeThought(def) is Thought_SituationalSocial thought))
                    return null;

                thought.pawn = ai;
                thought.otherPawn = other;

                if (def.Worker is ThoughtWorker_Precept_Social && ai.Ideo != null)
                    thought.sourcePrecept = ai.Ideo.GetFirstPreceptAllowingSituationalThought(def);

                thought.RecalculateState();
                return thought.Active ? thought : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
