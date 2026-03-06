using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public enum SD_DroneSlotState
    {
        Empty,
        Docked,
        Deployed,
        Returning,
        Charging
    }

    public class SD_DroneSlot : IExposable
    {
        public int Index;
        public SD_DroneSlotState State = SD_DroneSlotState.Docked;
        public int ChargeTicksRemaining;
        public int ChargeTicksTotal;
        public SD_DroneTypeDef DroneType;
        public SD_DroneEntity Drone;

        public bool IsCharged => State == SD_DroneSlotState.Docked && ChargeTicksRemaining <= 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Index, "index");
            Scribe_Values.Look(ref State, "state", SD_DroneSlotState.Docked);
            Scribe_Values.Look(ref ChargeTicksRemaining, "chargeTicksRemaining", 0);
            Scribe_Values.Look(ref ChargeTicksTotal, "chargeTicksTotal", 0);
            Scribe_Defs.Look(ref DroneType, "droneType");
            Scribe_References.Look(ref Drone, "drone");
        }
    }

    public class CompProperties_SD_DroneController : CompProperties
    {
        public int maxSlots = 4;
        public List<SD_DroneTypeDef> droneTypes;
        public List<SD_DroneTypeDef> slotDroneTypes;
        public int rechargeTicks = 180;
        public float orbitRadius = 1.8f;
        public float moveSpeed = 0.18f;
        public int deployTicks = 24;
        public bool autoDeployOnThreat = true;
        public int idleRecallTicks = 90;
        public int targetScanIntervalTicks = 15;
        public string deployCommandLabel = "SD_Drone_DeployCommandLabel";
        public string recallCommandLabel = "SD_Drone_RecallCommandLabel";
        public string commandDesc = "SD_Drone_CommandDesc";

        public CompProperties_SD_DroneController()
        {
            compClass = typeof(CompSD_DroneController);
        }
    }

    public class CompSD_DroneController : ThingComp
    {
        private List<SD_DroneSlot> slots = new List<SD_DroneSlot>();
        private int lastThreatTick = -99999;
        private bool autoDeployEnabled = true;

        public CompProperties_SD_DroneController Props => (CompProperties_SD_DroneController)props;

        public Apparel Apparel => parent as Apparel;

        public Pawn Wearer => Apparel?.Wearer;

        public IReadOnlyList<SD_DroneSlot> Slots => slots;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref slots, "slots", LookMode.Deep);
            Scribe_Values.Look(ref lastThreatTick, "lastThreatTick", -99999);
            Scribe_Values.Look(ref autoDeployEnabled, "autoDeployEnabled", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && slots == null)
            {
                slots = new List<SD_DroneSlot>();
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            autoDeployEnabled = Props.autoDeployOnThreat;
            EnsureSlots();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureSlots();
        }

        public override void CompTick()
        {
            base.CompTick();
            EnsureSlots();

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.Drone != null && slot.Drone.Destroyed)
                {
                    slot.Drone = null;
                    if (slot.State == SD_DroneSlotState.Deployed || slot.State == SD_DroneSlotState.Returning)
                    {
                        StartCharging(slot);
                    }
                }

                if (slot.State != SD_DroneSlotState.Charging)
                {
                    continue;
                }

                if (slot.ChargeTicksRemaining > 0)
                {
                    slot.ChargeTicksRemaining--;
                }

                if (slot.ChargeTicksRemaining <= 0)
                {
                    slot.ChargeTicksRemaining = 0;
                    slot.State = SD_DroneSlotState.Docked;
                }
            }

            var wearer = Wearer;
            if (wearer == null || !wearer.Spawned || wearer.Dead)
            {
                RecallAllDrones();
                return;
            }

            var threat = GetPrimaryThreatTarget();
            if (threat != null)
            {
                lastThreatTick = Find.TickManager.TicksGame;
                if (autoDeployEnabled)
                {
                    DeployChargedDrones(showMessage: false);
                }
            }
            else if (HasActiveDrones() && Find.TickManager.TicksGame - lastThreatTick >= Props.idleRecallTicks)
            {
                RecallAllDrones();
            }
        }

        public override string CompInspectStringExtra()
        {
            EnsureSlots();
            var docked = slots.Count(slot => slot.State == SD_DroneSlotState.Docked);
            var charging = slots.Count(slot => slot.State == SD_DroneSlotState.Charging);
            var deployed = slots.Count(slot => slot.State == SD_DroneSlotState.Deployed);
            return "SD_Drone_Inspect".Translate(docked, slots.Count, charging, deployed);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (var gizmo in base.CompGetWornGizmosExtra())
            {
                yield return gizmo;
            }

            var wearer = Wearer;
            if (wearer == null || !wearer.IsColonistPlayerControlled)
            {
                yield break;
            }

            yield return new Gizmo_SDDroneControl(this);
        }

        public void DeployAllChargedDrones()
        {
            DeployChargedDrones(showMessage: true);
        }

        public void RecallAllDrones()
        {
            EnsureSlots();
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.Drone == null || slot.Drone.Destroyed)
                {
                    continue;
                }

                slot.State = SD_DroneSlotState.Returning;
                slot.Drone.StartReturn();
            }
        }

        public Thing GetAttackTargetForDrone(SD_DroneEntity drone)
        {
            var wearer = Wearer;
            if (wearer?.Map == null)
            {
                return null;
            }

            var attackRange = ResolveAttackRange(drone.DroneType);
            if (attackRange <= 0f)
            {
                return null;
            }

            var primaryTarget = GetPrimaryThreatTarget();
            if (IsValidAttackTarget(drone, wearer, primaryTarget, attackRange))
            {
                return primaryTarget;
            }

            return GenClosest.ClosestThing_Global(
                wearer.Position,
                wearer.Map.attackTargetsCache.GetPotentialTargetsFor(wearer),
                attackRange,
                target => IsValidAttackTarget(drone, wearer, target, attackRange));
        }

        public bool TryFindAttackDestination(SD_DroneEntity drone, Thing target, out Vector3 result)
        {
            result = Vector3.zero;
            var wearer = Wearer;
            if (wearer?.Map == null || target == null || target.Destroyed || !target.Spawned)
            {
                return false;
            }

            var droneType = drone.DroneType;
            var preferredDistance = Mathf.Max(1.5f, droneType?.preferredTargetDistance ?? 4f);
            var jitter = Mathf.Max(0f, droneType?.targetDistanceJitter ?? 0.6f);
            var targetCenter = target.DrawPos;
            var startAngle = (360f / Mathf.Max(Props.maxSlots, 1)) * drone.SlotIndex + Find.TickManager.TicksGame * 3f;
            var bestPreferred = Vector3.zero;
            var bestPreferredScore = float.MaxValue;
            var bestFallback = Vector3.zero;
            var bestFallbackScore = float.MaxValue;

            for (var i = 0; i < 12; i++)
            {
                var angle = startAngle + i * 30f;
                var radius = preferredDistance + Rand.Range(-jitter, jitter);
                var candidate = targetCenter + new Vector3(0f, 0f, radius).RotatedBy(angle);
                var cell = candidate.ToIntVec3();
                if (!cell.InBounds(wearer.Map) || cell.Impassable(wearer.Map))
                {
                    continue;
                }

                var losTarget = target.OccupiedRect().ClosestCellTo(cell);
                if (!GenSight.LineOfSight(cell, losTarget, wearer.Map, skipFirstCell: true))
                {
                    continue;
                }

                var score = (drone.DrawPos - candidate).Yto0().sqrMagnitude;
                var distanceError = Mathf.Abs((candidate - targetCenter).Yto0().magnitude - preferredDistance);
                if (distanceError <= 1.25f && score < bestPreferredScore)
                {
                    bestPreferred = candidate;
                    bestPreferredScore = score;
                }

                if (score < bestFallbackScore)
                {
                    bestFallback = candidate;
                    bestFallbackScore = score;
                }
            }

            if (bestPreferredScore < float.MaxValue)
            {
                result = bestPreferred;
                return true;
            }

            if (bestFallbackScore < float.MaxValue)
            {
                result = bestFallback;
                return true;
            }

            return false;
        }

        public Vector3 GetOrbitPosition(int slotIndex, SD_DroneTypeDef droneType = null)
        {
            var wearer = Wearer;
            if (wearer == null)
            {
                return Vector3.zero;
            }

            var count = Mathf.Max(Props.maxSlots, 1);
            var orbitRadius = droneType?.orbitRadius > 0f ? droneType.orbitRadius : Props.orbitRadius;
            var angle = (360f / count) * slotIndex + Find.TickManager.TicksGame * 2.2f;
            var offset = new Vector3(0f, 0f, orbitRadius).RotatedBy(angle);
            var pos = wearer.DrawPos + offset;
            pos.y = wearer.DrawPos.y;
            return pos;
        }

        public Vector3 GetDockPosition(int slotIndex, SD_DroneTypeDef droneType = null)
        {
            var wearer = Wearer;
            if (wearer == null)
            {
                return Vector3.zero;
            }

            var count = Mathf.Max(Props.maxSlots, 1);
            var orbitRadius = droneType?.orbitRadius > 0f ? droneType.orbitRadius : Props.orbitRadius;
            var angle = (360f / count) * slotIndex;
            var offset = new Vector3(0f, 0f, orbitRadius * 0.45f).RotatedBy(angle);
            var pos = wearer.DrawPos + offset;
            pos.y = wearer.DrawPos.y;
            return pos;
        }

        public void NotifyDroneReturned(int slotIndex, SD_DroneEntity drone)
        {
            EnsureSlots();
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return;
            }

            var slot = slots[slotIndex];
            if (slot.Drone == drone)
            {
                slot.Drone = null;
            }

            StartCharging(slot);
        }

        public void NotifyDroneLost(int slotIndex, SD_DroneEntity drone)
        {
            EnsureSlots();
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return;
            }

            var slot = slots[slotIndex];
            if (slot.Drone == drone)
            {
                slot.Drone = null;
            }

            StartCharging(slot);
        }

        public bool HasActiveDrones()
        {
            return slots.Any(slot => slot.Drone != null && !slot.Drone.Destroyed);
        }

        public bool AutoDeployEnabled => autoDeployEnabled;

        public void ToggleAutoDeploy()
        {
            autoDeployEnabled = !autoDeployEnabled;
        }

        public void ToggleAllDrones()
        {
            if (HasActiveDrones())
            {
                RecallAllDrones();
            }
            else
            {
                DeployChargedDrones(showMessage: true);
            }
        }

        public void ToggleSlot(int slotIndex)
        {
            EnsureSlots();
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return;
            }

            var slot = slots[slotIndex];
            if (slot.Drone != null && !slot.Drone.Destroyed)
            {
                slot.State = SD_DroneSlotState.Returning;
                slot.Drone.StartReturn();
                return;
            }

            if (slot.IsCharged)
            {
                TryDeploySlot(slot);
            }
        }

        private void DeployChargedDrones(bool showMessage)
        {
            var wearer = Wearer;
            if (wearer == null || !wearer.Spawned)
            {
                if (showMessage)
                {
                    Messages.Message("SD_Drone_NoWearer".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                }
                return;
            }

            EnsureSlots();
            var deployedAny = false;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsCharged || slot.Drone != null)
                {
                    continue;
                }

                if (TryDeploySlot(slot))
                {
                    deployedAny = true;
                }
            }

            if (!deployedAny && showMessage)
            {
                Messages.Message("SD_Drone_NoChargedSlot".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            }
        }

        public string GetSlotShortState(SD_DroneSlot slot)
        {
            switch (slot.State)
            {
                case SD_DroneSlotState.Docked:
                    return slot.IsCharged ? "SD_Drone_SlotStateReady".Translate().ToString() : "SD_Drone_SlotStateDocked".Translate().ToString();
                case SD_DroneSlotState.Deployed:
                    return "SD_Drone_SlotStateDeployed".Translate().ToString();
                case SD_DroneSlotState.Returning:
                    return "SD_Drone_SlotStateReturning".Translate().ToString();
                case SD_DroneSlotState.Charging:
                    return "SD_Drone_SlotStateCharging".Translate().ToString();
                default:
                    return "SD_Drone_SlotStateEmpty".Translate().ToString();
            }
        }

        public string BuildSlotDescription(SD_DroneSlot slot)
        {
            var droneType = slot.DroneType ?? ResolveDroneTypeForIndex(slot.Index);
            var droneTypeLabel = droneType?.label ?? droneType?.defName ?? "None";
            var chargeText = slot.State == SD_DroneSlotState.Charging
                ? slot.ChargeTicksRemaining.ToStringTicksToPeriod()
                : "SD_Drone_SlotChargeReady".Translate().ToString();
            return "SD_Drone_SlotCommandDesc".Translate(slot.Index + 1, droneTypeLabel, GetSlotShortState(slot), chargeText);
        }

        private Thing GetPrimaryThreatTarget()
        {
            var wearer = Wearer;
            if (wearer?.Map == null)
            {
                return null;
            }

            var wearerTarget = wearer.CurJob?.targetA.Thing ?? wearer.mindState?.enemyTarget;
            if (IsPotentialThreatTarget(wearer, wearerTarget))
            {
                return wearerTarget;
            }

            var maxRange = ResolveMaxRange();
            if (maxRange <= 0f)
            {
                return null;
            }

            return GenClosest.ClosestThing_Global(
                wearer.Position,
                wearer.Map.attackTargetsCache.GetPotentialTargetsFor(wearer),
                maxRange,
                target => IsPotentialThreatTarget(wearer, target));
        }

        private bool IsPotentialThreatTarget(Pawn wearer, Thing target)
        {
            if (wearer?.Map == null || target == null || target.Destroyed || !target.Spawned)
            {
                return false;
            }

            if (target == wearer || !wearer.HostileTo(target))
            {
                return false;
            }

            return !target.Position.Fogged(wearer.Map);
        }

        private float ResolveAttackRange(SD_DroneTypeDef droneType)
        {
            if (droneType?.verbs.NullOrEmpty() ?? true)
            {
                return 0f;
            }

            return droneType.verbs.Max(v => v.range);
        }

        private float ResolveMaxRange()
        {
            var maxRange = 0f;
            for (var i = 0; i < slots.Count; i++)
            {
                var droneType = slots[i].DroneType ?? ResolveDroneTypeForIndex(i);
                maxRange = Mathf.Max(maxRange, ResolveAttackRange(droneType));
            }

            return maxRange;
        }

        private bool IsValidAttackTarget(SD_DroneEntity drone, Pawn wearer, Thing target, float attackRange)
        {
            if (!IsPotentialThreatTarget(wearer, target))
            {
                return false;
            }

            if (!GenSight.LineOfSight(drone.Position, target.Position, wearer.Map))
            {
                return false;
            }

            return (drone.DrawPos - target.DrawPos).Yto0().sqrMagnitude <= attackRange * attackRange;
        }

        private bool TryDeploySlot(SD_DroneSlot slot)
        {
            var wearer = Wearer;
            var droneType = ResolveDroneTypeForSlot(slot.Index);
            if (wearer?.Map == null || droneType?.droneDef == null)
            {
                return false;
            }

            var drone = GenSpawn.Spawn(droneType.droneDef, wearer.Position, wearer.Map) as SD_DroneEntity;
            if (drone == null)
            {
                return false;
            }

            slot.State = SD_DroneSlotState.Deployed;
            slot.DroneType = droneType;
            slot.Drone = drone;
            slot.ChargeTicksRemaining = 0;
            slot.ChargeTicksTotal = 0;
            drone.Initialize(parent, slot.Index, droneType);
            return true;
        }

        private void EnsureSlots()
        {
            var desiredCount = Mathf.Max(Props.maxSlots, 1);
            while (slots.Count < desiredCount)
            {
                var index = slots.Count;
                slots.Add(new SD_DroneSlot
                {
                    Index = index,
                    State = SD_DroneSlotState.Docked,
                    DroneType = ResolveDroneTypeForIndex(index)
                });
            }
        }

        private void StartCharging(SD_DroneSlot slot)
        {
            var droneType = slot.DroneType ?? ResolveDroneTypeForSlot(slot.Index);
            var rechargeTicks = droneType?.rechargeTicks > 0 ? droneType.rechargeTicks : Props.rechargeTicks;
            slot.State = SD_DroneSlotState.Charging;
            slot.ChargeTicksTotal = Mathf.Max(rechargeTicks, 1);
            slot.ChargeTicksRemaining = slot.ChargeTicksTotal;
        }

        private SD_DroneTypeDef ResolveDroneTypeForSlot(int slotIndex)
        {
            return ResolveDroneTypeForIndex(slotIndex);
        }

        private SD_DroneTypeDef ResolveDroneTypeForIndex(int slotIndex)
        {
            if (Props.slotDroneTypes != null && slotIndex >= 0 && slotIndex < Props.slotDroneTypes.Count && Props.slotDroneTypes[slotIndex] != null)
            {
                return Props.slotDroneTypes[slotIndex];
            }

            if (Props.droneTypes != null && Props.droneTypes.Count > 0)
            {
                return Props.droneTypes[slotIndex % Props.droneTypes.Count];
            }

            return null;
        }
    }
}
