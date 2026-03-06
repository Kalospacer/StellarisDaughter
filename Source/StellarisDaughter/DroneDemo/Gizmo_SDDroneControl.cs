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

        private const float Width = 320f;
        private const float BaseGizmoHeight = 75f;
        private const float TopHeight = 46f;
        private const float SlotHeight = 20f;
        private const float MaxVisibleSlotArea = 124f;
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
            var fullSlotHeight = slots.Count * SlotHeight;
            var visibleSlotHeight = Mathf.Min(fullSlotHeight, MaxVisibleSlotArea);
            var height = Padding * 2f + TopHeight + Gap + visibleSlotHeight;
            var shiftUp = Mathf.Max(0f, height - BaseGizmoHeight);
            var outer = new Rect(topLeft.x, Mathf.Max(0f, topLeft.y - shiftUp), Width, height);
            Widgets.DrawWindowBackground(outer);

            var inner = outer.ContractedBy(Padding);
            DrawTopBlock(new Rect(inner.x, inner.y, inner.width, TopHeight));

            var listOutRect = new Rect(inner.x, inner.y + TopHeight + Gap, inner.width, visibleSlotHeight);
            DrawSlotList(listOutRect, slots);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawTopBlock(Rect rect)
        {
            var titleRect = new Rect(rect.x, rect.y, rect.width, 16f);
            var summaryRect = new Rect(rect.x, titleRect.yMax, rect.width, 12f);
            var buttonsRect = new Rect(rect.x, summaryRect.yMax + 4f, rect.width, rect.yMax - summaryRect.yMax - 4f);
            var actionRect = new Rect(buttonsRect.x, buttonsRect.y, 120f, buttonsRect.height);
            var toggleRect = new Rect(actionRect.xMax + 4f, buttonsRect.y, buttonsRect.width - actionRect.width - 4f, buttonsRect.height);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleRect, "SD_Drone_GizmoTitle".Translate());

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.82f, 0.82f, 0.82f);
            Widgets.Label(summaryRect, BuildSummaryText());
            GUI.color = Color.white;

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
            Text.Font = GameFont.Small;
        }

        private void DrawSlotRow(Rect row, SD_DroneSlot slot)
        {
            var clickable = row.ContractedBy(1f);
            Widgets.DrawHighlightIfMouseover(clickable);

            var indexRect = new Rect(clickable.x, clickable.y, 24f, clickable.height);
            var labelRect = new Rect(indexRect.xMax + 4f, clickable.y, 112f, clickable.height);
            var countRect = new Rect(labelRect.xMax + 4f, clickable.y, 30f, clickable.height);
            var statusRect = new Rect(countRect.xMax + 4f, clickable.y, 42f, clickable.height);
            var barRect = new Rect(statusRect.xMax + 5f, clickable.y + 3f, clickable.xMax - statusRect.xMax - 5f, clickable.height - 6f);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(indexRect, $"#{slot.Index + 1}");
            Widgets.Label(labelRect, ShortenLabel(ResolveDroneTypeLabel(slot), 16));
            Widgets.Label(countRect, $"{controller.GetActiveDroneCount(slot)}/{controller.GetSquadronSize(slot)}");

            GUI.color = ResolveStateColor(slot);
            Widgets.Label(statusRect, ShortenLabel(controller.GetSlotShortState(slot), 5));
            GUI.color = Color.white;

            DrawChargeBar(barRect, slot);

            if (Widgets.ButtonInvisible(clickable))
            {
                controller.ToggleSlot(slot.Index);
            }

            TooltipHandler.TipRegion(clickable, controller.BuildSlotDescription(slot));
        }

        private void DrawSlotList(Rect outRect, System.Collections.Generic.IReadOnlyList<SD_DroneSlot> slots)
        {
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, slots.Count * SlotHeight);
            var scrollPosition = new Vector2(0f, controller.GizmoScrollPosition);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            for (var i = 0; i < slots.Count; i++)
            {
                var row = new Rect(0f, i * SlotHeight, viewRect.width, SlotHeight - 1f);
                DrawSlotRow(row, slots[i]);
            }

            Widgets.EndScrollView();
            controller.GizmoScrollPosition = scrollPosition.y;
        }

        private string BuildSummaryText()
        {
            var ready = 0;
            var active = 0;
            var charging = 0;
            var slots = controller.Slots;

            for (var i = 0; i < slots.Count; i++)
            {
                if (controller.GetActiveDroneCount(slots[i]) > 0)
                {
                    active++;
                }
                else if (slots[i].IsCharged)
                {
                    ready++;
                }
                else if (slots[i].State == SD_DroneSlotState.Charging)
                {
                    charging++;
                }
            }

            return "SD_Drone_GizmoSummary".Translate(ready, active, charging).ToString();
        }

        private static string ResolveDroneTypeLabel(SD_DroneSlot slot)
        {
            return slot.DroneType?.label ?? slot.DroneType?.defName ?? "None";
        }

        private static string ShortenLabel(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            {
                return text;
            }

            if (maxChars <= 3)
            {
                return text.Substring(0, maxChars);
            }

            return text.Substring(0, maxChars - 3) + "...";
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
                : slot.IsCharged ? 1f : slot.ActiveDroneCount > 0 ? slot.ActiveDroneCount / (float)Mathf.Max(slot.SquadronSize, 1) : 0f;

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

            if (slot.ActiveDroneCount > 0)
            {
                return FillActive;
            }

            return FillReady;
        }
    }
}
