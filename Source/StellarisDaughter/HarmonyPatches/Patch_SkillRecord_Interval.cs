using HarmonyLib;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Interval))]
    public static class Patch_SkillRecord_Interval
    {
        [HarmonyPrefix]
        public static bool Prefix(SkillRecord __instance)
        {
            Pawn pawn = __instance?.Pawn;
            if (pawn?.story?.traits == null) return true;

            // 原版 PerfectMemory 的效果是跳过技能衰减；这里让 SD_MemoryCore 复用同等行为。
            if (SD_DefOf.SD_MemoryCore != null && pawn.story.traits.HasTrait(SD_DefOf.SD_MemoryCore))
                return false;

            return true;
        }
    }
}
