using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 魔女化组件属性配置
    /// </summary>
    public class CompProperties_AIWitchForm : CompProperties
    {
        // 魔女因子基础上限
        public float baseMaxFactor = 1000f;

        // 基础增长速度（每TickRare）
        public float baseGrowthRate = 0.1f;

        // 变身状态增长倍率
        public float witchFormGrowthMult = 3f;

        // 超过50%后的增长倍率
        public float halfMaxGrowthMult = 0.5f;

        // 正常形态衣服
        public ThingDef normalApparelDef;

        // 魔女形态衣服
        public ThingDef witchApparelDef;

        // 正常形态头发
        public HairDef normalHairDef;

        // 魔女形态头发
        public HairDef witchHairDef;

        // 切换时保留的装备白名单（不脱下）
        public List<ThingDef> keepApparelDefs;

        public CompProperties_AIWitchForm()
        {
            compClass = typeof(CompAIWitchForm);
        }
    }
}
