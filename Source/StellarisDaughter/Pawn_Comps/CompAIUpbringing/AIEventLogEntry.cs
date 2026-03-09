using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 单条养成数值变化日志，用于 Gizmo 悬浮窗展示。
    /// </summary>
    public class AIEventLogEntry : IExposable
    {
        /// <summary>显示标签。</summary>
        public string label;

        /// <summary>好感变化量。</summary>
        public float affDelta;

        /// <summary>信任变化量。</summary>
        public float trsDelta;

        /// <summary>发生时的游戏 tick。</summary>
        public int tick;

        /// <summary>连续合并次数。</summary>
        public int repeatCount = 1;

        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label", "");
            Scribe_Values.Look(ref affDelta, "affDelta", 0f);
            Scribe_Values.Look(ref trsDelta, "trsDelta", 0f);
            Scribe_Values.Look(ref tick, "tick", 0);
            Scribe_Values.Look(ref repeatCount, "repeatCount", 1);
        }

        /// <summary>返回“第 X 天”格式的相对时间。</summary>
        public string DayLabel => "SD_AIEventLog_DayLabel".Translate(GenDate.DaysPassed - GenDate.DaysPassedAt(tick) + 1);
    }
}
