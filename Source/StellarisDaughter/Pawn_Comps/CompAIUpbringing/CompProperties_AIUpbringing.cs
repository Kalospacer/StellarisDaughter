using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    public class CompProperties_AIUpbringing : CompProperties
    {
        public float moodToAffectionFactor = 0.3f;
        public float moodToTrustFactor = 0.5f;
        public float perThoughtDeltaAbsCap = 20f;

        public CompProperties_AIUpbringing()
        {
            compClass = typeof(CompAIUpbringing);
        }
    }
}
