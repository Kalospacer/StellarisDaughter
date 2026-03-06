using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public enum SD_DroneRuntimeState
    {
        Launching,
        MovingToAttackCell,
        Attacking,
        PostAttackWaiting,
        Returning
    }

    public class SD_DroneEntity : ThingWithComps, IVerbOwner
    {
        private Thing controllerThing;
        private int slotIndex = -1;
        private SD_DroneRuntimeState state = SD_DroneRuntimeState.Launching;
        private SD_DroneTypeDef droneType;
        private VerbTracker verbTracker;
        private Vector3 realPos;
        private Vector3 realDir = Vector3.forward;
        private Thing currentTarget;
        private Vector3 attackDestination;
        private int attackCooldownTicksLeft;
        private int preAttackAimTicksLeft;
        private int postAttackWaitTicksLeft;
        private int completedAttackCycles;
        private bool notifiedController;

        private Vector3 pathStart;
        private Vector3 pathControlA;
        private Vector3 pathControlB;
        private Vector3 pathEnd;
        private int pathTicksElapsed;
        private int pathTicksTotal;

        private CompSD_DroneController Controller => controllerThing?.TryGetComp<CompSD_DroneController>();

        public Pawn Owner => Controller?.Wearer;

        public Thing ControllerThing => controllerThing;

        public int SlotIndex => slotIndex;

        public SD_DroneTypeDef DroneType => droneType;

        public VerbTracker VerbTracker => verbTracker ??= new VerbTracker(this);

        public List<Verb> AllVerbs => VerbTracker.AllVerbs;

        public Verb PrimaryVerb => VerbTracker.PrimaryVerb;

        List<VerbProperties> IVerbOwner.VerbProperties => droneType?.verbs;

        List<Tool> IVerbOwner.Tools => null;

        ImplementOwnerTypeDef IVerbOwner.ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

        Thing IVerbOwner.ConstantCaster => this;

        public override Vector3 DrawPos
        {
            get
            {
                var pos = realPos;
                pos.y = def.Altitude;
                return pos;
            }
        }

        public void Initialize(Thing parentThing, int newSlotIndex, SD_DroneTypeDef newDroneType)
        {
            controllerThing = parentThing;
            slotIndex = newSlotIndex;
            droneType = newDroneType;
            notifiedController = false;
            currentTarget = null;
            attackCooldownTicksLeft = 0;
            preAttackAimTicksLeft = 0;
            postAttackWaitTicksLeft = 0;
            completedAttackCycles = 0;
            verbTracker = new VerbTracker(this);
            EnsureVerbCasters();

            var controller = Controller;
            realPos = controller?.GetDockPosition(slotIndex, droneType) ?? Position.ToVector3Shifted();
            Position = realPos.ToIntVec3();
            realDir = ((Owner?.DrawPos ?? realPos) - realPos).Yto0();
            if (realDir == Vector3.zero)
            {
                realDir = Vector3.forward;
            }

            BeginLaunch();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref controllerThing, "controllerThing");
            Scribe_Values.Look(ref slotIndex, "slotIndex", -1);
            Scribe_Values.Look(ref state, "state", SD_DroneRuntimeState.Launching);
            Scribe_Defs.Look(ref droneType, "droneType");
            Scribe_Values.Look(ref realPos, "realPos");
            Scribe_Values.Look(ref realDir, "realDir", Vector3.forward);
            Scribe_References.Look(ref currentTarget, "currentTarget");
            Scribe_Values.Look(ref attackDestination, "attackDestination");
            Scribe_Values.Look(ref attackCooldownTicksLeft, "attackCooldownTicksLeft", 0);
            Scribe_Values.Look(ref preAttackAimTicksLeft, "preAttackAimTicksLeft", 0);
            Scribe_Values.Look(ref postAttackWaitTicksLeft, "postAttackWaitTicksLeft", 0);
            Scribe_Values.Look(ref completedAttackCycles, "completedAttackCycles", 0);
            Scribe_Values.Look(ref notifiedController, "notifiedController", false);
            Scribe_Values.Look(ref pathStart, "pathStart");
            Scribe_Values.Look(ref pathControlA, "pathControlA");
            Scribe_Values.Look(ref pathControlB, "pathControlB");
            Scribe_Values.Look(ref pathEnd, "pathEnd");
            Scribe_Values.Look(ref pathTicksElapsed, "pathTicksElapsed", 0);
            Scribe_Values.Look(ref pathTicksTotal, "pathTicksTotal", 0);
            Scribe_Deep.Look(ref verbTracker, "verbTracker", this);
        }

        protected override void Tick()
        {
            base.Tick();

            var controller = Controller;
            var owner = Owner;
            if (controller == null || owner == null || !owner.Spawned || owner.Dead)
            {
                Destroy();
                return;
            }

            if (attackCooldownTicksLeft > 0)
            {
                attackCooldownTicksLeft--;
            }

            VerbTracker.VerbsTick();
            EnsureVerbCasters();

            switch (state)
            {
                case SD_DroneRuntimeState.Launching:
                    TickLaunching();
                    break;
                case SD_DroneRuntimeState.MovingToAttackCell:
                    TickMovingToAttackCell();
                    break;
                case SD_DroneRuntimeState.Attacking:
                    TickAttacking();
                    break;
                case SD_DroneRuntimeState.PostAttackWaiting:
                    TickPostAttackWaiting();
                    break;
                case SD_DroneRuntimeState.Returning:
                    TickReturning();
                    break;
            }

            Position = realPos.ToIntVec3();
        }

        public void StartReturn()
        {
            if (Destroyed || state == SD_DroneRuntimeState.Returning)
            {
                return;
            }

            BeginReturn();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (!notifiedController)
            {
                Controller?.NotifyDroneLost(slotIndex, this);
                notifiedController = true;
            }

            base.Destroy(mode);
        }

        public string UniqueVerbOwnerID()
        {
            return "SD_DroneEntity_" + ThingID;
        }

        public bool VerbsStillUsableBy(Pawn p)
        {
            return p == Owner && !Destroyed;
        }

        private void TickLaunching()
        {
            if (TickBezierPath())
            {
                if (TryAcquireTargetAndDestination())
                {
                    BeginMoveToAttackCell(attackDestination);
                }
                else
                {
                    state = SD_DroneRuntimeState.Attacking;
                    preAttackAimTicksLeft = 0;
                }
            }
        }

        private void TickMovingToAttackCell()
        {
            if (!ValidateCurrentTarget())
            {
                BeginReturn();
                return;
            }

            var moveSpeed = ResolveMoveSpeed() * 1.1f;
            if (TickLinearMove(attackDestination, moveSpeed))
            {
                state = SD_DroneRuntimeState.Attacking;
                preAttackAimTicksLeft = Mathf.Max(droneType?.preAttackAimTicks ?? 0, 0);
            }
        }

        private void TickAttacking()
        {
            if (state == SD_DroneRuntimeState.Returning)
            {
                return;
            }

            if (!ValidateCurrentTarget())
            {
                if (TryAcquireTargetAndDestination())
                {
                    BeginMoveToAttackCell(attackDestination);
                }
                else
                {
                    BeginReturn();
                }

                return;
            }

            FaceTarget(currentTarget);

            if (preAttackAimTicksLeft > 0)
            {
                preAttackAimTicksLeft--;
                return;
            }

            if (attackCooldownTicksLeft > 0 || VerbTracker.AnyVerbBursting)
            {
                return;
            }

            if (!HasLineOfSightToTarget())
            {
                if (Controller != null && Controller.TryFindAttackDestination(this, currentTarget, out var destination))
                {
                    BeginMoveToAttackCell(destination);
                }
                else
                {
                    BeginReturn();
                }

                return;
            }

            if (!TryAttack(currentTarget))
            {
                if (Controller != null && Controller.TryFindAttackDestination(this, currentTarget, out var fallbackDestination))
                {
                    BeginMoveToAttackCell(fallbackDestination);
                }

                return;
            }

            completedAttackCycles++;
            postAttackWaitTicksLeft = Mathf.Max(droneType?.postAttackWaitTicks ?? 18, 0);
            state = SD_DroneRuntimeState.PostAttackWaiting;
        }

        private void TickPostAttackWaiting()
        {
            if (postAttackWaitTicksLeft > 0)
            {
                postAttackWaitTicksLeft--;
                if (ValidateCurrentTarget())
                {
                    FaceTarget(currentTarget);
                }

                return;
            }

            if (!ValidateCurrentTarget())
            {
                if (TryAcquireTargetAndDestination())
                {
                    BeginMoveToAttackCell(attackDestination);
                }
                else
                {
                    BeginReturn();
                }

                return;
            }

            if (droneType != null && droneType.maxAttackCycles > 0 && completedAttackCycles >= droneType.maxAttackCycles)
            {
                BeginReturn();
                return;
            }

            if (Controller != null && Controller.TryFindAttackDestination(this, currentTarget, out var destination))
            {
                BeginMoveToAttackCell(destination);
            }
            else
            {
                BeginReturn();
            }
        }

        private void TickReturning()
        {
            if (TickBezierPath())
            {
                NotifyReturned();
                Destroy();
            }
        }

        private void BeginLaunch()
        {
            var controller = Controller;
            var owner = Owner;
            var start = controller?.GetDockPosition(slotIndex, droneType) ?? realPos;
            var dockDir = owner == null ? Vector3.forward : (start - owner.DrawPos).Yto0().normalized;
            if (dockDir == Vector3.zero)
            {
                dockDir = new Vector3(0f, 0f, 1f).RotatedBy(slotIndex * 90f);
            }

            var tangent = new Vector3(-dockDir.z, 0f, dockDir.x);
            var end = controller?.GetOrbitPosition(slotIndex, droneType) ?? start;
            var forwardDistance = droneType?.launchForwardDistance ?? 0.9f;
            var sideOffset = droneType?.launchSideOffset ?? 0.45f;

            BuildBezierPath(
                start,
                end,
                start + dockDir * forwardDistance + tangent * sideOffset,
                end - dockDir * (forwardDistance * 0.4f),
                Mathf.Max(droneType?.deployTicks ?? controller?.Props.deployTicks ?? 24, 6));

            state = SD_DroneRuntimeState.Launching;
            currentTarget = null;
            completedAttackCycles = 0;
        }

        private void BeginMoveToAttackCell(Vector3 destination)
        {
            attackDestination = destination;
            state = SD_DroneRuntimeState.MovingToAttackCell;
        }

        private void BeginReturn()
        {
            var controller = Controller;
            if (controller == null)
            {
                Destroy();
                return;
            }

            var end = controller.GetDockPosition(slotIndex, droneType);
            var toDock = (end - realPos).Yto0();
            var tangent = toDock == Vector3.zero ? realDir.Yto0().normalized : toDock.normalized;
            if (tangent == Vector3.zero)
            {
                tangent = Vector3.forward;
            }

            var side = new Vector3(-tangent.z, 0f, tangent.x);
            var moveSpeed = ResolveMoveSpeed() * Mathf.Max(droneType?.returnSpeedMultiplier ?? 1.25f, 1f);
            var distance = toDock.magnitude;
            var pathTicks = Mathf.Max(Mathf.RoundToInt(distance / Mathf.Max(moveSpeed, 0.02f)), 10);

            BuildBezierPath(
                realPos,
                end,
                realPos + tangent * 0.8f + side * 0.35f,
                end - tangent * 0.5f,
                pathTicks);

            currentTarget = null;
            state = SD_DroneRuntimeState.Returning;
        }

        private bool TryAcquireTargetAndDestination()
        {
            var controller = Controller;
            if (controller == null)
            {
                return false;
            }

            var target = controller.GetAttackTargetForDrone(this);
            if (!IsTargetUsable(target))
            {
                currentTarget = null;
                return false;
            }

            if (!controller.TryFindAttackDestination(this, target, out var destination))
            {
                currentTarget = null;
                return false;
            }

            currentTarget = target;
            attackDestination = destination;
            return true;
        }

        private bool ValidateCurrentTarget()
        {
            if (IsTargetUsable(currentTarget))
            {
                return true;
            }

            currentTarget = null;
            return false;
        }

        private bool IsTargetUsable(Thing target)
        {
            var owner = Owner;
            if (owner?.Map == null || target == null || target.Destroyed || !target.Spawned)
            {
                return false;
            }

            if (target == owner || !owner.HostileTo(target))
            {
                return false;
            }

            var maxRange = ResolveMaxRange();
            if (maxRange <= 0f)
            {
                return false;
            }

            return (DrawPos - target.DrawPos).Yto0().sqrMagnitude <= maxRange * maxRange;
        }

        private float ResolveMoveSpeed()
        {
            return droneType?.moveSpeed > 0f ? droneType.moveSpeed : Controller?.Props.moveSpeed ?? 0.18f;
        }

        private float ResolveMaxRange()
        {
            if (droneType?.verbs.NullOrEmpty() ?? true)
            {
                return 0f;
            }

            return droneType.verbs.Max(v => v.range);
        }

        private bool TickLinearMove(Vector3 targetPos, float moveSpeed)
        {
            var delta = (targetPos - realPos).Yto0();
            if (delta == Vector3.zero)
            {
                return true;
            }

            var step = Mathf.Max(moveSpeed, 0.02f);
            if (delta.magnitude <= step)
            {
                realPos = targetPos;
                realDir = delta.normalized;
                return true;
            }

            realPos += delta.normalized * step;
            realDir = delta.normalized;
            return false;
        }

        private void BuildBezierPath(Vector3 start, Vector3 end, Vector3 controlA, Vector3 controlB, int ticks)
        {
            pathStart = start;
            pathControlA = controlA;
            pathControlB = controlB;
            pathEnd = end;
            pathTicksElapsed = 0;
            pathTicksTotal = Mathf.Max(ticks, 1);
            realPos = start;
        }

        private bool TickBezierPath()
        {
            if (pathTicksTotal <= 0)
            {
                return true;
            }

            pathTicksElapsed++;
            var t = Mathf.Clamp01(pathTicksElapsed / (float)pathTicksTotal);
            var nextPos = BezierPoint(t, pathStart, pathControlA, pathControlB, pathEnd);
            var delta = (nextPos - realPos).Yto0();
            if (delta != Vector3.zero)
            {
                realDir = delta.normalized;
            }

            realPos = nextPos;
            return pathTicksElapsed >= pathTicksTotal;
        }

        private static Vector3 BezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * oneMinusT * p0
                + 3f * oneMinusT * oneMinusT * t * p1
                + 3f * oneMinusT * t * t * p2
                + t * t * t * p3;
        }

        private void FaceTarget(Thing target)
        {
            if (target == null)
            {
                return;
            }

            var direction = (target.DrawPos - DrawPos).Yto0();
            if (direction != Vector3.zero)
            {
                realDir = direction.normalized;
            }
        }

        private bool HasLineOfSightToTarget()
        {
            var owner = Owner;
            if (owner?.Map == null || currentTarget == null)
            {
                return false;
            }

            var from = DrawPos.ToIntVec3();
            var to = currentTarget.OccupiedRect().ClosestCellTo(from);
            return GenSight.LineOfSight(from, to, owner.Map, skipFirstCell: true);
        }

        private void NotifyReturned()
        {
            if (notifiedController)
            {
                return;
            }

            Controller?.NotifyDroneReturned(slotIndex, this);
            notifiedController = true;
        }

        private bool TryAttack(Thing target)
        {
            var targetInfo = new LocalTargetInfo(target);
            var verb = SelectAttackVerb(targetInfo);
            if (verb == null)
            {
                return false;
            }

            if (!verb.TryStartCastOn(targetInfo, canHitNonTargetPawns: true, preventFriendlyFire: false))
            {
                return false;
            }

            attackCooldownTicksLeft = ResolveCooldownTicks(verb);
            FaceTarget(target);
            return true;
        }

        private Verb SelectAttackVerb(LocalTargetInfo target)
        {
            if (droneType?.verbs.NullOrEmpty() ?? true)
            {
                return null;
            }

            EnsureVerbCasters();

            return AllVerbs
                .OrderByDescending(v => v.verbProps.isPrimary)
                .FirstOrDefault(v => v.state == VerbState.Idle && v.Available() && v.IsUsableOn(target.Thing) && v.CanHitTarget(target));
        }

        private int ResolveCooldownTicks(Verb verb)
        {
            if (verb == null)
            {
                return 0;
            }

            return Mathf.Max(verb.verbProps.AdjustedCooldown(verb, Owner), 0.05f).SecondsToTicks();
        }

        private void EnsureVerbCasters()
        {
            var verbs = AllVerbs;
            for (var i = 0; i < verbs.Count; i++)
            {
                verbs[i].caster = this;
            }
        }
    }
}
