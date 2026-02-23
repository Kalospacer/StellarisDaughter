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

            // 体型切换必须在 ProgramState 检查前，CharacterEditor 也需要正确渲染
            EnsureAdultBodyType(pawn);

            if (Current.ProgramState != ProgramState.Playing) return;

            // 只在游戏中才掉落儿童服装
            pawn.apparel?.DropAllOrMoveAllToInventory(
                apparel => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult));

            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 核心：锁定结局路线
            comp.LockEndingRoute();

            if (PawnUtility.ShouldSendNotificationAbout(pawn))
                SendRouteLockedLetter(pawn, comp);
        }

        private void EnsureAdultBodyType(Pawn pawn)
        {
            if (pawn.story == null) return;
            if (pawn.story.bodyType == BodyTypeDefOf.Thin) return;
            pawn.story.bodyType = BodyTypeDefOf.Thin;
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
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
                    comp.affection.ToString("F1"),
                    comp.trust.ToString("F1"));
                letterDef = LetterDefOf.PositiveEvent;
            }
            else
            {
                title = "SD_Letter_RouteLocked_DarkCorruption_Title".Translate(pawn.NameShortColored);
                text = "SD_Letter_RouteLocked_DarkCorruption_Text".Translate(
                    pawn.NameShortColored,
                    comp.affection.ToString("F1"),
                    comp.trust.ToString("F1"));
                letterDef = LetterDefOf.NegativeEvent;
            }

            Find.LetterStack.ReceiveLetter(title, text, letterDef, pawn);
        }
    }
}
