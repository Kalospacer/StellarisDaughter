using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 青年期Worker - 8-14岁（懵懂期）
    /// 无显式分支，数值根据玩家行为自然分化
    /// 13-14岁时发送征兆信件暗示走向
    /// </summary>
    // ✨ 沐雪写的哦~
    public class LifeStageWorker_AI_Youth : LifeStageWorker
    {
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);

            // 身体成长：必须在 ProgramState 检查前执行，CharacterEditor 也需要正确体型
            UpdateBodyType(pawn);

            if (Current.ProgramState != ProgramState.Playing) return;

            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 发送青年期通知（无选择）
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                string title = "SD_Letter_YouthStarted_Title".Translate(pawn.NameShortColored);
                string text = "SD_Letter_YouthStarted_Text".Translate(
                    pawn.NameShortColored,
                    comp.affection.ToString("F1"),
                    comp.trust.ToString("F1"));
                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent, pawn);
            }

            // 青年期开始，细微好感加成
            comp.Apply(5f, 5f, "进入青年期");
        }

        private void UpdateBodyType(Pawn pawn)
        {
            if (pawn.story == null) return;
            BodyTypeDef youthBody = BodyTypeDefOf.Thin;
            if (pawn.story.bodyType == youthBody) return;
            // 服装掉落只在游戏中执行
            if (Current.ProgramState == ProgramState.Playing)
                pawn.apparel?.DropAllOrMoveAllToInventory(
                    apparel => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child));
            pawn.story.bodyType = youthBody;
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}
