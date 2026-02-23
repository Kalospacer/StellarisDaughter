using HarmonyLib;
using RimWorld;
using Verse;

namespace StellarisDaughter.HarmonyPatches
{
    /// <summary>
    /// 拦截互动事件：当引导者与AI女儿互动时增加同步率
    /// </summary>
    // ✨ 沐雪写的哦~
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
    public static class Patch_InteractWith
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn ___pawn, Pawn recipient, bool __result)
        {
            if (!__result) return;

            var comp = recipient.GetComp<CompAIUpbringing>();
            if (comp != null && comp.mentor == ___pawn)
            {
                comp.RecordEvent("PositiveInteraction", syncChange: 1f);
            }

            // 反方向：AI主动与引导者互动
            var compInit = ___pawn.GetComp<CompAIUpbringing>();
            if (compInit != null && compInit.mentor == recipient)
            {
                compInit.RecordEvent("PositiveInteraction", syncChange: 0.5f);
            }
        }
    }
}
