using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// CompAIUpbringing：记忆扫描模块
    /// 每 TickRare 扫描原版 Thought_Memory，匹配 AIEventResponseDef，触发 Apply。
    /// </summary>
    public partial class CompAIUpbringing
    {
        private const int SocialThoughtEventCooldownTicks = 30000; // 0.5 in-game day

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

                var resp = DefDatabase<AIEventResponseDef>.GetNamedSilentFail(memory.def.defName);
                if (resp != null)
                    Apply(resp.affDelta, resp.trustDelta, resp.eventLabel);
            }

            if (_processedMemories.Count > 0)
                _processedMemories.RemoveWhere(key => !_scratchMemoryKeys.Contains(key));
        }

        private void ScanSituationalThoughtsForEvents(Pawn ai)
        {
            var thoughtHandler = ai.needs?.mood?.thoughts;
            if (thoughtHandler == null) return;

            if (_tmpMoodThoughts == null)
                _tmpMoodThoughts = new System.Collections.Generic.List<Thought>();

            if (_processedSituationalThoughts == null)
                _processedSituationalThoughts = new HashSet<string>();

            _tmpMoodThoughts.Clear();
            thoughtHandler.GetAllMoodThoughts(_tmpMoodThoughts);

            foreach (var thought in _tmpMoodThoughts)
            {
                if (!(thought is Thought_Situational situational)) continue;
                if (situational.def == null || !situational.Active) continue;

                string key = GetSituationalThoughtKey(situational);
                if (_processedSituationalThoughts.Contains(key))
                    continue;

                var resp = DefDatabase<AIEventResponseDef>.GetNamedSilentFail(situational.def.defName);
                if (resp != null)
                {
                    Apply(resp.affDelta, resp.trustDelta, resp.eventLabel);
                    _processedSituationalThoughts.Add(key);
                }
            }
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
                // 原版部分 social thought worker（如 ThoughtWorker_Joyous）直接访问 other.story.traits。
                // 这里先做强过滤，再走我们自己的安全构造逻辑，避免调用 GetSocialThoughts 触发原版批量重算刷红字。
                if (!other.RaceProps.Humanlike) continue;
                if (other.story == null || other.story.traits == null) continue;
                if (other.relations == null) continue;

                for (int i = 0; i < socialDefs.Count; i++)
                {
                    var def = socialDefs[i];
                    if (def == null) continue;
                    if (def.IsMemory) continue; // MemorySocial 已由记忆扫描处理

                    var thought = TryCreateSocialSituationalThoughtSafe(ai, other, def);
                    if (thought == null || thought.def == null || !thought.Active) continue;

                    int stage = thought.CurStageIndex + 1;
                    int otherId = other.thingIDNumber;
                    long key = ((long)thought.def.shortHash << 40)
                             | (((long)stage & 0xFFL) << 32)
                             | (uint)otherId;

                    if (_socialThoughtNextApplyTick.TryGetValue(key, out int nextTick) && now < nextTick)
                        continue;

                    var resp = DefDatabase<AIEventResponseDef>.GetNamedSilentFail(thought.def.defName);
                    if (resp == null) continue;

                    Apply(resp.affDelta, resp.trustDelta, resp.eventLabel);
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
                // 某些原版/DLC social thought worker 会对特殊 pawn 组合抛异常；这里静默跳过避免日志刷屏。
                return null;
            }
        }
    }
}
