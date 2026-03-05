using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class Verb_MeleeAttack_BodyWeapon : Verb_MeleeAttack
    {
        private CompPawnBodyWeapon Comp => CasterPawn?.GetComp<CompPawnBodyWeapon>();

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            var result = new DamageWorker.DamageResult();

            PlayAttackEffecter(target);

            var dinfo = new DamageInfo(
                verbProps.meleeDamageDef,
                verbProps.AdjustedMeleeDamageAmount(this, CasterPawn),
                verbProps.AdjustedArmorPenetration(this, CasterPawn),
                -1f,
                CasterPawn,
                null,
                null);
            dinfo.SetTool(tool);

            if (target.HasThing)
            {
                result = target.Thing.TakeDamage(dinfo);
                ApplySpecialEffects(target.Thing);
            }

            var casterPawn = CasterPawn;
            if (casterPawn == null || !target.HasThing)
            {
                return result;
            }

            var mainTarget = target.Thing;
            var attackDirection = (mainTarget.Position - casterPawn.Position).ToVector3().normalized;

            var cleaveRange = Comp.GetCleaveRange(this);
            var cleaveAngle = Comp.GetCleaveAngle(this);
            var damageFactor = Comp.GetDamageFactor(this);

            var potentialTargets = GenRadial.RadialDistinctThingsAround(casterPawn.Position, casterPawn.Map, cleaveRange, useCenter: true);

            foreach (var thing in potentialTargets)
            {
                if (thing == mainTarget || thing == casterPawn || thing is not Pawn secondaryTargetPawn)
                {
                    continue;
                }

                if (!Comp.Props.damageDowned && secondaryTargetPawn.Downed)
                {
                    continue;
                }

                if (secondaryTargetPawn.Faction == casterPawn.Faction)
                {
                    continue;
                }

                var directionToTarget = (thing.Position - casterPawn.Position).ToVector3().normalized;
                var angle = Vector3.Angle(attackDirection, directionToTarget);

                if (angle <= cleaveAngle / 2f)
                {
                    var cleaveDinfo = new DamageInfo(
                        verbProps.meleeDamageDef,
                        verbProps.AdjustedMeleeDamageAmount(this, casterPawn) * damageFactor,
                        verbProps.AdjustedArmorPenetration(this, casterPawn) * damageFactor,
                        -1f,
                        casterPawn,
                        null,
                        null);
                    cleaveDinfo.SetTool(tool);
                    secondaryTargetPawn.TakeDamage(cleaveDinfo);
                    ApplySpecialEffects(secondaryTargetPawn);
                }
            }

            return result;
        }

        private void PlayAttackEffecter(LocalTargetInfo target)
        {
            if (CasterPawn == null || CasterPawn.Map == null || Comp == null)
            {
                return;
            }

            if (Comp.Props.attackEffecter != null)
            {
                var attackEffect = Comp.Props.attackEffecter.Spawn();
                attackEffect.Trigger(new TargetInfo(CasterPawn.Position, CasterPawn.Map), target.ToTargetInfo(CasterPawn.Map));
                attackEffect.Cleanup();
            }

            if (Comp.Props.cleaveEffecter != null && target.HasThing)
            {
                PlayCleaveEffecter(target.Thing);
            }
        }

        private void PlayCleaveEffecter(Thing mainTarget)
        {
            if (CasterPawn == null || CasterPawn.Map == null || mainTarget == null || Comp == null)
            {
                return;
            }

            var casterPawn = CasterPawn;
            var cleaveEffect = Comp.Props.cleaveEffecter.Spawn();
            cleaveEffect.Trigger(new TargetInfo(casterPawn.Position, casterPawn.Map), new TargetInfo(mainTarget.Position, casterPawn.Map));
            cleaveEffect.Cleanup();
        }

        private void ApplySpecialEffects(Thing target)
        {
            if (Comp == null || Comp.Props.applyHediffOnHit == null)
            {
                return;
            }

            if (target is Pawn targetPawn && Rand.Chance(Comp.Props.hediffChance))
            {
                var hediff = HediffMaker.MakeHediff(Comp.Props.applyHediffOnHit, targetPawn);
                hediff.Severity = Comp.Props.hediffSeverity;
                targetPawn.health.AddHediff(hediff);
            }
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);

            if (target.IsValid && CasterPawn != null && Comp != null && Comp.CanUseBodyWeapon(this))
            {
                GenDraw.DrawFieldEdges(GetCleaveCells(target.Cell));
            }
        }

        private List<IntVec3> GetCleaveCells(IntVec3 center)
        {
            if (Comp == null || !Comp.CanUseBodyWeapon(this))
            {
                return new List<IntVec3>();
            }

            var casterPos = CasterPawn.Position;
            var map = CasterPawn.Map;
            var attackDirection = (center - casterPos).ToVector3().normalized;
            var cleaveRange = Comp.GetCleaveRange(this);
            var cleaveAngle = Comp.GetCleaveAngle(this);

            return GenRadial.RadialCellsAround(casterPos, cleaveRange, useCenter: true)
                .Where(cell =>
                {
                    if (!cell.InBounds(map))
                    {
                        return false;
                    }

                    var directionToCell = (cell - casterPos).ToVector3();
                    if (directionToCell.sqrMagnitude <= 0.001f)
                    {
                        return false;
                    }

                    return Vector3.Angle(attackDirection, directionToCell) <= cleaveAngle / 2f;
                }).ToList();
        }

        protected override bool TryCastShot()
        {
            if (Comp != null && !Comp.CanUseBodyWeapon(this))
            {
                if (CasterPawn != null)
                {
                    Messages.Message("SD_BodyWeapon_CannotUse".Translate(CasterPawn.LabelShortCap), MessageTypeDefOf.NeutralEvent);
                }

                return base.TryCastShot();
            }

            return base.TryCastShot();
        }
    }
}
