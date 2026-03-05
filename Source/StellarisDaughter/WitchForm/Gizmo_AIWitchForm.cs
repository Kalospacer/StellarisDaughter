using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 魔女化Gizmo - 显示魔女因子条和变身按钮
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_AIWitchForm : Gizmo
    {
        // ── 颜色 ──────────────────────────────────────────────────────
        private static readonly Texture2D BarBg =
            SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);

        // 紫色到粉色渐变系统
        private static readonly Texture2D BarFillPurpleLight =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.50f, 0.85f)); // 淡紫色

        private static readonly Texture2D BarFillPurple =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.60f, 0.35f, 0.75f)); // 中紫色

        private static readonly Texture2D BarFillPurpleDark =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.50f, 0.25f, 0.65f)); // 深紫色

        private static readonly Texture2D BarFillPink =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.85f, 0.45f, 0.70f)); // 粉紫色

        private static readonly Texture2D BarHalfLine =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.6f, 0.6f, 0.6f, 0.9f));

        private static readonly Texture2D BarBorder =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.4f, 0.2f, 0.5f, 0.8f)); // 紫色边框


        // ── 布局 ──────────────────────────────────────────────────────
        private const float Width       = 220f;
        private const float GizmoHeight = 75f;
        private const float Pad         = 4f;
        private const float LabelPct    = 0.28f;
        private const float RowGap      = 3f;

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

            var inner  = outer.ContractedBy(Pad);
            float rowH = (inner.height - RowGap) / 2f;

            // 状态标签行
            var stateRow = new Rect(inner.x, inner.y,                  inner.width, rowH);
            DrawStateLabel(stateRow);

            // 魔女因子条（靠底部对齐）
            var barRow   = new Rect(inner.x, inner.y + rowH + RowGap,  inner.width, rowH);
            DrawWitchFactorBar(barRow);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font   = GameFont.Small;

            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawStateLabel(Rect row)
        {
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            string stateLabel = comp.state switch
            {
                WitchFormState.Normal    => "SD_Witch_State_Normal".Translate(),
                WitchFormState.WitchForm => "SD_Witch_State_Witch".Translate(),
                WitchFormState.Berserk   => "SD_Witch_State_Berserk".Translate(),
                _                        => "Unknown"
            };

            GUI.color = comp.IsBerserk
                ? Color.red
                : (comp.IsWitchForm ? new Color(0.8f, 0.4f, 0.8f) : new Color(0.4f, 0.7f, 1.0f));

            Widgets.Label(row, stateLabel);
            GUI.color = Color.white;
        }

        private void DrawWitchFactorBar(Rect row)
        {
            // 标签
            var labelRect = new Rect(row.x, row.y, row.width * LabelPct, row.height);
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, "魔女因子");
            Text.Anchor = TextAnchor.UpperLeft;

            // 进度条背景
            var bar = new Rect(labelRect.xMax, row.y + 2f, row.width - labelRect.width, row.height - 4f);
            GUI.DrawTexture(bar, BarBg);
            
            // 绘制紫色边框
            Widgets.DrawBox(bar, 1);
            GUI.color = new Color(0.5f, 0.3f, 0.6f, 0.8f);
            Widgets.DrawBox(bar, 1);
            GUI.color = Color.white;

            // 填充 - 紫色到粉色渐变效果
            float max     = comp.MaxWitchFactor;
            float current = comp.witchFactor;
            float fillPct = Mathf.Clamp01(current / max);

            // 渐变填充：分段绘制实现平滑过渡
            if (fillPct > 0f)
            {
                float fillWidth = bar.width * fillPct;
                
                // 第一段：0-50% 使用中紫色
                if (fillPct > 0f)
                {
                    float segment1Width = Mathf.Min(fillWidth, bar.width * 0.5f);
                    var rect1 = new Rect(bar.x, bar.y, segment1Width, bar.height);
                    GUI.DrawTexture(rect1, BarFillPurple);
                }
                
                // 第二段：50-90% 使用深紫色（渐变过渡）
                if (fillPct > 0.5f)
                {
                    float segment2Start = bar.width * 0.5f;
                    float segment2Width = Mathf.Min(fillWidth - segment2Start, bar.width * 0.4f);
                    var rect2 = new Rect(bar.x + segment2Start, bar.y, segment2Width, bar.height);
                    GUI.DrawTexture(rect2, BarFillPurpleDark);
                }
                
                // 第三段：90-100% 使用粉紫色（危险区域）
                if (fillPct > 0.9f)
                {
                    float segment3Start = bar.width * 0.9f;
                    float segment3Width = fillWidth - segment3Start;
                    var rect3 = new Rect(bar.x + segment3Start, bar.y, segment3Width, bar.height);
                    GUI.DrawTexture(rect3, BarFillPink);
                }
            }

            // 50%标记线
            float halfX = bar.x + bar.width * 0.5f;
            GUI.DrawTexture(new Rect(halfX - 1f, bar.y, 2f, bar.height), BarHalfLine);

            // 量表刻度（0%, 25%, 50%, 75%, 100%）
            DrawScaleMarks(bar);

            // 数值文字
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font   = GameFont.Tiny;
            Widgets.Label(bar, $"{current:F0} / {max:F0}");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font   = GameFont.Small;

            // 高亮 + Tooltip
            if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
                string tooltip = BuildDetailedTooltip();
                TooltipHandler.TipRegion(row, tooltip);
            }
        }

        /// <summary>
        /// 绘制量表刻度线：0%, 25%, 50%, 75%, 100%
        /// </summary>
        private void DrawScaleMarks(Rect bar)
        {
            var markColor = new Color(0.5f, 0.4f, 0.6f, 0.6f); // 紫色调刻度
            var oldColor = GUI.color;
            GUI.color = markColor;

            // 25% 刻度
            float mark25 = bar.x + bar.width * 0.25f;
            GUI.DrawTexture(new Rect(mark25 - 0.5f, bar.y, 1f, bar.height * 0.3f), BarHalfLine);
            
            // 75% 刻度
            float mark75 = bar.x + bar.width * 0.75f;
            GUI.DrawTexture(new Rect(mark75 - 0.5f, bar.y, 1f, bar.height * 0.3f), BarHalfLine);
            
            // 0% 刻度（左端）
            GUI.DrawTexture(new Rect(bar.x + 1f, bar.y, 1f, bar.height * 0.25f), BarHalfLine);
            
            // 100% 刻度（右端）
            GUI.DrawTexture(new Rect(bar.xMax - 2f, bar.y, 1f, bar.height * 0.25f), BarHalfLine);

            GUI.color = oldColor;
        }

        /// <summary>
        /// 构建详细的悬停提示信息
        /// </summary>
        private string BuildDetailedTooltip()
        {
            var upbringing = comp.Pawn?.GetComp<CompAIUpbringing>();
            float affection = upbringing?.affection ?? 0f;
            float trust = upbringing?.trust ?? 0f;

            // 当前值和上限
            float current = comp.witchFactor;
            float max = comp.MaxWitchFactor;
            float percentage = (current / max) * 100f;

            // 计算增长率
            float baseRate = comp.Props.baseGrowthRate;
            float trustModifier = trust * 0.001f;
            float modifiedBaseRate = Mathf.Max(baseRate - trustModifier, 0f);
            
            float actualRate = modifiedBaseRate;
            if (comp.IsWitchForm)
            {
                actualRate *= comp.Props.witchFormGrowthMult;
            }
            else if (current >= comp.HalfMaxFactor)
            {
                actualRate *= comp.Props.halfMaxGrowthMult;
            }

            // 转换为每天的增长（TickRare = 250 ticks, 1天 = 60000 ticks）
            float perDay = actualRate * (60000f / 250f);

            // 构建提示文本
            var tooltip = new System.Text.StringBuilder();
            
            // 标题
            tooltip.AppendLine("═══ 魔女因子系统 ═══\n");
            
            // 当前状态
            tooltip.AppendLine($"【当前状态】");
            tooltip.AppendLine($"  数值：{current:F1} / {max:F1} ({percentage:F1}%)");
            tooltip.AppendLine($"  状态：{GetStateDescription()}");
            tooltip.AppendLine();

            // 增长率详情
            tooltip.AppendLine($"【增长率】");
            tooltip.AppendLine($"  当前增长：{actualRate:F3} / TickRare ({perDay:F1} / 天)");
            tooltip.AppendLine($"  基础速度：{baseRate:F3}");
            tooltip.AppendLine($"  信任修正：{(trustModifier >= 0 ? "-" : "+")}{Mathf.Abs(trustModifier):F3} (信任值 {trust:F0})");
            
            if (comp.IsWitchForm)
            {
                tooltip.AppendLine($"  变身加速：×{comp.Props.witchFormGrowthMult:F1}");
            }
            else if (current >= comp.HalfMaxFactor)
            {
                tooltip.AppendLine($"  超50%减速：×{comp.Props.halfMaxGrowthMult:F1}");
            }
            tooltip.AppendLine();

            // 上限公式
            tooltip.AppendLine($"【上限计算】");
            tooltip.AppendLine($"  公式：基础上限 + 好感度 × 0.1");
            tooltip.AppendLine($"  计算：{comp.Props.baseMaxFactor:F0} + {affection:F0} × 0.1 = {max:F1}");
            tooltip.AppendLine();

            // 机制说明
            tooltip.AppendLine($"【机制说明】");
            tooltip.AppendLine($"  • 信任值越低，增长越快（负信任加速）");
            tooltip.AppendLine($"  • 好感度越高，上限越高");
            tooltip.AppendLine($"  • 未变身时，自动停在50%");
            tooltip.AppendLine($"  • 变身后，增长速度×3");
            tooltip.AppendLine($"  • 达到上限时，触发魔女狂暴");
            tooltip.AppendLine();

            // 操作提示
            tooltip.AppendLine($"【操作提示】");
            if (comp.IsBerserk)
            {
                tooltip.AppendLine($"  ⚠ 当前处于狂暴状态！");
                tooltip.AppendLine($"  → 击倒后可右键选择'镇压'结束");
            }
            else if (current >= max * 0.9f)
            {
                tooltip.AppendLine($"  ⚠ 危险！接近上限，即将失控");
                tooltip.AppendLine($"  → 建议立即解除变身");
            }
            else if (comp.IsWitchForm)
            {
                tooltip.AppendLine($"  → 点击变身按钮可解除魔女形态");
            }
            else if (current >= comp.HalfMaxFactor)
            {
                tooltip.AppendLine($"  → 点击变身按钮可进入魔女形态");
                tooltip.AppendLine($"  → 变身后增长加速，请谨慎使用");
            }
            else
            {
                tooltip.AppendLine($"  → 因子达到50%后可手动变身");
            }

            return tooltip.ToString();
        }

        /// <summary>
        /// 获取状态描述
        /// </summary>
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
