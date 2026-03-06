using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 魔女因子 Gizmo。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_AIWitchForm : Gizmo
    {
        private static readonly Texture2D BarBg = SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);
        private static readonly Texture2D BarFillPurple = SolidColorMaterials.NewSolidColorTexture(new Color(0.60f, 0.35f, 0.75f));
        private static readonly Texture2D BarFillPurpleDark = SolidColorMaterials.NewSolidColorTexture(new Color(0.50f, 0.25f, 0.65f));
        private static readonly Texture2D BarFillPink = SolidColorMaterials.NewSolidColorTexture(new Color(0.85f, 0.45f, 0.70f));
        private static readonly Texture2D BarHalfLine = SolidColorMaterials.NewSolidColorTexture(new Color(0.6f, 0.6f, 0.6f, 0.9f));

        private const float Width = 220f;
        private const float GizmoHeight = 75f;
        private const float Pad = 4f;
        private const float LabelPct = 0.28f;
        private const float RowGap = 3f;

        private readonly CompAIWitchForm comp;

        public Gizmo_AIWitchForm(CompAIWitchForm comp)
        {
            this.comp = comp;
            Order = -98f;
        }

        public override float GetWidth(float maxWidth) => Width;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var outer = new Rect(topLeft.x, topLeft.y, Width, GizmoHeight);
            Widgets.DrawWindowBackground(outer);

            var inner = outer.ContractedBy(Pad);
            var rowH = (inner.height - RowGap) / 2f;

            DrawStateLabel(new Rect(inner.x, inner.y, inner.width, rowH));
            DrawWitchFactorBar(new Rect(inner.x, inner.y + rowH + RowGap, inner.width, rowH));

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawStateLabel(Rect row)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            string stateLabel = comp.state switch
            {
                WitchFormState.Normal => "SD_Witch_State_Normal".Translate(),
                WitchFormState.WitchForm => "SD_Witch_State_Witch".Translate(),
                WitchFormState.Berserk => "SD_Witch_State_Berserk".Translate(),
                _ => "Unknown"
            };

            GUI.color = comp.IsBerserk
                ? Color.red
                : comp.IsWitchForm ? new Color(0.8f, 0.4f, 0.8f) : new Color(0.4f, 0.7f, 1f);
            Widgets.Label(row, stateLabel);
            GUI.color = Color.white;
        }

        private void DrawWitchFactorBar(Rect row)
        {
            var labelRect = new Rect(row.x, row.y, row.width * LabelPct, row.height);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, "魔女因子");
            Text.Anchor = TextAnchor.UpperLeft;

            var bar = new Rect(labelRect.xMax, row.y + 2f, row.width - labelRect.width, row.height - 4f);
            GUI.DrawTexture(bar, BarBg);
            Widgets.DrawBox(bar, 1);

            var max = comp.MaxWitchFactor;
            var current = comp.witchFactor;
            var fillPct = Mathf.Clamp01(current / max);

            var thresholdPct = Mathf.Clamp01(comp.TransformThresholdRatio);

            if (fillPct > 0f)
            {
                var fillWidth = bar.width * fillPct;
                var firstWidth = Mathf.Min(fillWidth, bar.width * thresholdPct);
                if (firstWidth > 0f)
                {
                    GUI.DrawTexture(new Rect(bar.x, bar.y, firstWidth, bar.height), BarFillPurple);
                }

                if (fillPct > thresholdPct)
                {
                    var secondStart = bar.width * thresholdPct;
                    var secondWidth = Mathf.Min(fillWidth - secondStart, bar.width * Mathf.Max(0.9f - thresholdPct, 0f));
                    if (secondWidth > 0f)
                    {
                        GUI.DrawTexture(new Rect(bar.x + secondStart, bar.y, secondWidth, bar.height), BarFillPurpleDark);
                    }
                }

                if (fillPct > 0.9f)
                {
                    var thirdStart = bar.width * 0.9f;
                    var thirdWidth = fillWidth - thirdStart;
                    if (thirdWidth > 0f)
                    {
                        GUI.DrawTexture(new Rect(bar.x + thirdStart, bar.y, thirdWidth, bar.height), BarFillPink);
                    }
                }
            }

            var halfX = bar.x + bar.width * thresholdPct;
            GUI.DrawTexture(new Rect(halfX - 1f, bar.y, 2f, bar.height), BarHalfLine);
            DrawScaleMarks(bar);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(bar, $"{current:F0} / {max:F0}");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
                TooltipHandler.TipRegion(row, BuildDetailedTooltip());
            }
        }

        private void DrawScaleMarks(Rect bar)
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0.5f, 0.4f, 0.6f, 0.6f);
            GUI.DrawTexture(new Rect(bar.x + bar.width * 0.25f - 0.5f, bar.y, 1f, bar.height * 0.3f), BarHalfLine);
            GUI.DrawTexture(new Rect(bar.x + bar.width * 0.75f - 0.5f, bar.y, 1f, bar.height * 0.3f), BarHalfLine);
            GUI.DrawTexture(new Rect(bar.x + 1f, bar.y, 1f, bar.height * 0.25f), BarHalfLine);
            GUI.DrawTexture(new Rect(bar.xMax - 2f, bar.y, 1f, bar.height * 0.25f), BarHalfLine);
            GUI.color = oldColor;
        }

        private string BuildDetailedTooltip()
        {
            var upbringing = comp.Pawn?.GetComp<CompAIUpbringing>();
            var affection = upbringing?.affection ?? 0f;
            var trust = upbringing?.trust ?? 0f;
            var current = comp.witchFactor;
            var max = comp.MaxWitchFactor;
            var percentage = (current / max) * 100f;
            var baseRate = comp.Props.baseGrowthRate;

            float actualRate;
            string trustRuleText;
            if (comp.IsWitchForm)
            {
                var hostileTrustBonus = trust < 0f ? -trust * comp.Props.negativeTrustGrowthFactor : 0f;
                actualRate = Mathf.Max(baseRate + hostileTrustBonus, 0f) * comp.Props.witchFormGrowthMult;
                trustRuleText = trust < 0f
                    ? $"负信任加速：+{hostileTrustBonus:F3}（信任值 {trust:F0}）"
                    : $"正信任在变身时不再抑制增长（信任值 {trust:F0}）";
            }
            else if (trust > 0f && current > 0f)
            {
                actualRate = -Mathf.Min(
                    trust * comp.Props.positiveTrustDecayFactor,
                    comp.Props.baseGrowthRate * comp.Props.positiveTrustDecayMaxBaseFraction);
                trustRuleText = $"正信任缓降：{Mathf.Abs(actualRate):F3}（信任值 {trust:F0}）";
            }
            else if (current < comp.TransformThresholdFactor)
            {
                actualRate = 0f;
                trustRuleText = $"未变身且低于阈值 {comp.TransformThresholdRatio * 100f:F0}% 时不增长（信任值 {trust:F0}）";
            }
            else
            {
                var hostileTrustBonus = trust < 0f ? -trust * comp.Props.negativeTrustGrowthFactor : 0f;
                actualRate = Mathf.Max(baseRate + hostileTrustBonus, 0f) * comp.Props.halfMaxGrowthMult;
                trustRuleText = trust < 0f
                    ? $"负信任加速：+{hostileTrustBonus:F3}（信任值 {trust:F0}）"
                    : $"后半段按缓速增长（信任值 {trust:F0}）";
            }

            var perDay = actualRate * (60000f / 250f);
            var tooltip = new System.Text.StringBuilder();
            tooltip.AppendLine("═══ 魔女因子系统 ═══");
            tooltip.AppendLine();
            tooltip.AppendLine("【当前状态】");
            tooltip.AppendLine($"  数值：{current:F1} / {max:F1} ({percentage:F1}%)");
            tooltip.AppendLine($"  状态：{GetStateDescription()}");
            tooltip.AppendLine();
            tooltip.AppendLine("【增长率】");
            tooltip.AppendLine($"  当前变化：{actualRate:F3} / TickRare ({perDay:F1} / 天)");
            tooltip.AppendLine($"  基础速度：{baseRate:F3}");
            tooltip.AppendLine($"  信任规则：{trustRuleText}");
            if (comp.IsWitchForm)
            {
                tooltip.AppendLine($"  变身加速：×{comp.Props.witchFormGrowthMult:F1}");
            }
            else if (current >= comp.TransformThresholdFactor)
            {
                tooltip.AppendLine($"  后半段倍率：×{comp.Props.halfMaxGrowthMult:F1}");
            }

            tooltip.AppendLine();
            tooltip.AppendLine("【上限计算】");
            tooltip.AppendLine("  公式：基础上限 + 好感度 × 0.1");
            tooltip.AppendLine($"  计算：{comp.Props.baseMaxFactor:F0} + {affection:F0} × 0.1 = {max:F1}");
            tooltip.AppendLine();
            tooltip.AppendLine("【机制说明】");
            tooltip.AppendLine("  • 负信任越高，魔女因子增长越快");
            tooltip.AppendLine("  • 好感度越高，魔女因子上限越高");
            tooltip.AppendLine($"  • 未变身且低于 {comp.TransformThresholdRatio * 100f:F0}% 阈值时，不会自然增长");
            tooltip.AppendLine("  • 信任为正时，未变身状态会缓慢降低魔女因子");
            tooltip.AppendLine("  • 达到上限时，会触发魔女狂暴");
            tooltip.AppendLine();
            tooltip.AppendLine("【操作提示】");
            if (comp.IsBerserk)
            {
                tooltip.AppendLine("  • 当前处于狂暴状态");
                tooltip.AppendLine("  • 击倒后可右键选择“镇压”结束");
            }
            else if (current >= max * 0.9f)
            {
                tooltip.AppendLine("  • 危险：接近上限，即将失控");
                tooltip.AppendLine("  • 建议立即解除变身");
            }
            else if (comp.IsWitchForm)
            {
                tooltip.AppendLine("  • 点击变身按钮可解除魔女形态");
            }
            else if (current >= comp.TransformThresholdFactor)
            {
                tooltip.AppendLine("  • 点击变身按钮可进入魔女形态");
                tooltip.AppendLine("  • 变身后因子增长会明显加快");
            }
            else
            {
                tooltip.AppendLine($"  • 因子达到 {comp.TransformThresholdRatio * 100f:F0}% 后可手动变身");
            }

            return tooltip.ToString();
        }

        private string GetStateDescription()
        {
            return comp.state switch
            {
                WitchFormState.Normal => "正常形态",
                WitchFormState.WitchForm => "魔女形态",
                WitchFormState.Berserk => "魔女狂暴（失控）",
                _ => "未知"
            };
        }
    }
}
