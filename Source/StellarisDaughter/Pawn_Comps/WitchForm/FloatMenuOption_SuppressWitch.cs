using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace StellarisDaughter
{
    /// <summary>
    /// 镇压魔女的右键菜单选项提供者
    /// </summary>
    public class FloatMenuOption_SuppressWitch : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool RequiresManipulation => true;

        protected override bool AppliesInt(FloatMenuContext context)
        {
            return true;
        }

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
        {
            // 检查目标是否是发狂的魔女
            var witchComp = clickedPawn.GetComp<CompAIWitchForm>();
            if (witchComp == null || !witchComp.IsBerserk)
            {
                yield break;
            }

            // 检查目标是否倒地
            if (!clickedPawn.Downed)
            {
                yield return new FloatMenuOption("SD_Witch_CannotSuppress".Translate() + ": " + "SD_Witch_MustBeDowned".Translate(), null);
                yield break;
            }

            // 检查是否可以到达
            if (!context.FirstSelectedPawn.CanReach(clickedPawn, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("SD_Witch_CannotSuppress".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }

            // 检查是否可以预约
            if (!context.FirstSelectedPawn.CanReserve(clickedPawn))
            {
                yield return new FloatMenuOption("SD_Witch_CannotSuppress".Translate() + ": " + "Reserved".Translate(), null);
                yield break;
            }

            // 创建镇压选项
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption("SD_Witch_Suppress".Translate(clickedPawn.LabelShort), delegate
                {
                    Job job = JobMaker.MakeJob(SD_DefOf.SD_SuppressWitch, clickedPawn);
                    context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }),
                context.FirstSelectedPawn,
                clickedPawn
            );
        }
    }
}
