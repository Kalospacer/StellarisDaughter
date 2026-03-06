using HarmonyLib;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 征召 pawn 时播放语音。
    /// Patch Pawn_DraftController.Drafted setter。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
    public static class Patch_DraftController
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_DraftController __instance, bool value)
        {
            // 仅征召时触发（取消征召不播放）
            if (!value) return;

            var pawn = __instance.pawn;
            if (pawn == null) return;

            var comp = pawn.TryGetComp<CompUnitVoice>();
            if (comp == null) return;

            comp.TryPlay(SD_VoiceEventDefOf.SD_VoiceEvent_Drafted, new VoiceContext
            {
                pawn = pawn,
                map = pawn.MapHeld
            });
        }
    }
}
