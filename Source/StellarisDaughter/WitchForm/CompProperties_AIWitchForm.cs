using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 魔女化组件属性配置。
    /// </summary>
    public class CompProperties_AIWitchForm : CompProperties
    {
        public float baseMaxFactor = 1000f;
        public float affectionToMaxFactor = 0.1f;
        public float baseGrowthRate = 0.1f;
        public float negativeTrustGrowthFactor = 0.001f;
        public float positiveTrustDecayFactor = 0.00002f;
        public float positiveTrustDecayMaxBaseFraction = 0.2f;
        public float transformThresholdBaseRatio = 0.5f;
        public float transformThresholdMaxRatio = 0.9f;
        public float affectionForMaxThreshold = 1000f;
        public float witchFormGrowthMult = 3f;
        public float halfMaxGrowthMult = 0.5f;
        public ThingDef normalApparelDef;
        public ThingDef witchApparelDef;
        public HairDef normalHairDef;
        public HairDef witchHairDef;
        public List<ThingDef> keepApparelDefs;

        public CompProperties_AIWitchForm()
        {
            compClass = typeof(CompAIWitchForm);
        }
    }
}
