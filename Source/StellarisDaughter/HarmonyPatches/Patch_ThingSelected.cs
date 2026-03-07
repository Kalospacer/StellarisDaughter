using HarmonyLib;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 选中 pawn 时播放语音。
    /// Thing.Notify_ThingSelected() 在 Selector.Select() 中被调用。
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.Notify_ThingSelected))]
    public static class Patch_ThingSelected
    {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance)
        {
            var comp = __instance.TryGetComp<CompUnitVoice>();
            if (comp == null) return;

            var pawn = __instance as Pawn;
            if (pawn == null) return;

            comp.TryPlay(SD_VoiceEventDefOf.SD_VoiceEvent_Selected, new VoiceContext
            {
                pawn = pawn,
                map = pawn.MapHeld
            });
        }
    }
}
