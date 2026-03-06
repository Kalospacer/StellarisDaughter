using HarmonyLib;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    [HarmonyPatch(typeof(Bullet), "Impact")]
    public static class Patch_Bullet_Impact
    {
        [HarmonyPostfix]
        public static void Postfix(Bullet __instance)
        {
            if (__instance?.Destroyed == true)
            {
                return;
            }

            __instance.TryGetComp<CompSD_ProjectileImpactEffect>()?.NotifyImpact();
        }
    }
}
