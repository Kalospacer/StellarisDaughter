using HarmonyLib;
using Verse;

namespace StellarisDaughter
{
    [HarmonyPatch(typeof(Projectile), "Impact")]
    public static class Patch_Projectile_Impact
    {
        [HarmonyPrefix]
        public static void Prefix(Projectile __instance)
        {
            if (__instance == null)
            {
                return;
            }

            __instance.TryGetComp<CompSD_ProjectileImpactEffect>()?.NotifyImpact();
        }
    }

    [HarmonyPatch(typeof(Projectile_ExplosiveTrackingBullet), "Impact")]
    public static class Patch_Projectile_ExplosiveTrackingBullet_Impact
    {
        [HarmonyPrefix]
        public static void Prefix(Projectile_ExplosiveTrackingBullet __instance)
        {
            if (__instance == null)
            {
                return;
            }

            __instance.TryGetComp<CompSD_ProjectileImpactEffect>()?.NotifyImpact();
        }
    }
}
