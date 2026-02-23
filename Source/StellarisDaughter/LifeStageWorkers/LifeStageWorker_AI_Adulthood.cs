using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 成年期Worker - 15-18岁
    /// 15岁时根据青年期积累的数值锁定结局路线
    /// </summary>
    // ✨ 沐雪写的哦~
    public class LifeStageWorker_AI_Adulthood : LifeStageWorker
    {
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);
            if (Current.ProgramState != ProgramState.Playing) return;

            UpdateBodyToAdult(pawn);

            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 核心：锁定结局路线
            comp.LockEndingRoute();

            if (PawnUtility.ShouldSendNotificationAbout(pawn))
                SendRouteLockedLetter(pawn, comp);

            comp.RecordEvent("Adulthood_Started",
                description: $"成年期开始，路线锁定为：{comp.lockedEnding}");
        }

        private void UpdateBodyToAdult(Pawn pawn)
        {
            if (pawn.story == null) return;
            BodyTypeDef adultBody = PawnGenerator.GetBodyTypeFor(pawn);
            if (pawn.story.bodyType != adultBody)
            {
                pawn.apparel?.DropAllOrMoveAllToInventory(
                    apparel => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult));
                pawn.story.bodyType = adultBody;
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        private void SendRouteLockedLetter(Pawn pawn, CompAIUpbringing comp)
        {
            string title;
            string text;
            LetterDef letterDef;

            if (comp.lockedEnding == AIEndingRoute.FatherBond)
            {
                title = "SD_Letter_RouteLocked_FatherBond_Title".Translate(pawn.NameShortColored);
                text = "SD_Letter_RouteLocked_FatherBond_Text".Translate(
                    pawn.NameShortColored,
                    comp.mentor?.NameShortColored ?? "SD_Unknown".Translate(),
                    comp.syncRate.ToString("F0"),
                    comp.chaosLevel.ToString("F0"));
                letterDef = LetterDefOf.PositiveEvent;
            }
            else
            {
                title = "SD_Letter_RouteLocked_DarkCorruption_Title".Translate(pawn.NameShortColored);
                text = "SD_Letter_RouteLocked_DarkCorruption_Text".Translate(
                    pawn.NameShortColored,
                    comp.syncRate.ToString("F0"),
                    comp.chaosLevel.ToString("F0"));
                letterDef = LetterDefOf.NegativeEvent;
            }

            Find.LetterStack.ReceiveLetter(title, text, letterDef, pawn);
        }
    }
}
