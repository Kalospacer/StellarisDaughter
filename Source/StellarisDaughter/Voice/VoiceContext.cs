using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 语音事件上下文 — Harmony Patch 触发时构造并传入 CompUnitVoice.TryPlay()。
    /// 条件 Worker 从此结构读取状态，无需自行查询游戏对象。
    /// 未来新增字段（如 Ability、DamageInfo）只需扩展此结构。
    /// </summary>
    public struct VoiceContext
    {
        /// <summary>语音主体 pawn</summary>
        public Pawn pawn;

        /// <summary>交互/攻击目标（可 null）</summary>
        public Thing target;

        /// <summary>当前被下达的 Job（可 null）</summary>
        public Verse.AI.Job job;

        /// <summary>当前地图</summary>
        public Map map;
    }
}
