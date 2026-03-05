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
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Forbidden", true);
                Disable("SD_Witch_CannotToggle_Berserk".Translate());
            }
            else if (comp.IsWitchForm)
            {
                defaultLabel = "SD_Witch_CancelTransform".Translate();
                defaultDesc = "SD_Witch_CancelTransform_Desc".Translate();
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CancelConstruction", true);
                action = () => comp.ToggleWitchForm();
            }
            else
            {
                defaultLabel = "SD_Witch_Transform".Translate();
                defaultDesc = "SD_Witch_Transform_Desc".Translate();
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true);
                action = () => comp.ToggleWitchForm();
            }

            // Ability风格的Order
            Order = 5f;
        }


    }
}
