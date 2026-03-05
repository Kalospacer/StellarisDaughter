using RimWorld;
using Verse;
using Verse.AI;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 魔女狂暴精神状态 - 不会自动结束，只能通过镇压解除
    /// 完全参考原版 Berserk 实现，对所有目标都敌对
    /// </summary>
    public class MentalState_WitchBerserk : MentalState
    {
        public override bool ForceHostileTo(Thing t)
        {
            // 对所有东西都敌对（原版实现）
            return true;
        }

        public override bool ForceHostileTo(Faction f)
        {
            // 对所有派系都敌对（原版实现）
            return true;
        }

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}
