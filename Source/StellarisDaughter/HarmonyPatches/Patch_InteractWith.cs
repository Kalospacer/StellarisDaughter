using HarmonyLib;
using RimWorld;
using Verse;

namespace StellarisDaughter.HarmonyPatches
{
    /// <summary>
    /// 拦截互动事件：所有人与AI女儿互动都增加同步率，指定监护人互动加成更多
    /// </summary>
    // ✨ 沐雪写的哦~
    // 互动事件现在通过 TickRare 扫描原版记忆思绪（AIEventResponseDef）驱动，此Patch已废弃
    // [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
    public static class Patch_InteractWith
    {
    }
}
