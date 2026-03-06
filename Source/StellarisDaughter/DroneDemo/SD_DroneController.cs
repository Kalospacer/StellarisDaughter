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
        public int SquadronSize = 1;
        public int OrbitLayer;
        public SD_DroneTypeDef DroneType;
        public List<SD_DroneEntity> Drones = new List<SD_DroneEntity>();
        public List<int> ReplacementTicksRemaining = new List<int>();

        public bool IsCharged => State == SD_DroneSlotState.Docked && ChargeTicksRemaining <= 0;

        public int ActiveDroneCount => Drones?.Count(drone => drone != null && !drone.Destroyed) ?? 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Index, "index");
            Scribe_Values.Look(ref State, "state", SD_DroneSlotState.Docked);
            Scribe_Values.Look(ref ChargeTicksRemaining, "chargeTicksRemaining", 0);
            Scribe_Values.Look(ref ChargeTicksTotal, "chargeTicksTotal", 0);
            Scribe_Values.Look(ref SquadronSize, "squadronSize", 1);
            Scribe_Values.Look(ref OrbitLayer, "orbitLayer", 0);
            Scribe_Defs.Look(ref DroneType, "droneType");
            Scribe_Collections.Look(ref Drones, "drones", LookMode.Reference);
            Scribe_Collections.Look(ref ReplacementTicksRemaining, "replacementTicksRemaining", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && Drones == null)
            {
                Drones = new List<SD_DroneEntity>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit && ReplacementTicksRemaining == null)
            {
                ReplacementTicksRemaining = new List<int>();
            }
        }
    }

    public class CompProperties_SD_DroneController : CompProperties
    {
        public int maxSlots = 4;
        public List<SD_DroneTypeDef> droneTypes;
        public List<SD_DroneTypeDef> slotDroneTypes;
        public List<int> slotSquadronSizes;
        public List<int> slotOrbitLayers;
        public List<float> orbitLayerRadii;
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
        private float gizmoScrollPosition;

        public CompProperties_SD_DroneController Props => (CompProperties_SD_DroneController)props;

        public Apparel Apparel => parent as Apparel;

        public Pawn Wearer => Apparel?.Wearer;

        public IReadOnlyList<SD_DroneSlot> Slots => slots;

        public bool AutoDeployEnabled => autoDeployEnabled;

        public float GizmoScrollPosition
        {
            get => gizmoScrollPosition;
            set => gizmoScrollPosition = Mathf.Max(0f, value);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref slots, "slots", LookMode.Deep);
            Scribe_Values.Look(ref lastThreatTick, "lastThreatTick", -99999);
            Scribe_Values.Look(ref autoDeployEnabled, "autoDeployEnabled", true);
            Scribe_Values.Look(ref gizmoScrollPosition, "gizmoScrollPosition", 0f);
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
                NormalizeSlotDrones(slot);
                TickReplacementQueue(slot);

                if (slot.State == SD_DroneSlotState.Charging)
                {
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
                else if ((slot.State == SD_DroneSlotState.Deployed || slot.State == SD_DroneSlotState.Returning) && slot.ActiveDroneCount <= 0)
                {
                    if (slot.State == SD_DroneSlotState.Deployed && HasPendingReplacements(slot))
                    {
                        continue;
                    }

                    StartCharging(slot);
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
                    DeployChargedDrones(false);
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            EnsureSlots();
            var docked = slots.Count(slot => slot.State == SD_DroneSlotState.Docked);
            var charging = slots.Count(slot => slot.State == SD_DroneSlotState.Charging);
            var deployed = slots.Count(slot => slot.ActiveDroneCount > 0);
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
            DeployChargedDrones(true);
        }

        public void RecallAllDrones()
        {
            EnsureSlots();
            for (var i = 0; i < slots.Count; i++)
            {
                RecallSlot(slots[i]);
            }
        }

        public Thing GetAttackTargetForDrone(SD_DroneEntity drone)
        {
            var wearer = Wearer;
            if (wearer?.Map == null)
            {
                return null;
            }

            var alertRange = ResolveAlertRange(drone.DroneType);
            if (alertRange <= 0f)
            {
                return null;
            }

            var primaryTarget = GetPrimaryThreatTarget();
            if (IsValidAttackTarget(drone, wearer, primaryTarget, alertRange))
            {
                return primaryTarget;
            }

            return GenClosest.ClosestThing_Global(
                wearer.Position,
                wearer.Map.attackTargetsCache.GetPotentialTargetsFor(wearer),
                alertRange,
                target => IsValidAttackTarget(drone, wearer, target, alertRange));
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
            var ownerCenter = wearer.DrawPos;
            var weaponRange = ResolveAttackRange(droneType);
            var alertRange = ResolveAlertRange(droneType);
            if (weaponRange <= 0f || alertRange <= 0f)
            {
                return false;
            }

            var targetCenter = target.DrawPos;
            var ownerMaxDistance = alertRange;
            var preferredDistance = Mathf.Max(1.5f, Mathf.Min(weaponRange * 0.6f, alertRange * 0.7f));
            var targetMinDistance = Mathf.Max(1.25f, Mathf.Min(weaponRange * 0.4f, alertRange * 0.45f));
            var targetMaxDistance = Mathf.Max(targetMinDistance, Mathf.Min(weaponRange * 0.75f, alertRange * 0.8f));
            var ownerPreferredDistance = Mathf.Max(1.5f, Mathf.Min(alertRange * 0.45f, preferredDistance));
            var layerDroneCount = Mathf.Max(GetLayerDroneCount(drone.OrbitLayer), 1);
            var layerDroneOrder = GetLayerDroneOrder(drone.SlotIndex, drone.SquadronMemberIndex, drone.OrbitLayer);
            var startAngle = (360f / layerDroneCount) * layerDroneOrder
                + drone.OrbitLayer * 18f
                + Find.TickManager.TicksGame * 3f;
            var bestPreferred = Vector3.zero;
            var bestPreferredScore = float.MaxValue;
            var bestFallback = Vector3.zero;
            var bestFallbackScore = float.MaxValue;

            for (var i = 0; i < 24; i++)
            {
                var angle = startAngle + i * 15f;
                var radius = ResolveCandidateTargetRadius(targetMinDistance, targetMaxDistance, preferredDistance);
                var candidate = targetCenter + new Vector3(0f, 0f, radius).RotatedBy(angle);
                candidate += GetAttackSpreadOffset(droneType, drone.SquadronMemberIndex, drone.SlotIndex, drone.OrbitLayer);
                var cell = candidate.ToIntVec3();
                if (!cell.InBounds(wearer.Map) || cell.Impassable(wearer.Map))
                {
                    continue;
                }

                var ownerDistance = (candidate - ownerCenter).Yto0().magnitude;
                if (ownerDistance > ownerMaxDistance)
                {
                    continue;
                }

                var targetDistance = (candidate - targetCenter).Yto0().magnitude;
                if (targetDistance < targetMinDistance || targetDistance > targetMaxDistance)
                {
                    continue;
                }

                var losTarget = target.OccupiedRect().ClosestCellTo(cell);
                if (!GenSight.LineOfSight(cell, losTarget, wearer.Map, skipFirstCell: true))
                {
                    continue;
                }

                var moveCost = (drone.DrawPos - candidate).Yto0().sqrMagnitude;
                var preferredTargetError = Mathf.Abs(targetDistance - preferredDistance);
                var ownerDistanceError = Mathf.Abs(ownerDistance - ownerPreferredDistance);
                var ownerDistanceRatio = ownerMaxDistance > 0.01f ? ownerDistance / ownerMaxDistance : 1f;
                var combinedScore = moveCost
                    + preferredTargetError * 5f
                    + ownerDistanceError * 6f
                    + ownerDistanceRatio * ownerDistanceRatio * 8f;
                if (preferredTargetError <= 1.25f && combinedScore < bestPreferredScore)
                {
                    bestPreferred = candidate;
                    bestPreferredScore = combinedScore;
                }

                if (combinedScore < bestFallbackScore)
                {
                    bestFallback = candidate;
                    bestFallbackScore = combinedScore;
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

        public Vector3 GetOrbitPosition(int slotIndex, SD_DroneTypeDef droneType = null, int squadronMemberIndex = 0, int squadronSize = 1, int orbitLayer = 0)
        {
            var wearer = Wearer;
            if (wearer == null)
            {
                return Vector3.zero;
            }

            var pos = GetIndependentOrbitPosition(slotIndex, droneType, squadronMemberIndex, squadronSize, orbitLayer, false);
            pos.y = wearer.DrawPos.y;
            return pos;
        }

        public Vector3 GetDockPosition(int slotIndex, SD_DroneTypeDef droneType = null, int squadronMemberIndex = 0, int squadronSize = 1, int orbitLayer = 0)
        {
            var wearer = Wearer;
            if (wearer == null)
            {
                return Vector3.zero;
            }

            var pos = GetIndependentOrbitPosition(slotIndex, droneType, squadronMemberIndex, squadronSize, orbitLayer, true);
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
            RemoveDroneReference(slot, drone);
            if (slot.ActiveDroneCount <= 0)
            {
                StartCharging(slot);
            }
        }

        public void NotifyDroneLost(int slotIndex, SD_DroneEntity drone)
        {
            EnsureSlots();
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return;
            }

            var slot = slots[slotIndex];
            RemoveDroneReference(slot, drone);
            if (slot.State == SD_DroneSlotState.Deployed)
            {
                QueueReplacement(slot);
            }

            if (slot.ActiveDroneCount <= 0)
            {
                if (slot.State == SD_DroneSlotState.Deployed && HasPendingReplacements(slot))
                {
                    return;
                }

                StartCharging(slot);
            }
        }

        public bool HasActiveDrones()
        {
            return slots.Any(slot => slot.ActiveDroneCount > 0);
        }

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
                DeployChargedDrones(true);
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
            if (slot.ActiveDroneCount > 0)
            {
                RecallSlot(slot);
                return;
            }

            if (slot.IsCharged)
            {
                TryDeploySlot(slot);
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
            return "SD_Drone_SlotCommandDesc".Translate(slot.Index + 1, droneTypeLabel, GetSlotShortState(slot), chargeText)
                + "\n"
                + $"Squadron: {slot.ActiveDroneCount}/{Mathf.Max(slot.SquadronSize, 1)}"
                + "\n"
                + $"Layer: {slot.OrbitLayer + 1}";
        }

        public int GetSquadronSize(SD_DroneSlot slot)
        {
            return Mathf.Max(slot?.SquadronSize ?? 1, 1);
        }

        public int GetActiveDroneCount(SD_DroneSlot slot)
        {
            return slot?.ActiveDroneCount ?? 0;
        }

        public bool IsOwnerForcedAttackTarget(Thing target)
        {
            var wearer = Wearer;
            if (wearer?.Map == null || target == null || target.Destroyed || !target.Spawned)
            {
                return false;
            }

            var forcedTarget = wearer.CurJob?.targetA.Thing;
            if (forcedTarget == null)
            {
                return false;
            }

            return forcedTarget == target && wearer.HostileTo(target);
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

            var alertRange = ResolveMaxAlertRange();
            if (alertRange <= 0f)
            {
                return null;
            }

            return GenClosest.ClosestThing_Global(
                wearer.Position,
                wearer.Map.attackTargetsCache.GetPotentialTargetsFor(wearer),
                alertRange,
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

        private float ResolveMaxAlertRange()
        {
            var maxRange = 0f;
            for (var i = 0; i < slots.Count; i++)
            {
                var droneType = slots[i].DroneType ?? ResolveDroneTypeForIndex(i);
                maxRange = Mathf.Max(maxRange, ResolveAlertRange(droneType));
            }

            return maxRange;
        }

        private bool IsValidAttackTarget(SD_DroneEntity drone, Pawn wearer, Thing target, float attackRange)
        {
            if (!IsPotentialThreatTarget(wearer, target))
            {
                return false;
            }

            if (IsOwnerForcedAttackTarget(target))
            {
                return true;
            }

            if (!GenSight.LineOfSight(drone.Position, target.Position, wearer.Map))
            {
                return false;
            }

            return (drone.DrawPos - target.DrawPos).Yto0().sqrMagnitude <= attackRange * attackRange;
        }

        private float ResolveAlertRange(SD_DroneTypeDef droneType)
        {
            if (droneType == null)
            {
                return 0f;
            }

            if (droneType.alertRange > 0f)
            {
                return droneType.alertRange;
            }

            var attackRange = ResolveAttackRange(droneType);
            return attackRange > 0f ? attackRange : 0f;
        }

        private void DeployChargedDrones(bool showMessage)
        {
            var wearer = Wearer;
            if (wearer == null || !wearer.Spawned)
            {
                if (showMessage)
                {
                    Messages.Message("SD_Drone_NoWearer".Translate(), MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            EnsureSlots();
            lastThreatTick = Find.TickManager.TicksGame;
            var deployedAny = false;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsCharged || slot.ActiveDroneCount > 0)
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
                Messages.Message("SD_Drone_NoChargedSlot".Translate(), MessageTypeDefOf.RejectInput, false);
            }
        }

        private bool TryDeploySlot(SD_DroneSlot slot)
        {
            var wearer = Wearer;
            var droneType = ResolveDroneTypeForSlot(slot.Index);
            if (wearer?.Map == null || droneType?.droneDef == null)
            {
                return false;
            }

            NormalizeSlotDrones(slot);
            var squadronSize = Mathf.Max(slot.SquadronSize, 1);
            var deployedAny = false;
            for (var memberIndex = 0; memberIndex < squadronSize; memberIndex++)
            {
                var drone = GenSpawn.Spawn(droneType.droneDef, wearer.Position, wearer.Map) as SD_DroneEntity;
                if (drone == null)
                {
                    continue;
                }

                slot.Drones.Add(drone);
                drone.Initialize(parent, slot.Index, droneType, memberIndex, squadronSize, slot.OrbitLayer);
                deployedAny = true;
            }

            if (!deployedAny)
            {
                return false;
            }

            slot.State = SD_DroneSlotState.Deployed;
            slot.DroneType = droneType;
            slot.ChargeTicksRemaining = 0;
            slot.ChargeTicksTotal = 0;
            slot.ReplacementTicksRemaining.Clear();
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
                    Index = index
                });
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.Index = i;
                slot.DroneType = ResolveDroneTypeForIndex(i);
                slot.SquadronSize = ResolveSquadronSizeForIndex(i);
                slot.OrbitLayer = ResolveOrbitLayerForIndex(i);
                NormalizeSlotDrones(slot);
            }
        }

        private void StartCharging(SD_DroneSlot slot)
        {
            var droneType = slot.DroneType ?? ResolveDroneTypeForSlot(slot.Index);
            var rechargeTicks = droneType?.rechargeTicks > 0 ? droneType.rechargeTicks : Props.rechargeTicks;
            slot.State = SD_DroneSlotState.Charging;
            slot.ChargeTicksTotal = Mathf.Max(rechargeTicks, 1);
            slot.ChargeTicksRemaining = slot.ChargeTicksTotal;
            slot.Drones.Clear();
            slot.ReplacementTicksRemaining.Clear();
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

        private int ResolveSquadronSizeForIndex(int slotIndex)
        {
            if (Props.slotSquadronSizes != null && slotIndex >= 0 && slotIndex < Props.slotSquadronSizes.Count)
            {
                return Mathf.Max(Props.slotSquadronSizes[slotIndex], 1);
            }

            return 1;
        }

        private int ResolveOrbitLayerForIndex(int slotIndex)
        {
            if (Props.slotOrbitLayers != null && slotIndex >= 0 && slotIndex < Props.slotOrbitLayers.Count)
            {
                return Mathf.Max(Props.slotOrbitLayers[slotIndex], 0);
            }

            return 0;
        }

        private void RecallSlot(SD_DroneSlot slot)
        {
            NormalizeSlotDrones(slot);
            if (slot.ActiveDroneCount <= 0)
            {
                return;
            }

            slot.State = SD_DroneSlotState.Returning;
            for (var i = 0; i < slot.Drones.Count; i++)
            {
                var drone = slot.Drones[i];
                if (drone == null || drone.Destroyed)
                {
                    continue;
                }

                drone.StartReturn();
            }
        }

        private void NormalizeSlotDrones(SD_DroneSlot slot)
        {
            if (slot.Drones == null)
            {
                slot.Drones = new List<SD_DroneEntity>();
                return;
            }

            slot.Drones.RemoveAll(drone => drone == null || drone.Destroyed);
            if (slot.ReplacementTicksRemaining == null)
            {
                slot.ReplacementTicksRemaining = new List<int>();
            }
        }

        private void RemoveDroneReference(SD_DroneSlot slot, SD_DroneEntity drone)
        {
            if (slot.Drones == null)
            {
                slot.Drones = new List<SD_DroneEntity>();
                return;
            }

            slot.Drones.RemoveAll(existing => existing == null || existing == drone || existing.Destroyed);
        }

        private void TickReplacementQueue(SD_DroneSlot slot)
        {
            if (slot.ReplacementTicksRemaining == null || slot.ReplacementTicksRemaining.Count == 0)
            {
                return;
            }

            for (var i = 0; i < slot.ReplacementTicksRemaining.Count; i++)
            {
                if (slot.ReplacementTicksRemaining[i] > 0)
                {
                    slot.ReplacementTicksRemaining[i]--;
                }
            }

            if (slot.State != SD_DroneSlotState.Deployed)
            {
                return;
            }

            while (slot.ActiveDroneCount < Mathf.Max(slot.SquadronSize, 1))
            {
                var readyIndex = slot.ReplacementTicksRemaining.FindIndex(ticks => ticks <= 0);
                if (readyIndex < 0)
                {
                    break;
                }

                if (!TryDeployReplacementDrone(slot))
                {
                    break;
                }

                slot.ReplacementTicksRemaining.RemoveAt(readyIndex);
            }
        }

        private void QueueReplacement(SD_DroneSlot slot)
        {
            if (slot.ReplacementTicksRemaining == null)
            {
                slot.ReplacementTicksRemaining = new List<int>();
            }

            var droneType = slot.DroneType ?? ResolveDroneTypeForSlot(slot.Index);
            var rechargeTicks = droneType?.rechargeTicks > 0 ? droneType.rechargeTicks : Props.rechargeTicks;
            slot.ReplacementTicksRemaining.Add(Mathf.Max(rechargeTicks, 1));
        }

        private bool HasPendingReplacements(SD_DroneSlot slot)
        {
            return slot.ReplacementTicksRemaining != null && slot.ReplacementTicksRemaining.Count > 0;
        }

        private bool TryDeployReplacementDrone(SD_DroneSlot slot)
        {
            var wearer = Wearer;
            var droneType = slot.DroneType ?? ResolveDroneTypeForSlot(slot.Index);
            if (wearer?.Map == null || droneType?.droneDef == null)
            {
                return false;
            }

            var memberIndex = ResolveAvailableMemberIndex(slot);
            var squadronSize = Mathf.Max(slot.SquadronSize, 1);
            var drone = GenSpawn.Spawn(droneType.droneDef, wearer.Position, wearer.Map) as SD_DroneEntity;
            if (drone == null)
            {
                return false;
            }

            slot.Drones.Add(drone);
            drone.Initialize(parent, slot.Index, droneType, memberIndex, squadronSize, slot.OrbitLayer);
            slot.State = SD_DroneSlotState.Deployed;
            return true;
        }

        private int ResolveAvailableMemberIndex(SD_DroneSlot slot)
        {
            var squadronSize = Mathf.Max(slot.SquadronSize, 1);
            var used = new bool[squadronSize];
            if (slot.Drones != null)
            {
                for (var i = 0; i < slot.Drones.Count; i++)
                {
                    var drone = slot.Drones[i];
                    if (drone == null || drone.Destroyed)
                    {
                        continue;
                    }

                    var memberIndex = drone.SquadronMemberIndex;
                    if (memberIndex >= 0 && memberIndex < squadronSize)
                    {
                        used[memberIndex] = true;
                    }
                }
            }

            for (var i = 0; i < used.Length; i++)
            {
                if (!used[i])
                {
                    return i;
                }
            }

            return Mathf.Clamp(slot.ActiveDroneCount, 0, squadronSize - 1);
        }

        private int GetLayerDroneOrder(int slotIndex, int squadronMemberIndex, int orbitLayer)
        {
            var order = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].OrbitLayer != orbitLayer)
                {
                    continue;
                }

                var squadronSize = Mathf.Max(slots[i].SquadronSize, 1);
                if (slots[i].Index == slotIndex)
                {
                    return order + Mathf.Clamp(squadronMemberIndex, 0, squadronSize - 1);
                }

                order += squadronSize;
            }

            return Mathf.Max(slotIndex + squadronMemberIndex, 0);
        }

        private int GetLayerDroneCount(int orbitLayer)
        {
            var count = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].OrbitLayer == orbitLayer)
                {
                    count += Mathf.Max(slots[i].SquadronSize, 1);
                }
            }

            return Mathf.Max(count, 1);
        }

        private float ResolveLayerRadius(int orbitLayer, SD_DroneTypeDef droneType)
        {
            if (Props.orbitLayerRadii != null && orbitLayer >= 0 && orbitLayer < Props.orbitLayerRadii.Count)
            {
                return Mathf.Max(Props.orbitLayerRadii[orbitLayer], 0.8f);
            }

            if (droneType?.orbitRadius > 0f)
            {
                return droneType.orbitRadius;
            }

            return Mathf.Max(Props.orbitRadius + orbitLayer * 1.6f, 0.8f);
        }

        private Vector3 GetIndependentOrbitPosition(int slotIndex, SD_DroneTypeDef droneType, int squadronMemberIndex, int squadronSize, int orbitLayer, bool docked)
        {
            var wearer = Wearer;
            if (wearer == null)
            {
                return Vector3.zero;
            }

            var countOnLayer = Mathf.Max(GetLayerDroneCount(orbitLayer), 1);
            var layerOrder = GetLayerDroneOrder(slotIndex, squadronMemberIndex, orbitLayer);
            var radius = ResolveLayerRadius(orbitLayer, droneType);
            if (docked)
            {
                radius *= 0.45f;
            }

            radius += GetLayerRadialOffset(droneType, squadronMemberIndex, squadronSize, orbitLayer, docked);

            var angle = (360f / countOnLayer) * layerOrder + orbitLayer * 12f;
            if (!docked)
            {
                angle += Find.TickManager.TicksGame * (2.2f + orbitLayer * 0.15f);
            }

            var offset = new Vector3(0f, 0f, radius).RotatedBy(angle);
            var pos = wearer.DrawPos + offset;
            pos.y = wearer.DrawPos.y;
            return pos;
        }

        private float GetLayerRadialOffset(SD_DroneTypeDef droneType, int squadronMemberIndex, int squadronSize, int orbitLayer, bool docked)
        {
            var count = Mathf.Max(squadronSize, 1);
            if (count <= 1)
            {
                return 0f;
            }

            var spreadRadius = Mathf.Max(droneType?.squadronSpreadRadius ?? 0.65f, 0.05f);
            if (docked)
            {
                spreadRadius *= 0.55f;
            }

            var midpoint = (count - 1) * 0.5f;
            return (squadronMemberIndex - midpoint) * spreadRadius;
        }

        private Vector3 GetAttackSpreadOffset(SD_DroneTypeDef droneType, int squadronMemberIndex, int slotIndex, int orbitLayer)
        {
            var spread = Mathf.Max((droneType?.squadronSpreadRadius ?? 0.65f) * 0.28f, 0.04f);
            var angle = slotIndex * 29f + squadronMemberIndex * 131f + orbitLayer * 17f;
            return new Vector3(0f, 0f, spread).RotatedBy(angle);
        }

        private float ResolveCandidateTargetRadius(float minTargetDistance, float maxTargetDistance, float preferredDistance)
        {
            var min = Mathf.Min(minTargetDistance, maxTargetDistance);
            var max = Mathf.Max(minTargetDistance, maxTargetDistance);
            if (Mathf.Approximately(min, max))
            {
                return min;
            }

            var spread = Mathf.Max((max - min) * 0.25f, 0.35f);
            var preferredMin = Mathf.Clamp(preferredDistance - spread, min, max);
            var preferredMax = Mathf.Clamp(preferredDistance + spread, min, max);
            if (preferredMin <= preferredMax && Rand.Chance(0.7f))
            {
                return Rand.Range(preferredMin, preferredMax);
            }

            return Rand.Range(min, max);
        }
    }
}
