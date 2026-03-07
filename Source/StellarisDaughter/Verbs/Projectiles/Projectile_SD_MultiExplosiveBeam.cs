using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class SD_MultiExplosiveBeamProperties
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

    public class SD_MultiExplosiveBeamExtension : DefModExtension
    {
        public List<SD_MultiExplosiveBeamProperties> multiExplosiveBeams = new List<SD_MultiExplosiveBeamProperties>();
    }

    public class Projectile_SD_MultiExplosiveBeam : Beam
    {
        private IntVec3 center = IntVec3.Invalid;

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            if (usedTarget.IsValid)
            {
                center = usedTarget.Cell;
            }

            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            var extension = def.GetModExtension<SD_MultiExplosiveBeamExtension>();
            if (center.IsValid && extension?.multiExplosiveBeams != null)
            {
                for (var i = 0; i < extension.multiExplosiveBeams.Count; i++)
                {
                    ExecuteExplosion(extension.multiExplosiveBeams[i], center);
                }
            }

            base.Impact(hitThing, blockedByShield);
        }

        private void ExecuteExplosion(SD_MultiExplosiveBeamProperties properties, IntVec3 explosionCenter)
        {
            if (Map == null || properties == null)
            {
                return;
            }

            if (properties.explosionEffect != null)
            {
                var effecter = properties.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(explosionCenter, Map), launcher);
                if (properties.explosionEffectLifetimeTicks > 0)
                {
                    Map.effecterMaintainer.AddEffecterToMaintain(effecter, explosionCenter, properties.explosionEffectLifetimeTicks);
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
                foreach (var cell in GenRadial.RadialCellsAround(explosionCenter, properties.radius, true))
                {
                    if (!cell.InBounds(Map))
                    {
                        continue;
                    }

                    var things = Map.thingGrid.ThingsListAt(cell);
                    for (var i = 0; i < things.Count; i++)
                    {
                        var thing = things[i];
                        if (!GenHostility.HostileTo(thing, launcher))
                        {
                            ignoredThings.Add(thing);
                        }
                    }
                }
            }

            GenExplosion.DoExplosion(
                center: explosionCenter,
                map: Map,
                radius: properties.radius,
                damType: properties.damageDef,
                instigator: launcher,
                damAmount: properties.damageAmount,
                armorPenetration: properties.armorPenetration,
                explosionSound: properties.explosionSound,
                weapon: equipmentDef,
                damageFalloff: properties.explosionDamageFalloff,
                intendedTarget: intendedTarget.Thing,
                postExplosionSpawnThingDef: properties.postExplosionSpawnThingDef,
                postExplosionSpawnChance: properties.postExplosionSpawnChance,
                postExplosionSpawnThingCount: properties.postExplosionSpawnThingCount,
                postExplosionGasType: properties.postExplosionGasType,
                postExplosionGasRadiusOverride: properties.postExplosionGasRadiusOverride,
                postExplosionGasAmount: properties.postExplosionGasAmount,
                ignoredThings: ignoredThings);
        }
    }
}
