using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace StellarisDaughter
{
    /// <summary>
    /// 镇压魔女的Job驱动
    /// </summary>
    public class JobDriver_SuppressWitch : JobDriver
    {
        private const int SuppressDuration = 300; // 5秒
        private const TargetIndex WitchIndex = TargetIndex.A;

        private Pawn Witch => (Pawn)job.GetTarget(WitchIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Witch, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 检查目标是否有效
            this.FailOnDespawnedOrNull(WitchIndex);
            this.FailOnAggroMentalState(WitchIndex);

            // 前往目标
            yield return Toils_Goto.GotoThing(WitchIndex, PathEndMode.Touch);

            // 执行镇压
            Toil suppress = new Toil();
            suppress.initAction = delegate
            {
                ticksLeftThisToil = SuppressDuration;
            };
            suppress.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Witch);
            };
            suppress.defaultCompleteMode = ToilCompleteMode.Delay;
            suppress.defaultDuration = SuppressDuration;
            suppress.WithProgressBarToilDelay(WitchIndex);
            suppress.AddFinishAction(delegate
            {
                if (Witch != null && !Witch.Dead)
                {
                    var witchComp = Witch.GetComp<CompAIWitchForm>();
                    if (witchComp != null && witchComp.IsBerserk)
                    {
                        witchComp.ApplySuppression();
                    }
                }
            });
            yield return suppress;
        }
    }
}
