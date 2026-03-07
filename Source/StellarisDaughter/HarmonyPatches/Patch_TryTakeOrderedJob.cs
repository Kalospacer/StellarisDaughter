using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 玩家下达移动/攻击指令时播放语音。
    /// TryTakeOrderedJob 仅在玩家手动下令时被调用（job.playerForced = true）。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
    public static class Patch_TryTakeOrderedJob
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_JobTracker __instance, bool __result, Job job)
        {
            // 仅在下令成功时触发
            if (!__result) return;
            if (job == null) return;

            // pawn 字段是 protected，通过 Traverse 访问
            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null) return;

            var comp = pawn.TryGetComp<CompUnitVoice>();
            if (comp == null) return;

            VoiceEventDef eventDef = null;
            Thing target = null;

            if (job.def == JobDefOf.Goto || job.def == JobDefOf.GotoWander)
            {
                eventDef = SD_VoiceEventDefOf.SD_VoiceEvent_MoveOrder;
            }
            else if (job.def == JobDefOf.AttackMelee || job.def == JobDefOf.AttackStatic)
            {
                eventDef = SD_VoiceEventDefOf.SD_VoiceEvent_AttackOrder;
                target = job.targetA.Thing;
            }

            if (eventDef == null) return;

            comp.TryPlay(eventDef, new VoiceContext
            {
                pawn = pawn,
                target = target,
                job = job,
                map = pawn.MapHeld
            });
        }
    }
}
