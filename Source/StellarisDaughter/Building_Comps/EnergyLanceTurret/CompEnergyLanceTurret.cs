using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class CompEnergyLanceTurret : ThingComp
    {
        public CompProperties_EnergyLanceTurret Props => (CompProperties_EnergyLanceTurret)props;

        private Pawn currentTarget;
        private EnergyLance activeLance;
        private int lastTargetUpdateTick;
        private int warmupTicksRemaining;
        private int cooldownTicksRemaining;
        private bool isActive;
        private IntVec3 lastTargetPosition;
        private int lastPositionUpdateTick;
        private int debugTickCounter;
        private int lanceCreationTick = -1;
        private int targetLostTick = -1;
        private TurretState currentState = TurretState.Idle;

        private const int DebugLogInterval = 120;
        private const int LanceGracePeriod = 60;
        private const int TargetLostGracePeriod = 60;

        private enum TurretState
        {
            Idle,
            WarmingUp,
            Firing,
            CoolingDown
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                ResetState();
            }
            Log.Message($"[EnergyLanceTurret] Spawned at {parent.Position}, range: {Props.detectionRange}");
        }

        public override void CompTick()
        {
            base.CompTick();
            debugTickCounter++;
            if (parent.Destroyed || parent.Map == null)
            {
                return;
            }

            if (debugTickCounter % DebugLogInterval == 0)
            {
                OutputDebugInfo();
            }

            switch (currentState)
            {
                case TurretState.CoolingDown:
                    HandleCoolingDown();
                    break;
                case TurretState.WarmingUp:
                    HandleWarmingUp();
                    break;
                case TurretState.Firing:
                    HandleFiring();
                    break;
                case TurretState.Idle:
                    HandleIdle();
                    break;
            }
        }

        private void StartEnergyLance()
        {
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                List<Pawn> potentialTargets = FindPotentialTargets();
                if (potentialTargets.Count > 0)
                {
                    currentTarget = potentialTargets.OrderBy(t => t.Position.DistanceTo(parent.Position)).First();
                }
                else
                {
                    StartCooldown();
                    return;
                }
            }

            try
            {
                ThingDef lanceDef = Props.energyLanceDef ?? ThingDef.Named("EnergyLance");
                activeLance = EnergyLance.MakeEnergyLance(
                    lanceDef,
                    currentTarget.Position,
                    currentTarget.Position,
                    parent.Map,
                    Props.energyLanceMoveDistance,
                    false,
                    Props.energyLanceDuration,
                    instigatorPawn: null);

                if (activeLance == null)
                {
                    StartCooldown();
                    return;
                }

                activeLance.instigator = parent;
                lanceCreationTick = Find.TickManager.TicksGame;
                lastTargetPosition = currentTarget.Position;
                lastPositionUpdateTick = Find.TickManager.TicksGame;
                targetLostTick = -1;
                currentState = TurretState.Firing;
                UpdateEnergyLancePosition();
            }
            catch (System.Exception ex)
            {
                Log.Message($"[EnergyLanceTurret] Startup error: {ex}");
                StartCooldown();
            }
        }

        private void UpdateEnergyLancePosition()
        {
            if (activeLance == null || activeLance.Destroyed)
            {
                return;
            }

            if (currentTarget != null && IsTargetValid(currentTarget))
            {
                UpdateLanceTargetPosition(currentTarget.Position);
            }
            else if (lastTargetPosition.IsValid && Find.TickManager.TicksGame - lastPositionUpdateTick <= Props.targetUpdateInterval * 2)
            {
                UpdateLanceTargetPosition(lastTargetPosition);
            }
            else
            {
                UpdateLanceTargetPosition(IntVec3.Invalid);
            }
        }

        private void HandleCoolingDown()
        {
            cooldownTicksRemaining--;
            if (cooldownTicksRemaining <= 0)
            {
                currentState = TurretState.Idle;
                isActive = false;
            }
        }

        private void HandleWarmingUp()
        {
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                ResetState();
                return;
            }

            warmupTicksRemaining--;
            if (warmupTicksRemaining <= 0)
            {
                StartEnergyLance();
            }
        }

        private void HandleFiring()
        {
            if (Find.TickManager.TicksGame - lastTargetUpdateTick >= Props.targetUpdateInterval)
            {
                UpdateTarget();
                lastTargetUpdateTick = Find.TickManager.TicksGame;
            }

            if (activeLance != null && !activeLance.Destroyed)
            {
                UpdateEnergyLancePosition();
            }

            CheckEnergyLanceValidity();
        }

        private void HandleIdle()
        {
            if (Find.TickManager.TicksGame - lastTargetUpdateTick >= Props.targetUpdateInterval)
            {
                UpdateTarget();
                lastTargetUpdateTick = Find.TickManager.TicksGame;
            }
        }

        private void OutputDebugInfo()
        {
            List<Pawn> targets = FindPotentialTargets();
            Log.Message($"[EnergyLanceTurret] State={currentState}, Target={currentTarget?.LabelCap ?? "None"}, LanceActive={activeLance != null && !activeLance.Destroyed}, Targets={targets.Count}");
        }

        private void ResetState()
        {
            currentTarget = null;
            activeLance = null;
            lastTargetUpdateTick = Find.TickManager.TicksGame;
            warmupTicksRemaining = 0;
            cooldownTicksRemaining = 0;
            isActive = false;
            lanceCreationTick = -1;
            targetLostTick = -1;
            currentState = TurretState.Idle;
        }

        private void UpdateTarget()
        {
            if (activeLance == null || activeLance.Destroyed)
            {
                FindNewTarget();
                return;
            }

            if (currentTarget != null && IsTargetValid(currentTarget))
            {
                lastTargetPosition = currentTarget.Position;
                lastPositionUpdateTick = Find.TickManager.TicksGame;
                targetLostTick = -1;
                return;
            }

            FindNewTargetForExistingLance();
        }

        private void FindNewTarget()
        {
            if (currentState != TurretState.Idle)
            {
                return;
            }

            List<Pawn> potentialTargets = FindPotentialTargets();
            if (potentialTargets.Count <= 0)
            {
                return;
            }

            currentTarget = potentialTargets.OrderBy(t => t.Position.DistanceTo(parent.Position)).First();
            StartWarmup();
        }

        private void FindNewTargetForExistingLance()
        {
            if (activeLance == null || activeLance.Destroyed)
            {
                return;
            }

            List<Pawn> potentialTargets = FindPotentialTargets();
            if (potentialTargets.Count > 0)
            {
                currentTarget = potentialTargets.OrderBy(t => t.Position.DistanceTo(activeLance.Position)).First();
                lastTargetPosition = currentTarget.Position;
                lastPositionUpdateTick = Find.TickManager.TicksGame;
                targetLostTick = -1;
                return;
            }

            if (targetLostTick < 0)
            {
                targetLostTick = Find.TickManager.TicksGame;
            }

            currentTarget = null;
            lastTargetPosition = IntVec3.Invalid;
        }

        private List<Pawn> FindPotentialTargets()
        {
            List<Pawn> targets = new List<Pawn>();
            Map map = parent.Map;
            if (map == null)
            {
                return targets;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(p => p.Position.DistanceTo(parent.Position) <= Props.detectionRange))
            {
                if (IsValidTarget(pawn) && CanShootAtTarget(pawn))
                {
                    targets.Add(pawn);
                }
            }

            return targets;
        }

        private bool IsValidTarget(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Downed || pawn.Dead)
            {
                return false;
            }

            if (pawn.Faction != null)
            {
                bool isHostile = pawn.HostileTo(parent.Faction);
                bool isNeutral = !pawn.HostileTo(parent.Faction) && pawn.Faction != parent.Faction;
                if (Props.targetHostileFactions && isHostile)
                {
                    return true;
                }
                if (Props.targetNeutrals && isNeutral)
                {
                    return true;
                }
                return false;
            }

            if (pawn.RaceProps.Animal && !Props.targetAnimals)
            {
                return false;
            }

            if (pawn.RaceProps.IsMechanoid && !Props.targetMechs)
            {
                return false;
            }

            return true;
        }

        private bool CanShootAtTarget(Pawn target)
        {
            return target != null && (!Props.requireLineOfSight || GenSight.LineOfSight(parent.Position, target.Position, parent.Map, true));
        }

        private bool IsTargetValid(Pawn target)
        {
            return IsValidTarget(target)
                && target.Position.DistanceTo(parent.Position) <= Props.detectionRange
                && (!Props.requireLineOfSight || GenSight.LineOfSight(parent.Position, target.Position, parent.Map, true));
        }

        private void StartWarmup()
        {
            if (currentTarget == null)
            {
                return;
            }

            warmupTicksRemaining = Props.warmupTicks;
            isActive = true;
            currentState = TurretState.WarmingUp;
        }

        private void UpdateLanceTargetPosition(IntVec3 targetPos)
        {
            if (activeLance == null || activeLance.Destroyed)
            {
                return;
            }

            activeLance.UpdateTargetPosition(targetPos);
        }

        private void CheckEnergyLanceValidity()
        {
            if (activeLance == null || activeLance.Destroyed)
            {
                StartCooldown();
                return;
            }

            if (lanceCreationTick >= 0 && Find.TickManager.TicksGame - lanceCreationTick < LanceGracePeriod)
            {
                return;
            }

            if (targetLostTick >= 0 && Find.TickManager.TicksGame - targetLostTick > TargetLostGracePeriod)
            {
                activeLance.Destroy();
                StartCooldown();
                return;
            }

            if (Find.TickManager.TicksGame - lastPositionUpdateTick > Props.targetUpdateInterval * 3)
            {
                activeLance.Destroy();
                StartCooldown();
            }
        }

        private void StartCooldown()
        {
            cooldownTicksRemaining = Props.cooldownTicks;
            isActive = false;
            currentTarget = null;
            activeLance = null;
            lanceCreationTick = -1;
            targetLostTick = -1;
            currentState = TurretState.CoolingDown;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (!Find.Selector.IsSelected(parent))
            {
                return;
            }

            GenDraw.DrawRadiusRing(parent.Position, Props.detectionRange, Color.red);
            if (currentTarget != null && !currentTarget.Destroyed)
            {
                GenDraw.DrawLineBetween(parent.DrawPos, currentTarget.DrawPos, SimpleColor.Red, 0.2f);
                GenDraw.DrawTargetHighlight(currentTarget.Position);
            }

            if (activeLance != null && !activeLance.Destroyed)
            {
                GenDraw.DrawLineBetween(parent.DrawPos, activeLance.DrawPos, SimpleColor.Yellow, 0.3f);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref currentTarget, "currentTarget");
            Scribe_References.Look(ref activeLance, "activeLance");
            Scribe_Values.Look(ref lastTargetUpdateTick, "lastTargetUpdateTick");
            Scribe_Values.Look(ref warmupTicksRemaining, "warmupTicksRemaining");
            Scribe_Values.Look(ref cooldownTicksRemaining, "cooldownTicksRemaining");
            Scribe_Values.Look(ref isActive, "isActive");
            Scribe_Values.Look(ref lastTargetPosition, "lastTargetPosition");
            Scribe_Values.Look(ref lastPositionUpdateTick, "lastPositionUpdateTick");
            Scribe_Values.Look(ref lanceCreationTick, "lanceCreationTick", -1);
            Scribe_Values.Look(ref targetLostTick, "targetLostTick", -1);
            Scribe_Values.Look(ref currentState, "currentState", TurretState.Idle);
        }

        public override string CompInspectStringExtra()
        {
            string targetInfo = currentTarget != null ? $"\nTarget: {currentTarget.LabelCap}" : string.Empty;
            return $"{currentState}{targetInfo}\nRange: {Props.detectionRange}";
        }
    }
}
