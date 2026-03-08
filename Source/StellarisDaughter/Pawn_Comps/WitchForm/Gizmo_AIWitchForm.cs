using System.Text;
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
        private static readonly Texture2D BarBorder = SolidColorMaterials.NewSolidColorTexture(new Color(0.45f, 0.22f, 0.58f, 0.9f));

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
            Widgets.Label(labelRect, "SD_Witch_Gizmo_Label".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            var bar = new Rect(labelRect.xMax, row.y + 2f, row.width - labelRect.width, row.height - 4f);
            GUI.DrawTexture(bar, BarBg);
            Widgets.DrawBoxSolid(bar, new Color(0f, 0f, 0f, 0.15f));
            DrawBorder(bar);

            var max = comp.MaxWitchFactor;
            var current = comp.witchFactor;
            var fillPct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
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

            var thresholdX = bar.x + bar.width * thresholdPct;
            GUI.DrawTexture(new Rect(thresholdX - 1f, bar.y, 2f, bar.height), BarHalfLine);
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

        private void DrawBorder(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), BarBorder);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BarBorder);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), BarBorder);
            GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), BarBorder);
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
            var percentage = max > 0f ? (current / max) * 100f : 0f;
            var baseRate = comp.Props.baseGrowthRate;

            float actualRate;
            string trustRuleText;
            if (comp.IsWitchForm)
            {
                var hostileTrustBonus = trust < 0f ? -trust * comp.Props.negativeTrustGrowthFactor : 0f;
                actualRate = Mathf.Max(baseRate + hostileTrustBonus, 0f) * comp.Props.witchFormGrowthMult;
                trustRuleText = trust < 0f
                    ? "SD_Witch_Tooltip_TrustRule_NegativeActive".Translate(hostileTrustBonus.ToString("F3"), trust.ToString("F0"))
                    : "SD_Witch_Tooltip_TrustRule_PositiveActive".Translate(trust.ToString("F0"));
            }
            else if (trust > 0f && current > 0f)
            {
                actualRate = -Mathf.Min(
                    trust * comp.Props.positiveTrustDecayFactor,
                    comp.Props.baseGrowthRate * comp.Props.positiveTrustDecayMaxBaseFraction);
                trustRuleText = "SD_Witch_Tooltip_TrustRule_PositiveDecay".Translate(Mathf.Abs(actualRate).ToString("F3"), trust.ToString("F0"));
            }
            else if (current < comp.TransformThresholdFactor)
            {
                actualRate = 0f;
                trustRuleText = "SD_Witch_Tooltip_TrustRule_BelowThreshold".Translate((comp.TransformThresholdRatio * 100f).ToString("F0"), trust.ToString("F0"));
            }
            else
            {
                var hostileTrustBonus = trust < 0f ? -trust * comp.Props.negativeTrustGrowthFactor : 0f;
                actualRate = Mathf.Max(baseRate + hostileTrustBonus, 0f) * comp.Props.halfMaxGrowthMult;
                trustRuleText = trust < 0f
                    ? "SD_Witch_Tooltip_TrustRule_NegativePassive".Translate(hostileTrustBonus.ToString("F3"), trust.ToString("F0"))
                    : "SD_Witch_Tooltip_TrustRule_PassiveGrowth".Translate(trust.ToString("F0"));
            }

            var perDay = actualRate * (60000f / 250f);
            var tooltip = new StringBuilder();
            tooltip.AppendLine("SD_Witch_Tooltip_Header".Translate());
            tooltip.AppendLine();
            tooltip.AppendLine("SD_Witch_Tooltip_CurrentState_Section".Translate());
            tooltip.AppendLine("SD_Witch_Tooltip_CurrentValue".Translate(current.ToString("F1"), max.ToString("F1"), percentage.ToString("F1")));
            tooltip.AppendLine("SD_Witch_Tooltip_CurrentState".Translate(GetStateDescription()));
            tooltip.AppendLine();
            tooltip.AppendLine("SD_Witch_Tooltip_Growth_Section".Translate());
            tooltip.AppendLine("SD_Witch_Tooltip_CurrentRate".Translate(actualRate.ToString("F3"), perDay.ToString("F1")));
            tooltip.AppendLine("SD_Witch_Tooltip_BaseRate".Translate(baseRate.ToString("F3")));
            tooltip.AppendLine("SD_Witch_Tooltip_TrustRule".Translate(trustRuleText));
            if (comp.IsWitchForm)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_WitchMult".Translate(comp.Props.witchFormGrowthMult.ToString("F1")));
            }
            else if (current >= comp.TransformThresholdFactor)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_HalfMult".Translate(comp.Props.halfMaxGrowthMult.ToString("F1")));
            }

            tooltip.AppendLine();
            tooltip.AppendLine("SD_Witch_Tooltip_MaxCalc_Section".Translate());
            tooltip.AppendLine("SD_Witch_Tooltip_MaxFormula".Translate(comp.Props.affectionToMaxFactor.ToString("F2")));
            tooltip.AppendLine("SD_Witch_Tooltip_MaxFormulaValue".Translate(comp.Props.baseMaxFactor.ToString("F0"), affection.ToString("F0"), comp.Props.affectionToMaxFactor.ToString("F2"), max.ToString("F1")));
            tooltip.AppendLine();
            tooltip.AppendLine("SD_Witch_Tooltip_Mechanic_Section".Translate());
            tooltip.AppendLine("SD_Witch_Tooltip_Mechanic_NegativeTrust".Translate());
            tooltip.AppendLine("SD_Witch_Tooltip_Mechanic_Affection".Translate());
            tooltip.AppendLine("SD_Witch_Tooltip_Mechanic_Threshold".Translate((comp.TransformThresholdRatio * 100f).ToString("F0")));
            tooltip.AppendLine("SD_Witch_Tooltip_Mechanic_PositiveTrust".Translate());
            tooltip.AppendLine("SD_Witch_Tooltip_Mechanic_Berserk".Translate());
            if (comp.Props.transformCooldownTicks > 0 || comp.Props.cancelTransformCooldownTicks > 0)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_Mechanic_Cooldown".Translate());
            }
            tooltip.AppendLine();
            tooltip.AppendLine("SD_Witch_Tooltip_Cooldown_Section".Translate());
            tooltip.AppendLine("SD_Witch_Tooltip_TransformCooldown".Translate(CompAIWitchForm.FormatCooldownDuration(comp.Props.transformCooldownTicks)));
            tooltip.AppendLine("SD_Witch_Tooltip_CancelCooldown".Translate(CompAIWitchForm.FormatCooldownDuration(comp.Props.cancelTransformCooldownTicks)));
            if (comp.state == WitchFormState.Normal && comp.TransformCooldownRemainingTicks > 0)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_TransformCooldownRemaining".Translate(CompAIWitchForm.FormatCooldownDuration(comp.TransformCooldownRemainingTicks)));
            }
            else if (comp.state == WitchFormState.WitchForm && comp.CancelTransformCooldownRemainingTicks > 0)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_CancelCooldownRemaining".Translate(CompAIWitchForm.FormatCooldownDuration(comp.CancelTransformCooldownRemainingTicks)));
            }

            tooltip.AppendLine();
            tooltip.AppendLine("SD_Witch_Tooltip_Action_Section".Translate());
            if (comp.IsBerserk)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_Action_Berserk1".Translate());
                tooltip.AppendLine("SD_Witch_Tooltip_Action_Berserk2".Translate());
            }
            else if (current >= max * 0.9f)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_Action_Danger1".Translate());
                tooltip.AppendLine("SD_Witch_Tooltip_Action_Danger2".Translate());
            }
            else if (comp.IsWitchForm)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_Action_Cancel".Translate());
            }
            else if (current >= comp.TransformThresholdFactor)
            {
                tooltip.AppendLine("SD_Witch_Tooltip_Action_Transform1".Translate());
                tooltip.AppendLine("SD_Witch_Tooltip_Action_Transform2".Translate());
            }
            else
            {
                tooltip.AppendLine("SD_Witch_Tooltip_Action_Threshold".Translate((comp.TransformThresholdRatio * 100f).ToString("F0")));
            }

            return tooltip.ToString();
        }

        private string GetStateDescription()
        {
            return comp.state switch
            {
                WitchFormState.Normal => "SD_Witch_StateDesc_Normal".Translate(),
                WitchFormState.WitchForm => "SD_Witch_StateDesc_Witch".Translate(),
                WitchFormState.Berserk => "SD_Witch_StateDesc_Berserk".Translate(),
                _ => "SD_Unknown".Translate()
            };
        }

    }
}
