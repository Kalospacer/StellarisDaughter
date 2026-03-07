using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class MapComponent_AIOverwatch : MapComponent
    {
        private bool enabled;
        private int durationTicks;
        private int tickCounter;
        private int globalCooldownTicks;
        private int strikesThisScan;
        private const int CheckInterval = 180;

        public bool IsEnabled => enabled;
        public int DurationTicks => durationTicks;

        public MapComponent_AIOverwatch(Map map) : base(map)
        {
        }

        public void EnableOverwatch(int durationSeconds, bool useArtilleryVersion = false)
        {
            if (enabled)
            {
                Messages.Message("SD_AIOverwatch_AlreadyActive".Translate(durationTicks / 60), MessageTypeDefOf.RejectInput);
                return;
            }

            int clampedDuration = Math.Min(durationSeconds, 180);
            enabled = true;
            durationTicks = clampedDuration * 60;
            tickCounter = 0;
            globalCooldownTicks = 0;
            TryCallFleet(useArtilleryVersion);
            Messages.Message("SD_AIOverwatch_Engaged".Translate(clampedDuration), MessageTypeDefOf.PositiveEvent);
        }

        public void DisableOverwatch()
        {
            enabled = false;
            durationTicks = 0;
            TryClearFlightPath();
            Messages.Message("SD_AIOverwatch_Disengaged".Translate(), MessageTypeDefOf.NeutralEvent);
        }

        private void TryCallFleet(bool useArtilleryVersion)
        {
            try
            {
                string defName = useArtilleryVersion ? "SD_Spear_Of_Galaxy_Zenith_Planet_Interdiction" : "SD_Spear_Of_Galaxy_Zenith";
                ThingDef flyOverDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (flyOverDef == null)
                {
                    Log.Message($"[AI Overwatch] Could not find {defName} ThingDef.");
                    return;
                }

                IntVec3 startPos = GetRandomMapEdgePosition(map);
                IntVec3 endPos = GetOppositeMapEdgePosition(map, startPos);
                FlyOver flyOver = FlyOver.MakeFlyOver(flyOverDef, startPos, endPos, map, useArtilleryVersion ? 0.02f : 0.01f, 20f);
                if (flyOver != null)
                {
                    Messages.Message("SD_AIOverwatch_FleetCalled".Translate(), MessageTypeDefOf.PositiveEvent);
                }
            }
            catch (Exception ex)
            {
                Log.Message($"[AI Overwatch] Failed to call fleet: {ex.Message}");
            }
        }

        private IntVec3 GetRandomMapEdgePosition(Map map)
        {
            int edge = Rand.Range(0, 4);
            return edge switch
            {
                0 => new IntVec3(Rand.Range(5, map.Size.x - 5), 0, 0),
                1 => new IntVec3(map.Size.x - 1, 0, Rand.Range(5, map.Size.z - 5)),
                2 => new IntVec3(Rand.Range(5, map.Size.x - 5), 0, map.Size.z - 1),
                _ => new IntVec3(0, 0, Rand.Range(5, map.Size.z - 5))
            };
        }

        private IntVec3 GetOppositeMapEdgePosition(Map map, IntVec3 startPos)
        {
            IntVec3 center = map.Center;
            Vector3 toCenter = (center.ToVector3() - startPos.ToVector3()).normalized;
            Vector3 endVec = startPos.ToVector3() + toCenter * Mathf.Max(map.Size.x, map.Size.z) * 1.5f;
            return new IntVec3(Mathf.Clamp((int)endVec.x, 0, map.Size.x - 1), 0, Mathf.Clamp((int)endVec.z, 0, map.Size.z - 1));
        }

        private void TryClearFlightPath()
        {
            try
            {
                List<FlyOver> flyOvers = map.listerThings.AllThings.OfType<FlyOver>().ToList();
                foreach (FlyOver flyOver in flyOvers.Where(f => !f.Destroyed))
                {
                    flyOver.EmergencyDestroy();
                }

                if (flyOvers.Count > 0)
                {
                    Messages.Message("SD_AIOverwatch_FleetCleared".Translate(), MessageTypeDefOf.NeutralEvent);
                }
            }
            catch (Exception ex)
            {
                Log.Message($"[AI Overwatch] Failed to clear flight path: {ex.Message}");
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!enabled)
            {
                return;
            }

            durationTicks--;
            if (durationTicks <= 0)
            {
                DisableOverwatch();
                return;
            }

            if (globalCooldownTicks > 0)
            {
                globalCooldownTicks--;
            }

            tickCounter++;
            if (tickCounter >= CheckInterval)
            {
                tickCounter = 0;
                if ((durationTicks % 1800) < CheckInterval)
                {
                    Messages.Message("SD_AIOverwatch_SystemActive".Translate(durationTicks / 60), MessageTypeDefOf.NeutralEvent);
                }

                PerformScanAndStrike();
            }
        }

        private void PerformScanAndStrike()
        {
            List<Pawn> hostilePawns = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead && !p.Downed && p.HostileTo(Faction.OfPlayer) && !p.IsPrisoner).ToList();
            List<Building> hostileBuildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                .OfType<Building>()
                .Where(b => b.Faction != null && b.Faction.HostileTo(Faction.OfPlayer))
                .Distinct()
                .ToList();

            strikesThisScan = 0;
            if (hostilePawns.Count > 0)
            {
                List<List<Pawn>> clusters = ClusterPawns(hostilePawns, 12f);
                clusters.Sort((a, b) => b.Count.CompareTo(a.Count));
                foreach (List<Pawn> cluster in clusters)
                {
                    if (globalCooldownTicks > 0 || strikesThisScan >= 3)
                    {
                        break;
                    }
                    ProcessCluster(cluster);
                }
            }

            foreach (IntVec3 buildingPos in hostileBuildings.Select(b => b.Position))
            {
                if (globalCooldownTicks > 0 || strikesThisScan >= 3)
                {
                    break;
                }
                ProcessBuildingTarget(buildingPos);
            }
        }

        private void ProcessBuildingTarget(IntVec3 target)
        {
            if (!target.InBounds(map) || IsFriendlyFireRisk(target, 9.9f))
            {
                return;
            }

            AbilityDef cannonDef = DefDatabase<AbilityDef>.GetNamedSilentFail("SD_Firepower_Cannon_Salvo");
            if (cannonDef != null)
            {
                Messages.Message("SD_AIOverwatch_EngagingBuilding".Translate(), new TargetInfo(target, map), MessageTypeDefOf.PositiveEvent);
                FireAbility(cannonDef, target, Rand.Range(0, 360));
                strikesThisScan++;
            }
        }

        private void ProcessCluster(List<Pawn> cluster)
        {
            if (cluster.Count == 0 || strikesThisScan >= 3)
            {
                return;
            }

            float x = 0f;
            float z = 0f;
            foreach (Pawn pawn in cluster)
            {
                x += pawn.Position.x;
                z += pawn.Position.z;
            }

            IntVec3 center = new IntVec3((int)(x / cluster.Count), 0, (int)(z / cluster.Count));
            if (!center.InBounds(map))
            {
                return;
            }

            float angle = Rand.Range(0, 360);
            if (cluster.Count >= 20)
            {
                if (IsFriendlyFireRisk(center, 18.9f))
                {
                    return;
                }

                AbilityDef lanceDef = DefDatabase<AbilityDef>.GetNamedSilentFail("SD_Firepower_EnergyLance_Strafe");
                AbilityDef cannonDef = DefDatabase<AbilityDef>.GetNamedSilentFail("SD_Firepower_Primary_Cannon_Strafe");
                Messages.Message("SD_AIOverwatch_MassiveCluster".Translate(cluster.Count), new TargetInfo(center, map), MessageTypeDefOf.ThreatBig);
                if (lanceDef != null) FireAbility(lanceDef, center, angle);
                if (cannonDef != null) FireAbility(cannonDef, center, angle + 45f);
                strikesThisScan++;
                return;
            }

            if (cluster.Count >= 10)
            {
                if (IsFriendlyFireRisk(center, 16.9f))
                {
                    return;
                }

                AbilityDef lanceDef = DefDatabase<AbilityDef>.GetNamedSilentFail("SD_Firepower_EnergyLance_Strafe");
                if (lanceDef != null)
                {
                    Messages.Message("SD_AIOverwatch_EngagingLance".Translate(cluster.Count), new TargetInfo(center, map), MessageTypeDefOf.PositiveEvent);
                    FireAbility(lanceDef, center, angle);
                    strikesThisScan++;
                }
                return;
            }

            if (cluster.Count >= 3)
            {
                if (IsFriendlyFireRisk(center, 9.9f))
                {
                    return;
                }

                AbilityDef cannonDef = DefDatabase<AbilityDef>.GetNamedSilentFail("SD_Firepower_Cannon_Salvo");
                if (cannonDef != null)
                {
                    Messages.Message("SD_AIOverwatch_EngagingCannon".Translate(cluster.Count), new TargetInfo(center, map), MessageTypeDefOf.PositiveEvent);
                    FireAbility(cannonDef, center, angle);
                    strikesThisScan++;
                }
                return;
            }

            if (IsFriendlyFireRisk(center, 5.9f))
            {
                return;
            }

            AbilityDef minigunDef = DefDatabase<AbilityDef>.GetNamedSilentFail("SD_Firepower_Minigun_Strafe");
            if (minigunDef != null)
            {
                Messages.Message("SD_AIOverwatch_EngagingMinigun".Translate(cluster.Count), new TargetInfo(center, map), MessageTypeDefOf.PositiveEvent);
                FireAbility(minigunDef, center, angle);
                strikesThisScan++;
            }
        }

        private void FireAbility(AbilityDef ability, IntVec3 target, float angle)
        {
            CompProperties_AbilityCircularBombardment circular = ability.comps?.OfType<CompProperties_AbilityCircularBombardment>().FirstOrDefault();
            if (circular != null)
            {
                BombardmentUtility.ExecuteCircularBombardment(map, target, ability, circular);
                globalCooldownTicks = 120;
                return;
            }

            CompProperties_AbilityBombardment bombard = ability.comps?.OfType<CompProperties_AbilityBombardment>().FirstOrDefault();
            if (bombard != null)
            {
                BombardmentUtility.ExecuteStrafeBombardmentDirect(map, target, ability, bombard, angle);
                globalCooldownTicks = 120;
                return;
            }

            CompProperties_AbilityEnergyLance lance = ability.comps?.OfType<CompProperties_AbilityEnergyLance>().FirstOrDefault();
            if (lance != null)
            {
                BombardmentUtility.ExecuteEnergyLanceDirect(map, target, ability, lance, angle);
                globalCooldownTicks = 120;
            }
        }

        private bool IsFriendlyFireRisk(IntVec3 center, float radius)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if ((pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony) && pawn.Position.InHorDistOf(center, radius))
                {
                    Messages.Message("SD_AIOverwatch_FriendlyFireAbort".Translate(center.ToString()), new TargetInfo(center, map), MessageTypeDefOf.CautionInput);
                    return true;
                }
            }

            return false;
        }

        private List<List<Pawn>> ClusterPawns(List<Pawn> pawns, float radius)
        {
            List<List<Pawn>> clusters = new List<List<Pawn>>();
            HashSet<Pawn> assigned = new HashSet<Pawn>();
            foreach (Pawn pawn in pawns)
            {
                if (assigned.Contains(pawn))
                {
                    continue;
                }

                List<Pawn> cluster = new List<Pawn> { pawn };
                assigned.Add(pawn);
                foreach (Pawn neighbor in pawns)
                {
                    if (!assigned.Contains(neighbor) && pawn.Position.InHorDistOf(neighbor.Position, radius))
                    {
                        cluster.Add(neighbor);
                        assigned.Add(neighbor);
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enabled, "enabled");
            Scribe_Values.Look(ref durationTicks, "durationTicks");
            Scribe_Values.Look(ref tickCounter, "tickCounter");
            Scribe_Values.Look(ref globalCooldownTicks, "globalCooldownTicks");
        }
    }
}
