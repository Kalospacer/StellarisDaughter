using RimWorld;
using Verse;

namespace StellarisDaughter
{
    [DefOf]
    public static class SD_DamageDefOf
    {
        public static DamageDef SD_DarkMatterFlame;

        static SD_DamageDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SD_DamageDefOf));
        }
    }
}
