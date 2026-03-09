using System.Text;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// Gizmo 悬浮窗文本生成。
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
                var entry = eventLog[i];
                if (entry.affDelta == 0f) continue;

                hasEvents = true;
                TaggedString delta = entry.affDelta > 0f
                    ? "SD_AIEvent_AffectionPositive".Translate(entry.affDelta.ToString("F1"))
                    : "SD_AIEvent_AffectionNegative".Translate(entry.affDelta.ToString("F1"));
                string label = entry.repeatCount > 1 ? $"{entry.label} ×{entry.repeatCount}" : entry.label;
                sb.AppendLine($"  {entry.DayLabel}  {label.PadRight(14)}{delta}");
            }

            if (!hasEvents)
                sb.AppendLine($"  {"SD_TipNoEvents".Translate()}");

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
                var entry = eventLog[i];
                if (entry.trsDelta == 0f) continue;

                hasEvents = true;
                TaggedString delta = entry.trsDelta > 0f
                    ? "SD_AIEvent_TrustPositive".Translate(entry.trsDelta.ToString("F1"))
                    : "SD_AIEvent_TrustNegative".Translate(entry.trsDelta.ToString("F1"));
                string label = entry.repeatCount > 1 ? $"{entry.label} ×{entry.repeatCount}" : entry.label;
                sb.AppendLine($"  {entry.DayLabel}  {label.PadRight(14)}{delta}");
            }

            if (!hasEvents)
                sb.AppendLine($"  {"SD_TipNoEvents".Translate()}");

            return sb.ToString().TrimEndNewlines();
        }
    }
}
