using HarmonyLib;
using Verse;

namespace StellarisDaughter
{
    [HarmonyPatch(typeof(Hediff_Injury), nameof(Hediff_Injury.BleedRate), MethodType.Getter)]
    public static class NoBloodForDaughter_InjuryBleedRate_Patch
    {
        public static void Postfix(Hediff_Injury __instance, ref float __result)
        {
            if (__instance?.pawn?.def?.defName == "SD_AI_Daughter_Race")
            {
                __result = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(Hediff_MissingPart), nameof(Hediff_MissingPart.BleedRate), MethodType.Getter)]
    public static class NoBloodForDaughter_MissingPartBleedRate_Patch
    {
        public static void Postfix(Hediff_MissingPart __instance, ref float __result)
        {
            if (__instance?.pawn?.def?.defName == "SD_AI_Daughter_Race")
            {
                __result = 0f;
            }
        }
    }
}
