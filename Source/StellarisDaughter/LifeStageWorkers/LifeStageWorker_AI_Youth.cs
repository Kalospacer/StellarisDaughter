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
            if (Current.ProgramState != ProgramState.Playing) return;

            // 身体成长
            UpdateBodyType(pawn);

            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 发送青年期通知（无选择）
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                string title = "SD_Letter_YouthStarted_Title".Translate(pawn.NameShortColored);
                string text = "SD_Letter_YouthStarted_Text".Translate(
                    pawn.NameShortColored,
                    comp.syncRate.ToString("F0"),
                    comp.chaosLevel.ToString("F0"));
                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent, pawn);
            }

            comp.RecordEvent("Youth_Started", syncChange: 5f,
                description: "进入青年懵懂期，暗流涌动");
        }

        private void UpdateBodyType(Pawn pawn)
        {
            if (pawn.story == null) return;
            // 青年阶段使用 SD_Youth 体型，贴图路径：Naked_SD_Youth_*.png
            BodyTypeDef youthBody = DefDatabase<BodyTypeDef>.GetNamed("SD_Youth", errorOnFail: false)
                ?? BodyTypeDefOf.Thin; // 安全回退
            if (pawn.story.bodyType == youthBody) return;
            pawn.apparel?.DropAllOrMoveAllToInventory(
                apparel => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child));
            pawn.story.bodyType = youthBody;
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }
    }
}
