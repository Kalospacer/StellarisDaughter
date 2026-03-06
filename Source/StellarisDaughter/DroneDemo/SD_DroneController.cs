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

    // 鉁?娌愰洩鍐欑殑鍝
    public class CompProperties_SD_DroneController : CompProperties
    {
        public int maxSlots = 4;
        public List<SD_DroneTypeDef> droneTypes;
        public List<SD_DroneTypeDef> slotDroneTypes;
        public int rechargeTicks = 180;
        public float orbitRadius = 1.8f;
        public float moveSpeed = 0.18f;
        public int deployTicks = 24;
        public string deployCommandLabel = "SD_Drone_DeployCommandLabel";
        public string recallCommandLabel = "SD_Drone_RecallCommandLabel";
        public string commandDesc = "SD_Drone_CommandDesc";

        public CompProperties_SD_DroneController()
        {
            compClass = typeof(CompSD_DroneController);
        }
    }

    // 鉁?娌愰洩鍐欑殑鍝
    public class CompSD_DroneController : ThingComp
    {
        private List<SD_DroneSlot> slots = new List<SD_DroneSlot>();

        public CompProperties_SD_DroneController Props => (CompProperties_SD_DroneController)props;

        public Apparel Apparel => parent as Apparel;

        public Pawn Wearer => Apparel?.Wearer;

        public IReadOnlyList<SD_DroneSlot> Slots => slots;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref slots, "slots", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && slots == null)
            {
                slots = new List<SD_DroneSlot>();
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
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

            yield return new Command_Action
            {
                defaultLabel = HasActiveDrones()
                    ? Props.recallCommandLabel.Translate()
                    : Props.deployCommandLabel.Translate(),
                defaultDesc = Props.commandDesc.Translate(),
                icon = TexCommand.Attack,
                action = delegate
                {
                    if (HasActiveDrones())
                    {
                        RecallAllDrones();
                    }
                    else
                    {
                        DeployAllChargedDrones();
                    }
                }
            };
        }

        public void DeployAllChargedDrones()
        {
            var wearer = Wearer;
            if (wearer == null || !wearer.Spawned)
            {
                Messages.Message("SD_Drone_NoWearer".Translate(), MessageTypeDefOf.RejectInput, historical: false);
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

            if (!deployedAny)
            {
                Messages.Message("SD_Drone_NoChargedSlot".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            }
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

            return GenClosest.ClosestThing_Global(
                wearer.Position,
                wearer.Map.attackTargetsCache.GetPotentialTargetsFor(wearer),
                attackRange,
                target =>
                {
                    if (target == null || target.Destroyed || !target.Spawned)
                    {
                        return false;
                    }

                    if (target == wearer || !wearer.HostileTo(target))
                    {
                        return false;
                    }

                    if (!GenSight.LineOfSight(drone.Position, target.Position, wearer.Map))
                    {
                        return false;
                    }

                    return (drone.DrawPos - target.DrawPos).Yto0().sqrMagnitude <= attackRange * attackRange;
                });
        }

        private float ResolveAttackRange(SD_DroneTypeDef droneType)
        {
            if (droneType?.verbs.NullOrEmpty() ?? true)
            {
                return 0f;
            }

            return droneType.verbs.Max(v => v.range);
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

