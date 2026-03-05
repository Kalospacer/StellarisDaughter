using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 魔女化Gizmo - 显示魔女因子条和变身按钮
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_AIWitchForm : Gizmo
    {
        private static readonly Texture2D BarBg = SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);
        private static readonly Texture2D BarFillNormal = SolidColorMaterials.NewSolidColorTexture(new Color(0.3f, 0.6f, 0.9f));
        private static readonly Texture2D BarFillWarning = SolidColorMaterials.NewSolidColorTexture(new Color(0.9f, 0.6f, 0.3f));
        private static readonly Texture2D BarFillDanger = SolidColorMaterials.NewSolidColorTexture(new Color(0.9f, 0.3f, 0.3f));

        private const float Width = 220f;
        private const float GizmoHeight = 85f;
        private const float Pad = 4f;

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

            // 状态标签
            var stateRect = new Rect(inner.x, inner.y, inner.width, 20f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            string stateLabel = comp.state switch
            {
                WitchFormState.Normal => "SD_Witch_State_Normal".Translate(),
                WitchFormState.WitchForm => "SD_Witch_State_Witch".Translate(),
                WitchFormState.Berserk => "SD_Witch_State_Berserk".Translate(),
                _ => "Unknown"
            };

            GUI.color = comp.IsBerserk ? Color.red : (comp.IsWitchForm ? new Color(0.8f, 0.4f, 0.8f) : Color.white);
            Widgets.Label(stateRect, stateLabel);
            GUI.color = Color.white;

            // 魔女因子条
            var barRect = new Rect(inner.x, inner.y + 25f, inner.width, 25f);
            DrawWitchFactorBar(barRect);

            // 变身按钮
            var buttonRect = new Rect(inner.x, inner.y + 55f, inner.width, 22f);
            DrawToggleButton(buttonRect);

            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawWitchFactorBar(Rect rect)
        {
            float max = comp.MaxWitchFactor;
            float current = comp.witchFactor;
            float fillPct = Mathf.Clamp01(current / max);

            // 背景
            GUI.DrawTexture(rect, BarBg);

            // 填充颜色根据数值变化
            Texture2D fillTex;
            if (current >= max * 0.9f)
                fillTex = BarFillDanger;
            else if (current >= max * 0.5f)
                fillTex = BarFillWarning;
            else
                fillTex = BarFillNormal;

            // 填充
            var fillRect = new Rect(rect.x, rect.y, rect.width * fillPct, rect.height);
            GUI.DrawTexture(fillRect, fillTex);

            // 50%标记线
            float halfX = rect.x + rect.width * 0.5f;
            GUI.DrawTexture(new Rect(halfX - 1f, rect.y, 2f, rect.height), BaseContent.BlackTex);

            // 数值文字
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            Widgets.Label(rect, $"{current:F0} / {max:F0}");
        }

        private void DrawToggleButton(Rect rect)
        {
            if (comp.IsBerserk)
            {
                // 发狂状态下显示警告
                GUI.color = new Color(1f, 0.5f, 0.5f, 0.5f);
                Widgets.DrawBox(rect);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "SD_Witch_BerserkWarning".Translate());
                return;
            }

            string buttonLabel = comp.IsWitchForm
                ? "SD_Witch_CancelTransform".Translate()
                : "SD_Witch_Transform".Translate();

            if (Widgets.ButtonText(rect, buttonLabel))
            {
                comp.ToggleWitchForm();
            }
        }
    }
}
