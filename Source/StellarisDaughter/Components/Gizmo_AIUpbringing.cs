using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~

    /// <summary>
    /// 仿原版成长评级，以三行进度条格式展示 AI 女儿的核心数值。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_AIUpbringing : Gizmo
    {
        // ── 颜色 ───────────────────────────────────────────
        private static readonly Texture2D BarEmpty =
            SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);

        private static readonly Texture2D BarSync =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.15f, 0.72f, 0.85f)); // 青色

        private static readonly Texture2D BarChaos =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.15f, 0.20f)); // 红色

        private static readonly Texture2D BarAwaken =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.90f, 0.70f, 0.15f)); // 金色

        // ── 布局 ──────────────────────────────────────────
        private const float GizmoWidth     = 220f;
        private const float GizmoHeight    = 105f;
        private const float Padding        = 8f;
        private const float LabelPct       = 0.42f;  // 标签占行宽比例
        private const float RowSpacing     = 2f;

        private readonly CompAIUpbringing comp;

        public Gizmo_AIUpbringing(CompAIUpbringing comp)
        {
            this.comp = comp;
            Order = -99f; // 紧跟在成长评级之后
        }

        public override float GetWidth(float maxWidth) => GizmoWidth;

        public override bool Visible
        {
            get
            {
                var pawn = comp.parent as Pawn;
                if (pawn == null) return false;
                return pawn.IsColonistPlayerControlled
                    || pawn.IsPrisonerOfColony
                    || pawn.IsSlaveOfColony;
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outer = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);
            Widgets.DrawWindowBackground(outer);

            Rect inner = outer.ContractedBy(Padding);
            float rowH = inner.height / 3f;

            Rect row1 = new Rect(inner.x, inner.y,              inner.width, rowH);
            Rect row2 = new Rect(inner.x, inner.y + rowH,       inner.width, rowH);
            Rect row3 = new Rect(inner.x, inner.y + rowH * 2f,  inner.width, rowH);

            row1.yMax -= RowSpacing;
            row2.yMin += RowSpacing; row2.yMax -= RowSpacing;
            row3.yMin += RowSpacing;

            DrawBar(row1, "SD_GizmoSync".Translate(),    comp.syncRate / 100f,         BarSync,
                tooltip: "SD_GizmoSync_Tip".Translate(comp.syncRate.ToString("F0")));

            DrawBar(row2, "SD_GizmoChaos".Translate(),   comp.chaosLevel / 100f,       BarChaos,
                tooltip: "SD_GizmoChaos_Tip".Translate(comp.chaosLevel.ToString("F0")));

            DrawBar(row3, "SD_GizmoAwaken".Translate(),  comp.awakeningProgress / 100f, BarAwaken,
                tooltip: "SD_GizmoAwaken_Tip".Translate(comp.awakeningProgress.ToString("F0"), comp.CurrentStageDescription));

            return new GizmoResult(GizmoState.Clear);
        }

        // ── 辅助：渲染一行（标签 + 进度条 + 百分比文字）─────────────
        private static void DrawBar(Rect row, string label, float pct, Texture2D barTex, string tooltip = null)
        {
            // 左侧标签
            Rect labelRect = new Rect(row.x, row.y, row.width * LabelPct, row.height);
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);

            // 右侧进度条
            Rect barRect = new Rect(labelRect.xMax, row.y, row.width - labelRect.width, row.height);
            barRect.yMin += 2f;
            barRect.yMax -= 2f;
            Widgets.FillableBar(barRect, pct, barTex, BarEmpty, doBorder: true);

            // 进度条上居中显示百分比
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, pct.ToStringPercent("F0"));
            Text.Anchor = TextAnchor.UpperLeft;

            // Tooltip
            if (!tooltip.NullOrEmpty() && Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
                TooltipHandler.TipRegion(row, tooltip);
            }
        }
    }
}
