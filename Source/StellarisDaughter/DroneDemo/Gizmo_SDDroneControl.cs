using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    [StaticConstructorOnStartup]
    public class Gizmo_SDDroneControl : Gizmo
    {
        private static readonly Texture2D FillBg = SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);
        private static readonly Texture2D FillReady = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.75f, 0.95f));
        private static readonly Texture2D FillCharging = SolidColorMaterials.NewSolidColorTexture(new Color(0.95f, 0.7f, 0.3f));
        private static readonly Texture2D FillActive = SolidColorMaterials.NewSolidColorTexture(new Color(0.95f, 0.4f, 0.55f));

        private const float Width = 220f;
        private const float TopHeight = 28f;
        private const float SlotHeight = 18f;
        private const float Padding = 6f;
        private const float Gap = 4f;

        private readonly CompSD_DroneController controller;

        public Gizmo_SDDroneControl(CompSD_DroneController controller)
        {
            this.controller = controller;
            Order = -97f;
        }

        public override float GetWidth(float maxWidth)
        {
            return Width;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var slots = controller.Slots;
            var height = Padding * 2f + TopHeight + Gap + slots.Count * SlotHeight;
            var outer = new Rect(topLeft.x, topLeft.y, Width, height);
            Widgets.DrawWindowBackground(outer);

            var inner = outer.ContractedBy(Padding);
            var topRow = new Rect(inner.x, inner.y, inner.width, TopHeight);
            DrawTopRow(topRow);

            var rowY = topRow.yMax + Gap;
            for (var i = 0; i < slots.Count; i++)
            {
                var row = new Rect(inner.x, rowY + i * SlotHeight, inner.width, SlotHeight - 1f);
                DrawSlotRow(row, slots[i]);
            }

            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawTopRow(Rect row)
        {
            var titleRect = new Rect(row.x, row.y, 70f, row.height);
            var actionRect = new Rect(titleRect.xMax + 4f, row.y + 2f, 74f, row.height - 4f);
            var toggleRect = new Rect(actionRect.xMax + 4f, row.y + 2f, row.xMax - (actionRect.xMax + 4f), row.height - 4f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleRect, "SD_Drone_GizmoTitle".Translate());

            var actionLabel = controller.HasActiveDrones()
                ? "SD_Drone_RecallCommandLabel".Translate().ToString()
                : "SD_Drone_DeployCommandLabel".Translate().ToString();
            if (Widgets.ButtonText(actionRect, actionLabel))
            {
                controller.ToggleAllDrones();
            }

            var autoLabel = controller.AutoDeployEnabled
                ? "SD_Drone_GizmoAutoOn".Translate().ToString()
                : "SD_Drone_GizmoAutoOff".Translate().ToString();
            if (Widgets.ButtonText(toggleRect, autoLabel))
            {
                controller.ToggleAutoDeploy();
            }

            TooltipHandler.TipRegion(toggleRect, "SD_Drone_AutoDeployToggleDesc".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawSlotRow(Rect row, SD_DroneSlot slot)
        {
            var clickable = row.ContractedBy(1f);
            Widgets.DrawHighlightIfMouseover(clickable);

            var indexRect = new Rect(clickable.x, clickable.y, 22f, clickable.height);
            var labelRect = new Rect(indexRect.xMax + 2f, clickable.y, 70f, clickable.height);
            var statusRect = new Rect(labelRect.xMax + 2f, clickable.y, 48f, clickable.height);
            var barRect = new Rect(statusRect.xMax + 4f, clickable.y + 3f, clickable.xMax - statusRect.xMax - 4f, clickable.height - 6f);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(indexRect, $"#{slot.Index + 1}");
            Widgets.Label(labelRect, ResolveDroneTypeLabel(slot));

            var stateText = controller.GetSlotShortState(slot).ToString();
            GUI.color = ResolveStateColor(slot);
            Widgets.Label(statusRect, stateText);
            GUI.color = Color.white;

            DrawChargeBar(barRect, slot);

            if (Widgets.ButtonInvisible(clickable))
            {
                controller.ToggleSlot(slot.Index);
            }

            TooltipHandler.TipRegion(clickable, controller.BuildSlotDescription(slot));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static string ResolveDroneTypeLabel(SD_DroneSlot slot)
        {
            return slot.DroneType?.label ?? slot.DroneType?.defName ?? "None";
        }

        private static Color ResolveStateColor(SD_DroneSlot slot)
        {
            switch (slot.State)
            {
                case SD_DroneSlotState.Deployed:
                    return new Color(0.95f, 0.45f, 0.55f);
                case SD_DroneSlotState.Returning:
                    return new Color(0.95f, 0.75f, 0.3f);
                case SD_DroneSlotState.Charging:
                    return new Color(0.95f, 0.75f, 0.3f);
                case SD_DroneSlotState.Docked:
                    return slot.IsCharged ? new Color(0.35f, 0.8f, 0.95f) : Color.gray;
                default:
                    return Color.gray;
            }
        }

        private static void DrawChargeBar(Rect rect, SD_DroneSlot slot)
        {
            GUI.DrawTexture(rect, FillBg);
            Widgets.DrawBox(rect);

            var fillPercent = slot.State == SD_DroneSlotState.Charging && slot.ChargeTicksTotal > 0
                ? 1f - Mathf.Clamp01(slot.ChargeTicksRemaining / (float)slot.ChargeTicksTotal)
                : slot.IsCharged || slot.Drone != null ? 1f : 0f;

            if (fillPercent > 0f)
            {
                var fillRect = rect.ContractedBy(1f);
                fillRect.width *= fillPercent;
                GUI.DrawTexture(fillRect, ResolveFillTexture(slot));
            }
        }

        private static Texture2D ResolveFillTexture(SD_DroneSlot slot)
        {
            if (slot.State == SD_DroneSlotState.Charging)
            {
                return FillCharging;
            }

            if (slot.Drone != null && !slot.Drone.Destroyed)
            {
                return FillActive;
            }

            return FillReady;
        }
    }
}
