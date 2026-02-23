using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        private const int SituationalThoughtEventCooldownTicks = 30000; // 0.5 in-game day

        private void ScanMemoriesForEvents(Pawn ai)
        {
            var memories = ai.needs?.mood?.thoughts?.memories?.Memories;
            if (memories == null) return;

            foreach (var memory in memories)
            {
                if (memory?.def == null) continue;

                // 用对象引用哈希做 Session 内去重（不序列化，存档后重置一次问题不大）
                long key = (long)RuntimeHelpers.GetHashCode(memory);
                if (_processedMemories.Contains(key)) continue;
                _processedMemories.Add(key);

                var resp = DefDatabase<AIEventResponseDef>.GetNamedSilentFail(memory.def.defName);
                if (resp != null)
                    Apply(resp.affDelta, resp.trustDelta, resp.eventLabel);
            }
        }

        private void ScanSituationalThoughtsForEvents(Pawn ai)
        {
            var thoughtHandler = ai.needs?.mood?.thoughts;
            if (thoughtHandler == null) return;

            if (_tmpMoodThoughts == null)
                _tmpMoodThoughts = new System.Collections.Generic.List<Thought>();
            if (_situationalThoughtNextApplyTick == null)
                _situationalThoughtNextApplyTick = new System.Collections.Generic.Dictionary<long, int>();
            if (_scratchSituationalThoughtKeys == null)
                _scratchSituationalThoughtKeys = new HashSet<long>();

            _scratchSituationalThoughtKeys.Clear();
            thoughtHandler.GetAllMoodThoughts(_tmpMoodThoughts);
            int now = Find.TickManager.TicksGame;

            foreach (var thought in _tmpMoodThoughts)
            {
                if (!(thought is Thought_Situational situational)) continue;
                if (situational.def == null || !situational.Active) continue;

                long key = ((long)situational.def.shortHash << 32) | (uint)(situational.CurStageIndex + 1);
                _scratchSituationalThoughtKeys.Add(key);
                if (_situationalThoughtNextApplyTick.TryGetValue(key, out int nextTick) && now < nextTick)
                    continue;

                var resp = DefDatabase<AIEventResponseDef>.GetNamedSilentFail(situational.def.defName);
                if (resp != null)
                {
                    Apply(resp.affDelta, resp.trustDelta, resp.eventLabel);
                    _situationalThoughtNextApplyTick[key] = now + SituationalThoughtEventCooldownTicks;
                }
            }

            // 清理已不再激活的情境Thought冷却项，避免字典无限增长。
            if (_situationalThoughtNextApplyTick.Count > 0)
            {
                var keysToRemove = new System.Collections.Generic.List<long>();
                foreach (var kv in _situationalThoughtNextApplyTick)
                {
                    if (!_scratchSituationalThoughtKeys.Contains(kv.Key))
                        keysToRemove.Add(kv.Key);
                }

                for (int i = 0; i < keysToRemove.Count; i++)
                    _situationalThoughtNextApplyTick.Remove(keysToRemove[i]);
            }
        }
    }
}
