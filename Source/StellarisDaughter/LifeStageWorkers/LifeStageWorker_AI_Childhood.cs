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
            if (comp.mentor == null)
                DetermineMentor(pawn, comp);

            comp.RecordEvent("Childhood_Initialized", syncChange: 10f,
                description: "星规子单元被激活，开始认知学习");

            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                string title = "SD_Letter_ChildhoodStarted_Title".Translate();
                string text = "SD_Letter_ChildhoodStarted_Text".Translate(
                    pawn.NameFullColored,
                    comp.mentor?.NameFullColored ?? "SD_Unknown".Translate());
                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, pawn);
            }

            // 添加好奇特性
            var curiousDef = DefDatabase<TraitDef>.GetNamed("SD_Curious", false);
            if (curiousDef != null && pawn.story?.traits != null)
                pawn.story.traits.GainTrait(new Trait(curiousDef));
        }

        private void DetermineMentor(Pawn ai, CompAIUpbringing comp)
        {
            if (ai.Faction == null) return;

            var leader = ai.Faction.leader;
            if (leader != null && !leader.Dead && leader.Map == ai.Map)
            {
                comp.mentor = leader;
                return;
            }

            if (ai.Map != null)
            {
                comp.mentor = ai.Map.mapPawns.FreeColonists
                    .Where(p => p != ai && !p.Dead)
                    .OrderBy(p => p.Position.DistanceToSquared(ai.Position))
                    .FirstOrDefault();
            }
        }
    }
}
