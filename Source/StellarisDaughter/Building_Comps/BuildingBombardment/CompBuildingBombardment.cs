using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class CompBuildingBombardment : ThingComp
    {
        public CompProperties_BuildingBombardment Props => (CompProperties_BuildingBombardment)props;

        private BuildingBombardmentState currentState = BuildingBombardmentState.Idle;
        private int nextBurstTick;
        private int currentBurstCount;
        private int nextInnerBurstTick;
        private List<LocalTargetInfo> currentTargets = new List<LocalTargetInfo>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                StartNextBurst();
            }
        }

        private void StartNextBurst()
        {
            currentState = BuildingBombardmentState.Targeting;
            currentBurstCount = 0;
            currentTargets.Clear();
            SelectTargets();

            if (currentTargets.Count > 0)
            {
                currentState = BuildingBombardmentState.Bursting;
                nextInnerBurstTick = Find.TickManager.TicksGame;
                Log.Message($"[BuildingBombardment] Starting burst with {currentTargets.Count} targets");
            }
            else
            {
                currentState = BuildingBombardmentState.Idle;
                nextBurstTick = Find.TickManager.TicksGame + Props.burstIntervalTicks;
                Log.Message("[BuildingBombardment] No targets found, waiting for next burst");
            }
        }

        private void SelectTargets()
        {
            Map map = parent.Map;
            if (map == null)
            {
                return;
            }

            List<Pawn> potentialTargets = new List<Pawn>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (IsValidTarget(pawn) && IsInRange(pawn.Position))
                {
                    potentialTargets.Add(pawn);
                }
            }

            int targetCount = Mathf.Min(Props.burstCount, potentialTargets.Count);
            currentTargets = potentialTargets.InRandomOrder().Take(targetCount).Select(p => new LocalTargetInfo(p)).ToList();
        }

        private bool IsValidTarget(Pawn pawn)
        {
            if (pawn == null || pawn.Downed || pawn.Dead)
            {
                return false;
            }

            if (Props.targetEnemies && pawn.HostileTo(parent.Faction))
            {
                return true;
            }

            if (Props.targetNeutrals && !pawn.HostileTo(parent.Faction) && pawn.Faction != parent.Faction)
            {
                return true;
            }

            return Props.targetAnimals && pawn.RaceProps.Animal;
        }

        private bool IsInRange(IntVec3 position)
        {
            return Vector3.Distance(parent.Position.ToVector3(), position.ToVector3()) <= Props.radius;
        }

        private void UpdateBursting()
        {
            if (Find.TickManager.TicksGame < nextInnerBurstTick)
            {
                return;
            }

            if (currentBurstCount >= currentTargets.Count)
            {
                currentState = BuildingBombardmentState.Idle;
                nextBurstTick = Find.TickManager.TicksGame + Props.burstIntervalTicks;
                Log.Message("[BuildingBombardment] Burst completed, waiting for next burst");
                return;
            }

            LocalTargetInfo target = currentTargets[currentBurstCount];
            LaunchBombardment(target);
            currentBurstCount++;

            if (currentBurstCount < currentTargets.Count)
            {
                nextInnerBurstTick = Find.TickManager.TicksGame + Props.innerBurstIntervalTicks;
            }
        }

        private void LaunchBombardment(LocalTargetInfo target)
        {
            try
            {
                IntVec3 targetCell = ApplyRandomOffset(target.Cell);
                if (Props.skyfallerDef != null)
                {
                    Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef);
                    GenSpawn.Spawn(skyfaller, targetCell, parent.Map);
                }
                else if (Props.projectileDef != null)
                {
                    LaunchProjectileAt(targetCell);
                }
            }
            catch (System.Exception ex)
            {
                Log.Message($"[BuildingBombardment] Error launching bombardment: {ex}");
            }
        }

        private IntVec3 ApplyRandomOffset(IntVec3 originalCell)
        {
            if (Props.randomOffset <= 0f)
            {
                return originalCell;
            }

            float offsetX = Rand.Range(-Props.randomOffset, Props.randomOffset);
            float offsetZ = Rand.Range(-Props.randomOffset, Props.randomOffset);
            IntVec3 offsetCell = new IntVec3(Mathf.RoundToInt(originalCell.x + offsetX), originalCell.y, Mathf.RoundToInt(originalCell.z + offsetZ));
            return offsetCell.InBounds(parent.Map) ? offsetCell : originalCell;
        }

        private void LaunchProjectileAt(IntVec3 targetCell)
        {
            Vector3 spawnPos = parent.Position.ToVector3Shifted();
            Projectile projectile = (Projectile)GenSpawn.Spawn(Props.projectileDef, parent.Position, parent.Map);
            projectile?.Launch(parent, spawnPos, new LocalTargetInfo(targetCell), new LocalTargetInfo(targetCell), ProjectileHitFlags.All, false);
        }

        public override void CompTick()
        {
            base.CompTick();
            switch (currentState)
            {
                case BuildingBombardmentState.Idle:
                    if (Find.TickManager.TicksGame >= nextBurstTick)
                    {
                        StartNextBurst();
                    }
                    break;
                case BuildingBombardmentState.Bursting:
                    UpdateBursting();
                    break;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentState, "currentState", BuildingBombardmentState.Idle);
            Scribe_Values.Look(ref nextBurstTick, "nextBurstTick");
            Scribe_Values.Look(ref currentBurstCount, "currentBurstCount");
            Scribe_Values.Look(ref nextInnerBurstTick, "nextInnerBurstTick");
            Scribe_Collections.Look(ref currentTargets, "currentTargets", LookMode.LocalTargetInfo);
        }
    }

    public enum BuildingBombardmentState
    {
        Idle,
        Targeting,
        Bursting
    }
}
