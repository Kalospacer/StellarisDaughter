using HarmonyLib;
using Verse;

namespace StellarisDaughter
{
    [HarmonyPatch(typeof(Hediff_Injury), "get_BleedRate")]
    public static class NoBloodForDaughter_BleedRate_Patch
    {
        public static void Postfix(Hediff_Injury __instance, ref float __result)
        {
            if (__instance.pawn.def.defName == "SD_AI_Daughter_Race")
            {
                __result = 0f;
            }
        }
    }
}
