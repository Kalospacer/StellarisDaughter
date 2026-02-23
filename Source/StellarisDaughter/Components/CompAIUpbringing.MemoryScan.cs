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
    }
}
