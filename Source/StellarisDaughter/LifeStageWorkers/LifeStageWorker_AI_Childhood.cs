using System.Linq;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 童年期Worker - 0-7岁
    /// 所有路线统一体验：认知学习，建立初始联系
    /// </summary>
    // ✨ 沐雪写的哦~
    public class LifeStageWorker_AI_Childhood : LifeStageWorker
    {
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);

            // 童年体型：必须在 ProgramState 检查前执行，CharacterEditor 也需要
            if (pawn.story != null && pawn.story.bodyType != BodyTypeDefOf.Child)
            {
                pawn.story.bodyType = BodyTypeDefOf.Child;
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }

            if (Current.ProgramState != ProgramState.Playing) return;

            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null)
            {
                Log.Warning("[StellarisDaughter] AI女儿缺少CompAIUpbringing组件");
                return;
            }

            // 首次初始化
            if (previousLifeStage == null)
            {
                InitializeAIDaughter(pawn, comp);
            }
        }

        private void InitializeAIDaughter(Pawn pawn, CompAIUpbringing comp)
        {
            // 初始激活，给予微弱的初始好感
            comp.Apply(10f, 10f, "初次激活");

            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                string title = "SD_Letter_ChildhoodStarted_Title".Translate();
                string text = "SD_Letter_ChildhoodStarted_Text".Translate(
                    pawn.NameFullColored);
                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, pawn);
            }

            // 添加好奇特性
            var curiousDef = DefDatabase<TraitDef>.GetNamed("SD_Curious", false);
            if (curiousDef != null && pawn.story?.traits != null)
                pawn.story.traits.GainTrait(new Trait(curiousDef));
        }
    }
}
