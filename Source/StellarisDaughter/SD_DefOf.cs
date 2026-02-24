using RimWorld;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    [DefOf]
    public static class SD_DefOf
    {
        // 生命阶段
        public static LifeStageDef SD_AI_Childhood;
        public static LifeStageDef SD_AI_Youth;
        public static LifeStageDef SD_AI_Adulthood;
        public static LifeStageDef SD_AI_Final;

        // 特性
        [MayRequire("Tourswen.StellarisDaughter")]
        public static TraitDef SD_Curious;
        [MayRequire("Tourswen.StellarisDaughter")]
        public static TraitDef SD_MemoryCore;

        static SD_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SD_DefOf));
    }
}
