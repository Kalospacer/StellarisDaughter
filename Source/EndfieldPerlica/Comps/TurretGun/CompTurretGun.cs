using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;

namespace EndfieldPerlica
{
    [StaticConstructorOnStartup]
    public class CompTurretGun : ThingComp, IAttackTargetSearcher
    {
        public Thing gun;
        public int burstCooldownTicksLeft;
        public int burstWarmupTicksLeft;
        public LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;
        public LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;
        public bool fireAtWill = true;
        public LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;
        public int lastAttackTargetTick;
        public float curRotation;
        public float floatOffset_xAxis;
        public float floatOffset_yAxis;
        private float randTime = Rand.Range(0f, 300f);

        public CompProperties_TurretGun Props => (CompProperties_TurretGun)props;
        public Pawn PawnOwner => (parent is Apparel apparel) ? apparel.Wearer : (parent is Pawn pawn ? pawn : null);
        public Verb AttackVerb => gun?.TryGetComp<CompEquippable>()?.PrimaryVerb;
        public Thing Thing => PawnOwner;
        public Verb CurrentEffectiveVerb => AttackVerb;
        public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;
        public int LastAttackTargetTick => lastAttackTargetTick;

        public bool CanShoot
        {
            get
            {
                if (PawnOwner == null || !PawnOwner.Spawned || PawnOwner.Downed || PawnOwner.Dead || !PawnOwner.Awake()) return false;
                if (!Props.attackUndrafted && PawnOwner.IsPlayerControlled && !PawnOwner.Drafted) return false;
                if (PawnOwner.stances.stunner.Stunned) return false;
                if (!fireAtWill && !forcedTarget.IsValid) return false;
                return true;
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            MakeGun();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            MakeGun();
            UpdateGunVerbs(); // 强制更新发射者
        }

        private void MakeGun()
        {
            if (gun == null)
            {
                gun = ThingMaker.MakeThing(Props.turretDef);
            }
            UpdateGunVerbs();
        }

        private void UpdateGunVerbs()
        {
            if (PawnOwner == null || gun == null) return;
            var eq = gun.TryGetComp<CompEquippable>();
            if (eq != null)
            {
                foreach (var v in eq.AllVerbs)
                {
                    v.caster = PawnOwner;
                    v.castCompleteCallback = delegate {
                        var verb = AttackVerb;
                        if (verb != null && verb.verbProps != null)
                             burstCooldownTicksLeft = verb.verbProps.defaultCooldownTime.SecondsToTicks();
                    };
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (burstCooldownTicksLeft > 0) burstCooldownTicksLeft--;
            if (!CanShoot)
            {
                ResetWarmup();
                return;
            }

            // 浮动动画计算
            if (Props.float_xAxis != null)
                floatOffset_xAxis = Mathf.Sin((Find.TickManager.TicksGame + randTime) * Props.float_xAxis.floatSpeed) * Props.float_xAxis.floatAmplitude;
            if (Props.float_yAxis != null)
                floatOffset_yAxis = Mathf.Sin((Find.TickManager.TicksGame + randTime) * Props.float_yAxis.floatSpeed) * Props.float_yAxis.floatAmplitude;

            // 旋转计算
            if (currentTarget.IsValid)
                curRotation = (currentTarget.Cell.ToVector3Shifted() - PawnOwner.DrawPos).AngleFlat() + Props.angleOffset;

            if (AttackVerb == null) return;
            AttackVerb.VerbTick();
            
            if (AttackVerb.state == VerbState.Bursting) return;

            // 预热与开火逻辑
            if (burstWarmupTicksLeft > 0 && currentTarget.IsValid)
            {
                burstWarmupTicksLeft--;
                if (burstWarmupTicksLeft == 0)
                {
                    if (AttackVerb.TryStartCastOn(currentTarget, false, true, false, true))
                    {
                        lastAttackedTarget = currentTarget;
                        lastAttackTargetTick = Find.TickManager.TicksGame;
                    }
                }
            }
            else if (burstCooldownTicksLeft <= 0 && PawnOwner.IsHashIntervalTick(10))
            {
                ScanForTarget();
            }
        }

        private void ScanForTarget()
        {
            var verb = AttackVerb;
            if (verb == null || verb.verbProps == null || PawnOwner == null) return;
            if (verb.caster == null) UpdateGunVerbs();
            if (verb.caster == null) return;

            // 1. 检查强制目标（手动锁定）是否失效
            if (forcedTarget.IsValid)
            {
                if (forcedTarget.HasThing && (!forcedTarget.Thing.Spawned || forcedTarget.Thing.Destroyed))
                    forcedTarget = LocalTargetInfo.Invalid;
                else if (forcedTarget.Thing is Pawn p && p.Downed)
                    forcedTarget = LocalTargetInfo.Invalid;
            }

            // 2. 目标选择逻辑：锁定 > 同步主人战斗目标
            LocalTargetInfo target = forcedTarget.IsValid ? forcedTarget : (fireAtWill ? GetOwnerTarget() : LocalTargetInfo.Invalid);

            if (target.IsValid && verb.CanHitTarget(target))
            {
                currentTarget = target;
                burstWarmupTicksLeft = verb.verbProps.warmupTime.SecondsToTicks();
            }
            else
            {
                ResetCurrentTarget();
            }
        }

        private LocalTargetInfo GetOwnerTarget()
        {
            if (PawnOwner == null) return LocalTargetInfo.Invalid;

            // 优先获取主人的远程瞄准目标 (Warmup/Cooldown 状态)
            var aimTarget = PawnOwner.TargetCurrentlyAimingAt;
            if (aimTarget.IsValid) return aimTarget;

            // 如果主人在近战或由于 Job 锁定了目标
            var curJob = PawnOwner.CurJob;
            if (curJob != null && (curJob.def == JobDefOf.AttackMelee || curJob.def == JobDefOf.AttackStatic))
            {
                return curJob.targetA;
            }

            // 兜底判定：如果是猎食者或 AI 控制，尝试同步其 enemyTarget
            if (!PawnOwner.IsColonist && PawnOwner.mindState.enemyTarget != null)
            {
                return PawnOwner.mindState.enemyTarget;
            }

            return LocalTargetInfo.Invalid;
        }

        public void ResetWarmup() => burstWarmupTicksLeft = 0;
        public void ResetCurrentTarget() => currentTarget = LocalTargetInfo.Invalid;

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (PawnOwner.Faction != Faction.OfPlayer) yield break;

            var verb = AttackVerb;
            if (verb != null)
            {
                // 手动指定目标
                yield return new Command_Action
                {
                    defaultLabel = "EF_Perlica_CommandSetForceAttackTarget".Translate(),
                    defaultDesc = "EF_Perlica_CommandSetForceAttackTargetDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                    action = delegate {
                        Find.Targeter.BeginTargeting(verb.verbProps.targetParams, delegate (LocalTargetInfo target) {
                            forcedTarget = target;
                        }, caster: PawnOwner);
                    }
                };
            }

            // 停止强制攻击
            if (forcedTarget.IsValid)
            {
                yield return new Command_Action
                {
                    defaultLabel = "EF_Perlica_CommandStopForceAttack".Translate(),
                    defaultDesc = "EF_Perlica_CommandStopForceAttackDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
                    action = delegate {
                        forcedTarget = LocalTargetInfo.Invalid;
                        ResetCurrentTarget();
                        ResetWarmup();
                    }
                };
            }

            // 同步火力开关
            yield return new Command_Toggle
            {
                defaultLabel = "EF_Perlica_CommandFireAtWill".Translate(),
                defaultDesc = "EF_Perlica_CommandFireAtWillDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Gizmos/ToggleTurret"),
                isActive = () => fireAtWill,
                toggleAction = delegate { fireAtWill = !fireAtWill; }
            };
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_Values.Look(ref fireAtWill, "fireAtWill", true);
            Scribe_TargetInfo.Look(ref forcedTarget, "forcedTarget");
            Scribe_Deep.Look(ref gun, "gun");
        }
    }
}
