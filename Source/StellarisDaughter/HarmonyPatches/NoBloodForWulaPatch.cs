using HarmonyLib;
using Verse;

namespace StellarisDaughter
{
    // 改为 Patch 基类 Hediff，这样能同时覆盖 Hediff_Injury（伤口）和 Hediff_MissingPart（断肢）
    [HarmonyPatch(typeof(Hediff), nameof(Hediff.BleedRate), MethodType.Getter)]
    public static class NoBloodForDaughter_BleedRate_Patch
    {
        public static void Postfix(Hediff __instance, ref float __result)
        {
            // 增加空值检查，防止红字报错
            if (__instance?.pawn?.def?.defName == "SD_AI_Daughter_Race")
            {
                __result = 0f;
            }
        }
    }
}
