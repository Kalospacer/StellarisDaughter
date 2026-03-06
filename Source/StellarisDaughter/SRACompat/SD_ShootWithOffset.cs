using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class ModExtension_SD_ShootWithOffset : DefModExtension
    {
        public List<Vector2> offsets = new List<Vector2>();

        public Vector2 GetOffsetFor(int index)
        {
            if (offsets.NullOrEmpty())
            {
                return Vector2.zero;
            }

            var offsetIndex = Mathf.Abs(index) % offsets.Count;
            return offsets[offsetIndex];
        }
    }

    public class Verb_SD_ShootWithOffset : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            var cast = BaseTryCastShot();
            if (cast && CasterIsPawn)
            {
                CasterPawn.records.Increment(RecordDefOf.ShotsFired);
            }

            return cast;
        }

        protected bool BaseTryCastShot()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
            {
                return false;
            }

            var projectile = Projectile;
            if (projectile == null)
            {
                return false;
            }

            if (!TryFindShootLineFromTo(caster.Position, currentTarget, out var resultingLine) && verbProps.stopBurstWithoutLos)
            {
                return false;
            }

            if (EquipmentSource != null)
            {
                EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
                EquipmentSource.GetComp<CompApparelVerbOwner_Charged>()?.UsedOnce();
            }

            lastShotTick = Find.TickManager.TicksGame;
            Thing manningPawn = caster;
            Thing equipmentSource = EquipmentSource;
            var compMannable = caster.TryGetComp<CompMannable>();
            if (compMannable?.ManningPawn != null)
            {
                manningPawn = compMannable.ManningPawn;
                equipmentSource = caster;
            }

            var drawPos = ApplyProjectileOffset(caster.DrawPos, equipmentSource);
            var launchedProjectile = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
            if (equipmentSource != null && equipmentSource.TryGetComp(out CompUniqueWeapon comp))
            {
                foreach (var trait in comp.TraitsListForReading)
                {
                    if (trait.damageDefOverride != null)
                    {
                        launchedProjectile.damageDefOverride = trait.damageDefOverride;
                    }

                    if (trait.extraDamages.NullOrEmpty())
                    {
                        continue;
                    }

                    if (launchedProjectile.extraDamages == null)
                    {
                        launchedProjectile.extraDamages = new List<ExtraDamage>();
                    }

                    launchedProjectile.extraDamages.AddRange(trait.extraDamages);
                }
            }

            if (verbProps.ForcedMissRadius > 0.5f)
            {
                var forcedMissRadius = verbProps.ForcedMissRadius;
                if (manningPawn is Pawn pawn)
                {
                    forcedMissRadius *= verbProps.GetForceMissFactorFor(equipmentSource, pawn);
                }

                var adjustedForcedMiss = VerbUtility.CalculateAdjustedForcedMiss(forcedMissRadius, currentTarget.Cell - caster.Position);
                if (adjustedForcedMiss > 0.5f)
                {
                    var forcedMissTarget = GetForcedMissTarget(adjustedForcedMiss);
                    if (forcedMissTarget != currentTarget.Cell)
                    {
                        var hitFlags = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f))
                        {
                            hitFlags = ProjectileHitFlags.All;
                        }

                        if (!canHitNonTargetPawnsNow)
                        {
                            hitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                        }

                        launchedProjectile.Launch(manningPawn, drawPos, forcedMissTarget, currentTarget, hitFlags, preventFriendlyFire, equipmentSource);
                        return true;
                    }
                }
            }

            var shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
            var randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
            var targetCoverDef = randomCoverToMissInto?.def;

            if (verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                var flyOverhead = launchedProjectile.def.projectile?.flyOverhead ?? false;
                resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, flyOverhead, caster.Map);
                var hitFlags = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
                {
                    hitFlags |= ProjectileHitFlags.NonTargetPawns;
                }

                launchedProjectile.Launch(manningPawn, drawPos, resultingLine.Dest, currentTarget, hitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
                return true;
            }

            if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
            {
                var hitFlags = ProjectileHitFlags.NonTargetWorld;
                if (canHitNonTargetPawnsNow)
                {
                    hitFlags |= ProjectileHitFlags.NonTargetPawns;
                }

                launchedProjectile.Launch(manningPawn, drawPos, randomCoverToMissInto, currentTarget, hitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
                return true;
            }

            var intendedHitFlags = ProjectileHitFlags.IntendedTarget;
            if (canHitNonTargetPawnsNow)
            {
                intendedHitFlags |= ProjectileHitFlags.NonTargetPawns;
            }

            if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
            {
                intendedHitFlags |= ProjectileHitFlags.NonTargetWorld;
            }

            if (currentTarget.Thing != null)
            {
                launchedProjectile.Launch(manningPawn, drawPos, currentTarget, currentTarget, intendedHitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
            }
            else
            {
                launchedProjectile.Launch(manningPawn, drawPos, resultingLine.Dest, currentTarget, intendedHitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
            }

            return true;
        }

        private Vector3 ApplyProjectileOffset(Vector3 originalDrawPos, Thing equipmentSource)
        {
            var offsetExtension = ResolveOffsetExtension(equipmentSource);
            if (offsetExtension == null || offsetExtension.offsets.NullOrEmpty())
            {
                return originalDrawPos;
            }

            var burstShotsLeft = GetBurstShotsLeft();
            var targetPos = currentTarget.CenterVector3;
            var casterPos = caster.DrawPos;
            var rimworldAngle = targetPos.AngleToFlat(casterPos);
            var correctedAngle = ConvertRimWorldAngleToOffsetAngle(rimworldAngle);
            var offset = offsetExtension.GetOffsetFor(burstShotsLeft);
            var rotatedOffset = offset.RotatedBy(correctedAngle);
            return originalDrawPos + new Vector3(rotatedOffset.x, 0f, rotatedOffset.y);
        }

        private ModExtension_SD_ShootWithOffset ResolveOffsetExtension(Thing equipmentSource)
        {
            if (equipmentSource?.def != null)
            {
                var extension = equipmentSource.def.GetModExtension<ModExtension_SD_ShootWithOffset>();
                if (extension != null)
                {
                    return extension;
                }
            }

            return caster?.def?.GetModExtension<ModExtension_SD_ShootWithOffset>();
        }

        private int GetBurstShotsLeft()
        {
            return burstShotsLeft >= 0 ? burstShotsLeft : 0;
        }

        private float ConvertRimWorldAngleToOffsetAngle(float rimworldAngle)
        {
            return -rimworldAngle - 90f;
        }
    }

    public class Verb_SD_ShootSustained : Verb_SD_ShootWithOffset
    {
        public Pawn forceTargetedDownedPawn;

        public int BurstShotsLeft => burstShotsLeft;

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            forceTargetedDownedPawn = null;
            base.OrderForceTarget(target);
            if (target.Pawn != null && target.Pawn.Downed && target.Pawn.Spawned)
            {
                forceTargetedDownedPawn = target.Pawn;
            }

            currentTarget = target;
        }

        public override void WarmupComplete()
        {
            burstShotsLeft = BurstShotCount;
            state = VerbState.Bursting;
            TryCastNextBurstShot();

            if (currentTarget.Thing is Pawn pawn && !pawn.Downed && !pawn.IsColonyMech && CasterIsPawn && CasterPawn.skills != null && burstShotsLeft == BurstShotCount)
            {
                var baseExperience = pawn.HostileTo(caster) ? 170f : 20f;
                var cycleTime = verbProps.AdjustedFullCycleTime(this, CasterPawn);
                CasterPawn.skills.Learn(SkillDefOf.Shooting, baseExperience * cycleTime, false);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref forceTargetedDownedPawn, "forceTargetedDownedPawn");
        }
    }
}
