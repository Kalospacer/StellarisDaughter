using System.Linq;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 最终结局Worker - 18岁+
    /// 触发最终结局判定，应用奖励/惩罚
    /// </summary>
    // ✨ 沐雪写的哦~
    public class LifeStageWorker_AI_Final : LifeStageWorker
    {
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);
            if (Current.ProgramState != ProgramState.Playing) return;

            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 兜底锁定
            if (comp.lockedEnding == AIEndingRoute.NotYetDetermined)
                comp.LockEndingRoute();

            var ending = ResolveEnding(comp);
            if (ending != null)
                TriggerEnding(pawn, comp, ending);
            else
                Log.Error("[StellarisDaughter] 无法找到结局定义！");
        }

        private AIEndingDef ResolveEnding(CompAIUpbringing comp)
        {
            var allEndings = DefDatabase<AIEndingDef>.AllDefsListForReading;

            // 按优先级匹配
            foreach (var ending in allEndings.OrderByDescending(e => e.priority))
            {
                if (ending.MeetsConditions(comp))
                    return ending;
            }

            // 回退默认
            string target = comp.lockedEnding == AIEndingRoute.FatherBond
                ? "SD_Ending_FatherBond"
                : "SD_Ending_DarkCorruption";
            return allEndings.FirstOrDefault(e => e.defName == target);
        }

        private void TriggerEnding(Pawn pawn, CompAIUpbringing comp, AIEndingDef ending)
        {
            ending.ApplyRewards(pawn);

            string title = ending.endingTitle.Translate(pawn.NameShortColored);
            string text = ending.GetEndingText(pawn, comp);
            LetterDef letterDef = ending.isPositive ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent;

            Find.LetterStack.ReceiveLetter(title, text, letterDef, pawn);

            if (ending.leavesColony)
            {
                // TODO: AI女儿离开殖民地的具体实现
                Log.Message($"[StellarisDaughter] {pawn.LabelShort} 离开了殖民地（黑化恶堕）");
            }
        }
    }
}
