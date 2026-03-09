using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// Dual-axis gizmo that displays affection and trust in the range [-1000, 1000].
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_AIUpbringing : Gizmo
    {
        private static readonly Texture2D BarBg =
            SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);

        private static readonly Texture2D BarAffPos =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.90f, 0.50f, 0.70f));

        private static readonly Texture2D BarAffNeg =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.70f, 0.20f, 0.30f));

        private static readonly Texture2D BarTrsPos =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.30f, 0.65f, 0.90f));

        private static readonly Texture2D BarTrsNeg =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.30f, 0.60f));

        private static readonly Texture2D BarCenterLine =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.6f, 0.6f, 0.6f, 0.9f));

        private const float Width = 220f;
        private const float GizmoHeight = 75f;
        private const float Pad = 4f;
        private const float LabelPct = 0.28f;
        private const float RowGap = 3f;

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
                var pawn = comp.parent as Pawn;
                return pawn != null && (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony);
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var outer = new Rect(topLeft.x, topLeft.y, Width, GizmoHeight);
            Widgets.DrawWindowBackground(outer);

            var inner = outer.ContractedBy(Pad);
            float rowHeight = (inner.height - RowGap) / 2f;

            var affRow = new Rect(inner.x, inner.y, inner.width, rowHeight);
            var trsRow = new Rect(inner.x, inner.y + rowHeight + RowGap, inner.width, rowHeight);

            DrawBiBar(affRow, "SD_GizmoAff".Translate(), comp.affection, BarAffPos, BarAffNeg, comp.GetAffectionTooltip());
            DrawBiBar(trsRow, "SD_GizmoTrs".Translate(), comp.trust, BarTrsPos, BarTrsNeg, comp.GetTrustTooltip());

            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawBiBar(Rect row, string label, float value, Texture2D posColor, Texture2D negColor, string tooltip)
        {
            var labelRect = new Rect(row.x, row.y, row.width * LabelPct, row.height);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            var barRect = new Rect(labelRect.xMax, row.y + 2f, row.width - labelRect.width, row.height - 4f);
            GUI.DrawTexture(barRect, BarBg);

            float mid = barRect.x + barRect.width * 0.5f;
            float fillWidth = barRect.width * 0.5f * Mathf.Abs(value) / 1000f;
            if (value >= 0f)
                GUI.DrawTexture(new Rect(mid, barRect.y, fillWidth, barRect.height), posColor);
            else
                GUI.DrawTexture(new Rect(mid - fillWidth, barRect.y, fillWidth, barRect.height), negColor);

            GUI.DrawTexture(new Rect(mid - 1f, barRect.y, 2f, barRect.height), BarCenterLine);
            DrawScaleMarks(barRect, mid);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(barRect, value >= 0f ? $"+{value:F0}" : value.ToString("F0"));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
                if (!tooltip.NullOrEmpty())
                    TooltipHandler.TipRegion(row, tooltip);
            }
        }

        private void DrawScaleMarks(Rect barRect, float mid)
        {
            var markColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            var oldColor = GUI.color;
            GUI.color = markColor;

            float halfWidth = barRect.width * 0.5f;
            float mark500L = mid - halfWidth * 0.5f;
            float mark500R = mid + halfWidth * 0.5f;

            GUI.DrawTexture(new Rect(mark500L - 0.5f, barRect.y, 1f, barRect.height * 0.3f), BarCenterLine);
            GUI.DrawTexture(new Rect(mark500R - 0.5f, barRect.y, 1f, barRect.height * 0.3f), BarCenterLine);
            GUI.DrawTexture(new Rect(barRect.x + 1f, barRect.y, 1f, barRect.height * 0.25f), BarCenterLine);
            GUI.DrawTexture(new Rect(barRect.xMax - 2f, barRect.y, 1f, barRect.height * 0.25f), BarCenterLine);

            GUI.color = oldColor;
        }
    }
}
