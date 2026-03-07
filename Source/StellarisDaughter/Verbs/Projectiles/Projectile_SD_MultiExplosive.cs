using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class SD_MultiExplosionProperties
    {
        public float radius;
        public DamageDef damageDef;
        public int damageAmount = 1;
        public float armorPenetration = 1f;
        public SoundDef explosionSound;
        public bool explosionDamageFalloff = true;
        public EffecterDef explosionEffect;
        public int explosionEffectLifetimeTicks;
        public bool onlyAntiHostile;
        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount = 1;
        public GasType? postExplosionGasType;
        public float? postExplosionGasRadiusOverride;
        public int postExplosionGasAmount = 255;
    }

    public class SD_BulletLaunchProperties
    {
        public ThingDef projectileDef;
        public int bulletCount = 1;
        public float angleRange = 60f;
        public FloatRange distanceRange = new FloatRange(3f, 10f);
    }

    public class SD_MultiExplosiveExtension : DefModExtension
    {
        public List<SD_MultiExplosionProperties> multiexplosions = new();
        public List<SD_BulletLaunchProperties> bulletLaunches = new();
    }

    public class SD_TailBulletDef : DefModExtension
    {
        public FleckDef tailFleckDef;
        public int fleckMakeFleckTickMax = 1;
        public int fleckDelayTicks = 10;
        public IntRange fleckMakeFleckNum = new(1, 1);
        public FloatRange fleckAngle = new(-180f, 180f);
        public FloatRange fleckScale = new(1f, 1f);
        public FloatRange fleckSpeed = new(0f, 0f);
        public FloatRange fleckRotation = new(-180f, 180f);
    }

    public class Projectile_SD_MultiExplosive : Projectile
    {
        private SD_TailBulletDef tailDefInt;
        private int fleckTickCounter;
        private Vector3 lastTickPosition;
        private int ticksToDetonation = 1;
        private bool projIsLanded;

        public SD_TailBulletDef TailDef
        {
            get
            {
                if (tailDefInt == null)
                {
                    tailDefInt = def.GetModExtension<SD_TailBulletDef>() ?? new SD_TailBulletDef();
                }

                return tailDefInt;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksToDetonation, "ticksToDetonation", 1);
            Scribe_Values.Look(ref projIsLanded, "projIsLanded");
        }

        protected override void Tick()
        {
            base.Tick();
            TickTrail();
            if (projIsLanded)
            {
                ticksToDetonation--;
            }

            lastTickPosition = ExactPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (def.projectile.explosionDelay > 0 && ticksToDetonation > 0)
            {
                if (!projIsLanded)
                {
                    projIsLanded = true;
                    ticksToDetonation = def.projectile.explosionDelay;
                }

                return;
            }

            var extension = def.GetModExtension<SD_MultiExplosiveExtension>();
            if (extension != null)
            {
                if (extension.multiexplosions != null)
                {
                    foreach (var explosion in extension.multiexplosions)
                    {
                        ExecuteExplosion(explosion);
                    }
                }

                if (extension.bulletLaunches != null)
                {
                    foreach (var bulletLaunch in extension.bulletLaunches)
                    {
                        LaunchAdditionalBullets(bulletLaunch);
                    }
                }
            }

            base.Impact(hitThing, blockedByShield);
        }

        private void TickTrail()
        {
            if (Map == null || TailDef == null || TailDef.tailFleckDef == null)
            {
                return;
            }

            fleckTickCounter++;
            if (fleckTickCounter < TailDef.fleckDelayTicks)
            {
                return;
            }

            if (fleckTickCounter >= TailDef.fleckDelayTicks + TailDef.fleckMakeFleckTickMax)
            {
                fleckTickCounter = TailDef.fleckDelayTicks;
            }

            int fleckCount = TailDef.fleckMakeFleckNum.RandomInRange;
            Vector3 currentPosition = ExactPosition;
            Vector3 previousPosition = lastTickPosition;
            float flightAngle = (currentPosition - previousPosition).AngleFlat();

            for (int i = 0; i < fleckCount; i++)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(currentPosition, Map, TailDef.tailFleckDef, TailDef.fleckScale.RandomInRange);
                data.rotation = flightAngle;
                data.rotationRate = TailDef.fleckRotation.RandomInRange;
                data.velocityAngle = TailDef.fleckAngle.RandomInRange + flightAngle;
                data.velocitySpeed = TailDef.fleckSpeed.RandomInRange;
                Map.flecks.CreateFleck(data);
            }
        }

        private void ExecuteExplosion(SD_MultiExplosionProperties properties)
        {
            if (properties.explosionEffect != null)
            {
                Effecter effecter = properties.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(Position, Map, false), launcher, -1);
                if (properties.explosionEffectLifetimeTicks != 0)
                {
                    Map.effecterMaintainer.AddEffecterToMaintain(effecter, Position, properties.explosionEffectLifetimeTicks);
                }
                else
                {
                    effecter.Cleanup();
                }
            }

            List<Thing> ignoredThings = null;
            if (properties.onlyAntiHostile)
            {
                ignoredThings = new List<Thing>();
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, properties.radius, true))
                {
                    if (!cell.InBounds(Map))
                    {
                        continue;
                    }

                    foreach (Thing thing in Map.thingGrid.ThingsListAt(cell))
                    {
                        if (!GenHostility.HostileTo(thing, launcher))
                        {
                            ignoredThings.Add(thing);
                        }
                    }
                }
            }

            GenExplosion.DoExplosion(
                center: Position,
                map: Map,
                radius: properties.radius,
                damType: properties.damageDef,
                instigator: launcher,
                damAmount: properties.damageAmount,
                armorPenetration: properties.armorPenetration,
                explosionSound: properties.explosionSound,
                weapon: equipmentDef,
                projectile: def,
                intendedTarget: intendedTarget.Thing,
                postExplosionSpawnThingDef: properties.postExplosionSpawnThingDef,
                postExplosionSpawnChance: properties.postExplosionSpawnChance,
                postExplosionSpawnThingCount: properties.postExplosionSpawnThingCount,
                postExplosionGasType: properties.postExplosionGasType,
                postExplosionGasRadiusOverride: properties.postExplosionGasRadiusOverride,
                postExplosionGasAmount: properties.postExplosionGasAmount,
                damageFalloff: properties.explosionDamageFalloff,
                ignoredThings: ignoredThings);
        }

        private void LaunchAdditionalBullets(SD_BulletLaunchProperties properties)
        {
            if (properties.projectileDef == null || properties.bulletCount <= 0 || Map == null)
            {
                return;
            }

            float baseAngle = ExactRotation.eulerAngles.y;
            for (int i = 0; i < properties.bulletCount; i++)
            {
                float randomAngle = (baseAngle + Random.Range(-properties.angleRange, properties.angleRange) + 360f) % 360f;
                float randomDistance = Random.Range(properties.distanceRange.min, properties.distanceRange.max);
                Vector3 direction = Quaternion.Euler(0f, randomAngle, 0f) * Vector3.forward;
                IntVec3 targetCell = Position + new IntVec3((int)(direction.x * randomDistance), 0, (int)(direction.z * randomDistance));

                if (!targetCell.InBounds(Map))
                {
                    targetCell = GetRandomValidTargetCell(randomDistance);
                    if (!targetCell.IsValid)
                    {
                        continue;
                    }
                }

                Projectile projectile = (Projectile)ThingMaker.MakeThing(properties.projectileDef);
                GenSpawn.Spawn(projectile, Position, Map);
                projectile.Launch(
                    launcher: launcher,
                    usedTarget: new LocalTargetInfo(targetCell),
                    intendedTarget: new LocalTargetInfo(targetCell),
                    hitFlags: projectile.HitFlags,
                    equipment: equipment);
            }
        }

        private IntVec3 GetRandomValidTargetCell(float radius)
        {
            for (int i = 0; i < 10; i++)
            {
                IntVec3 randomCell = Position + new IntVec3(Random.Range(-(int)radius, (int)radius), 0, Random.Range(-(int)radius, (int)radius));
                if (randomCell.InBounds(Map) && randomCell.Walkable(Map))
                {
                    return randomCell;
                }
            }

            return IntVec3.Invalid;
        }
    }
}
