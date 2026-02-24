using RimWorld;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 语音事件 Def 的静态引用 — Harmony Patch 通过此类获取事件 Def。
    /// </summary>
    [DefOf]
    public static class SD_VoiceEventDefOf
    {
        public static VoiceEventDef SD_VoiceEvent_Selected;
        public static VoiceEventDef SD_VoiceEvent_Drafted;
        public static VoiceEventDef SD_VoiceEvent_MoveOrder;
        public static VoiceEventDef SD_VoiceEvent_AttackOrder;
        public static VoiceEventDef SD_VoiceEvent_SpamClick;

        static SD_VoiceEventDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SD_VoiceEventDefOf));
    }
}
