using RimWorld;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary> 单条数值变化日志，用于Gizmo悬浮窗展示 </summary>
    public class AIEventLogEntry : IExposable
    {
        /// <summary> 显示标签（中文事件名） </summary>
        public string label;

        /// <summary> 好感变化量 </summary>
        public float affDelta;

        /// <summary> 信任变化量 </summary>
        public float trsDelta;

        /// <summary> 发生时的游戏tick </summary>
        public int tick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref label,    "label",    "");
            Scribe_Values.Look(ref affDelta, "affDelta", 0f);
            Scribe_Values.Look(ref trsDelta, "trsDelta", 0f);
            Scribe_Values.Look(ref tick,     "tick",     0);
        }

        /// <summary> 返回"第X天"格式的相对时间 </summary>
        public string DayLabel =>
            $"第{GenDate.DaysPassed - GenDate.DaysPassedAt(tick) + 1}天";
    }
}
