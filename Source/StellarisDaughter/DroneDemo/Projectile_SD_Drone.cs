using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public enum SD_DroneRuntimeState
    {
        Deploying,
        Orbiting,
        Returning
    }

    // 鉁?娌愰洩鍐欑殑鍝
    public class SD_DroneEntity : ThingWithComps, IVerbOwner
    {
        private Thing controllerThing;
        private int slotIndex = -1;
        private SD_DroneRuntimeState state = SD_DroneRuntimeState.Deploying;
        private SD_DroneTypeDef droneType;
        private VerbTracker verbTracker;
        private Vector3 realPos;
        private Vector3 realDir = Vector3.forward;
        private Thing currentTarget;
        private int attackCooldownTicksLeft;
        private bool notifiedController;

        private CompSD_DroneController Controller => controllerThing?.TryGetComp<CompSD_DroneController>();

        public Pawn Owner => Controller?.Wearer;

        public Thing ControllerThing => controllerThing;

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

        public void Initialize(Thing parentThing, int slotIndex, SD_DroneTypeDef droneType)
        {
            controllerThing = parentThing;
            this.slotIndex = slotIndex;
            this.droneType = droneType;
            realPos = Owner?.DrawPos ?? Position.ToVector3Shifted();
            Position = realPos.ToIntVec3();
            state = SD_DroneRuntimeState.Deploying;
            attackCooldownTicksLeft = 0;
            notifiedController = false;
            verbTracker = new VerbTracker(this);
            EnsureVerbCasters();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref controllerThing, "controllerThing");
            Scribe_Values.Look(ref slotIndex, "slotIndex", -1);
            Scribe_Values.Look(ref state, "state", SD_DroneRuntimeState.Deploying);
            Scribe_Defs.Look(ref droneType, "droneType");
            Scribe_Values.Look(ref realPos, "realPos");
            Scribe_Values.Look(ref realDir, "realDir", Vector3.forward);
            Scribe_References.Look(ref currentTarget, "currentTarget");
            Scribe_Values.Look(ref attackCooldownTicksLeft, "attackCooldownTicksLeft", 0);
            Scribe_Values.Look(ref notifiedController, "notifiedController", false);
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
                case SD_DroneRuntimeState.Deploying:
                    TickMove(controller.GetOrbitPosition(slotIndex, droneType), ResolveMoveSpeed(controller));
                    if ((realPos - controller.GetOrbitPosition(slotIndex, droneType)).sqrMagnitude < 0.04f)
                    {
                        state = SD_DroneRuntimeState.Orbiting;
                    }
                    break;

                case SD_DroneRuntimeState.Orbiting:
                    TickMove(controller.GetOrbitPosition(slotIndex, droneType), ResolveMoveSpeed(controller));
                    TickAutoAttack(controller);
                    break;

                case SD_DroneRuntimeState.Returning:
                    TickMove(controller.GetDockPosition(slotIndex, droneType), ResolveMoveSpeed(controller) * 1.35f);
                    if ((realPos - controller.GetDockPosition(slotIndex, droneType)).sqrMagnitude < 0.03f)
                    {
                        NotifyReturned();
                        Destroy();
                    }
                    break;
            }

            Position = realPos.ToIntVec3();
        }

        public void StartReturn()
        {
            state = SD_DroneRuntimeState.Returning;
            currentTarget = null;
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

        public void SetFacing(Vector3 dir)
        {
            if (dir != Vector3.zero)
            {
                realDir = dir;
            }
        }

        private void TickAutoAttack(CompSD_DroneController controller)
        {
            if (currentTarget == null || currentTarget.Destroyed || !currentTarget.Spawned)
            {
                currentTarget = null;
            }

            if (currentTarget == null && Find.TickManager.TicksGame % 15 == 0)
            {
                currentTarget = controller.GetAttackTargetForDrone(this);
            }

            if (currentTarget == null || attackCooldownTicksLeft > 0)
            {
                return;
            }

            TryAttack(currentTarget);
        }

        private float ResolveMoveSpeed(CompSD_DroneController controller)
        {
            return droneType?.moveSpeed > 0f ? droneType.moveSpeed : controller.Props.moveSpeed;
        }

        private void TickMove(Vector3 targetPos, float moveSpeed)
        {
            var delta = (targetPos - realPos).Yto0();
            if (delta != Vector3.zero)
            {
                var step = Mathf.Max(moveSpeed, 0.02f);
                if (delta.magnitude <= step)
                {
                    realPos = targetPos;
                }
                else
                {
                    realPos += delta.normalized * step;
                }

                realDir = delta.normalized;
            }
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

            var targetDir = (target.DrawPos - DrawPos).Yto0();
            if (targetDir != Vector3.zero)
            {
                SetFacing(targetDir.normalized);
            }

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

