using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// CompProperties for unit voice system.
    /// 在 ThingDef 的 comps 中配置，指定语音包。
    /// </summary>
    public class CompProperties_UnitVoice : CompProperties
    {
        /// <summary>引用哪套语音包 Def</summary>
        public UnitVoicePackDef voicePack;

        public CompProperties_UnitVoice()
        {
            compClass = typeof(CompUnitVoice);
        }
    }
}
