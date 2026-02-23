using System.Text;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// CompAIUpbringing：Gizmo 悬浮窗文本模块
    /// 生成好感条/信任条 Tooltip：近期事件流水 + 被动积累分桶统计。
    /// </summary>
    public partial class CompAIUpbringing
    {
        public string GetAffectionTooltip()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SD_GizmoAff_TipHeader".Translate(affection.ToString("F1")));
            sb.AppendLine();
            sb.AppendLine("SD_TipEvents".Translate());

            bool hasEvents = false;
            for (int i = eventLog.Count - 1; i >= 0 && i >= eventLog.Count - 10; i--)
            {
                var e = eventLog[i];
                if (e.affDelta == 0f) continue;
                hasEvents = true;
                string delta = e.affDelta > 0f
                    ? $"<color=#7ecf7e>好感 +{e.affDelta:F1}</color>"
                    : $"<color=#cf7e7e>好感 {e.affDelta:F1}</color>";
                sb.AppendLine($"  {e.DayLabel}  {e.label.PadRight(14)}{delta}");
            }
            if (!hasEvents)
                sb.AppendLine($"  {"SD_TipNoEvents".Translate()}");

            sb.AppendLine();
            sb.AppendLine("SD_TipPassive".Translate());
            sb.AppendLine($"  {"自然陪伴",-12}{passiveNaturalAff:+0.0;-0.0;+0.0}");
            sb.AppendLine($"  {"孤独衰减",-12}{passiveLonelyAff:+0.0;-0.0;+0.0}");

            return sb.ToString().TrimEndNewlines();
        }

        public string GetTrustTooltip()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SD_GizmoTrs_TipHeader".Translate(trust.ToString("F1")));
            sb.AppendLine();
            sb.AppendLine("SD_TipEvents".Translate());

            bool hasEvents = false;
            for (int i = eventLog.Count - 1; i >= 0 && i >= eventLog.Count - 10; i--)
            {
                var e = eventLog[i];
                if (e.trsDelta == 0f) continue;
                hasEvents = true;
                string delta = e.trsDelta > 0f
                    ? $"<color=#7ecf7e>信任 +{e.trsDelta:F1}</color>"
                    : $"<color=#cf7e7e>信任 {e.trsDelta:F1}</color>";
                sb.AppendLine($"  {e.DayLabel}  {e.label.PadRight(14)}{delta}");
            }
            if (!hasEvents)
                sb.AppendLine($"  {"SD_TipNoEvents".Translate()}");

            sb.AppendLine();
            sb.AppendLine("SD_TipPassive".Translate());
            sb.AppendLine($"  {"自然陪伴",-12}{passiveNaturalTrs:+0.0;-0.0;+0.0}");
            sb.AppendLine($"  {"孤独衰减",-12}{passiveLonelyTrs:+0.0;-0.0;+0.0}");
            sb.AppendLine($"  {"需求满足",-12}{passiveNeedsTrs:+0.0;-0.0;+0.0}");
            sb.AppendLine($"  {"环境质量",-12}{passiveEnvTrs:+0.0;-0.0;+0.0}");

            return sb.ToString().TrimEndNewlines();
        }
    }
}
