using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 魔女变身切换按钮 - Ability风格
    /// </summary>
    [StaticConstructorOnStartup]
    public class Command_ToggleWitchForm : Command_Action
    {
        private readonly CompAIWitchForm comp;

        public Command_ToggleWitchForm(CompAIWitchForm comp)
        {
            this.comp = comp;

            // 根据状态设置图标和标签
            if (comp.IsBerserk)
            {
                defaultLabel = "SD_Witch_BerserkWarning".Translate();
                defaultDesc = "SD_Witch_CannotToggle_Berserk".Translate();
                icon = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOn", true);
                Disable("SD_Witch_CannotToggle_Berserk".Translate());
            }
            else if (comp.IsWitchForm)
            {
                defaultLabel = "SD_Witch_CancelTransform".Translate();
                defaultDesc = "SD_Witch_CancelTransform_Desc".Translate();
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
                action = () => comp.ToggleWitchForm();
            }
            else
            {
                defaultLabel = "SD_Witch_Transform".Translate();
                defaultDesc = "SD_Witch_Transform_Desc".Translate();
                icon = ContentFinder<Texture2D>.Get("StellarisDaughter/UI/Commands/WitchTransform", true);
                action = () => comp.ToggleWitchForm();
            }

            var cooldownDesc = comp.GetCurrentToggleCooldownDescription();
            if (!string.IsNullOrEmpty(cooldownDesc))
            {
                defaultDesc = string.IsNullOrEmpty(defaultDesc)
                    ? cooldownDesc
                    : defaultDesc + "\n\n" + cooldownDesc;
            }

            if (!comp.CanToggleWitchFormNow(out var disableReason))
            {
                Disable(disableReason);
            }

            // Ability风格的Order
            Order = 5f;
        }
    }
}
