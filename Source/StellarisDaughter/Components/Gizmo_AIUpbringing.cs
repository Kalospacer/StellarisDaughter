using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 双向进度条 Gizmo：好感度（粉色）+ 信任值（蓝色），范围 -1000~+1000，0 居中。
    /// 悬浮窗显示近期事件流水和被动积累分析。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_AIUpbringing : Gizmo
    {
        // ── 颜色 ──────────────────────────────────────────────────────
        private static readonly Texture2D BarBg =
            SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);

        private static readonly Texture2D BarAffPos =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.90f, 0.50f, 0.70f)); // 粉红

        private static readonly Texture2D BarAffNeg =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.70f, 0.20f, 0.30f)); // 暗红

        private static readonly Texture2D BarTrsPos =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.30f, 0.65f, 0.90f)); // 天蓝

        private static readonly Texture2D BarTrsNeg =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.30f, 0.60f)); // 深蓝

        private static readonly Texture2D BarCenterLine =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.6f, 0.6f, 0.6f, 0.9f));

        // ── 布局 ──────────────────────────────────────────────────────
        private const float Width      = 220f;
        private const float GizmoHeight = 90f;
        private const float Pad        = 4f;
        private const float LabelPct   = 0.28f;
        private const float RowGap     = 3f;

        private readonly CompAIUpbringing comp;

        public Gizmo_AIUpbringing(CompAIUpbringing comp)
        {
            this.comp = comp;
            Order = -99f;
        }

        public override float GetWidth(float maxWidth) => Width;

        public override bool Visible
        {
            get
            {
                var p = comp.parent as Pawn;
                return p != null && (p.IsColonistPlayerControlled || p.IsPrisonerOfColony || p.IsSlaveOfColony);
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            // 向上延伸超出标准 75px 高度
            float extraUp = GizmoHeight - 75f;
            var outer = new Rect(topLeft.x, topLeft.y - extraUp, Width, GizmoHeight);
            Widgets.DrawWindowBackground(outer);

            var inner  = outer.ContractedBy(Pad);
            float rowH = (inner.height - RowGap) / 2f;

            var affRow = new Rect(inner.x, inner.y,                  inner.width, rowH);
            var trsRow = new Rect(inner.x, inner.y + rowH + RowGap,  inner.width, rowH);

            DrawBiBar(affRow, "SD_GizmoAff".Translate(), comp.affection,
                BarAffPos, BarAffNeg, comp.GetAffectionTooltip());

            DrawBiBar(trsRow, "SD_GizmoTrs".Translate(), comp.trust,
                BarTrsPos, BarTrsNeg, comp.GetTrustTooltip());

            return new GizmoResult(GizmoState.Clear);
        }

        /// <summary>
        /// 双向进度条：value ∈ [-1000, +1000]，0 居中，
        /// 正值向右填充 posColor，负值向左填充 negColor。
        /// </summary>
        private void DrawBiBar(Rect row, string label, float value,
            Texture2D posColor, Texture2D negColor, string tooltip)
        {
            // 标签
            var labelRect = new Rect(row.x, row.y, row.width * LabelPct, row.height);
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            // 进度条背景
            var bar = new Rect(labelRect.xMax, row.y + 2f, row.width - labelRect.width, row.height - 4f);
            GUI.DrawTexture(bar, BarBg);

            // 填充
            float mid   = bar.x + bar.width * 0.5f;
            float fillW = bar.width * 0.5f * Mathf.Abs(value) / 1000f;
            if (value >= 0f)
                GUI.DrawTexture(new Rect(mid, bar.y, fillW, bar.height), posColor);
            else
                GUI.DrawTexture(new Rect(mid - fillW, bar.y, fillW, bar.height), negColor);

            // 中轴线
            GUI.DrawTexture(new Rect(mid - 1f, bar.y, 2f, bar.height), BarCenterLine);

            // 数值文字
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font   = GameFont.Tiny;
            Widgets.Label(bar, value >= 0f ? $"+{value:F0}" : value.ToString("F0"));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font   = GameFont.Small;

            // 高亮 + Tooltip
            if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
                if (!tooltip.NullOrEmpty())
                    TooltipHandler.TipRegion(row, tooltip);
            }
        }
    }
}
