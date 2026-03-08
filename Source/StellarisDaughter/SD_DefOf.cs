using RimWorld;
using Verse;

namespace StellarisDaughter
{
    [DefOf]
    public static class SD_DefOf
    {
        public static PawnKindDef SD_AI_Daughter;
        public static ThingDef SD_Fake_Spear_Of_Galaxy_Zenith_Beacon_Building;
        public static ThingDef SD_Quest_AIDaughterCryptosleepCasket;

        public static LifeStageDef SD_AI_Childhood;
        public static LifeStageDef SD_AI_Youth;
        public static LifeStageDef SD_AI_Adulthood;
        public static LifeStageDef SD_AI_Final;

        [MayRequire("Tourswen.StellarisDaughter")]
        public static TraitDef SD_Curious;

        [MayRequire("Tourswen.StellarisDaughter")]
        public static TraitDef SD_MemoryCore;

        public static JobDef SD_SuppressWitch;
        public static MentalStateDef SD_WitchBerserk;
        public static ThoughtDef SD_WitchBerserkRecovered;
        public static HediffDef SD_Hediff_WitchForm;

        static SD_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SD_DefOf));
    }

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
