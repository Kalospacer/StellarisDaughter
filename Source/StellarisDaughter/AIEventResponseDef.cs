using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 将 AI 女儿事件对照表从硬编码 C# 迁移到 XML。
    /// defName 对应原版 ThoughtDef 的 defName，
    /// 加载时通过 DefDatabase 查询，支持 Patch 扩展。
    /// </summary>
    public class AIEventResponseDef : Def
    {
        /// <summary> Gizmo 事件日志和 Tooltip 中显示的中文标签 </summary>
        public string eventLabel = "";

        /// <summary> 触发事件时好感度变化量 </summary>
        public float affDelta = 0f;

        /// <summary> 触发事件时信任值变化量 </summary>
        public float trustDelta = 0f;
    }
}
