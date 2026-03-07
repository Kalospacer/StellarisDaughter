using RimWorld;
using HarmonyLib;
using Verse;

namespace StellarisDaughter
{
    [HarmonyPatch(typeof(Skyfaller), "Impact")]
    public static class Patch_Skyfaller_Impact
    {
        [HarmonyPrefix]
        public static void Prefix(Skyfaller __instance)
        {
            if (__instance == null)
            {
                return;
            }

            __instance.TryGetComp<CompSkyfallerImpactEffect>()?.NotifyImpact();
        }
    }
}
