using RimWorld;
using Verse;
using Verse.AI;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 魔女狂暴精神状态 - 不会自动结束，只能通过镇压解除
    /// 使用原版袭击逻辑，通过设置Duty来实现攻击行为
    /// </summary>
    public class MentalState_WitchBerserk : MentalState
    {
        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }

        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            // 设置攻击殖民地的Duty，使用原版袭击逻辑
            if (pawn.mindState != null)
            {
                pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
            }
        }

        public override bool ForceHostileTo(Thing t)
        {
            // 对所有生物都敌对
            return t is Pawn && t != pawn;
        }

        public override bool ForceHostileTo(Faction f)
        {
            // 对所有派系都敌对
            return true;
        }
    }
}
