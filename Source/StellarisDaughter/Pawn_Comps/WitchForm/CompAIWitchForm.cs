using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StellarisDaughter
{
    /// <summary>
    /// 魔女化系统核心组件。
    /// </summary>
    public class CompAIWitchForm : ThingComp
    {
        public float witchFactor = 0f;
        public WitchFormState state = WitchFormState.Normal;

        private int nextTransformAllowedTick;
        private int nextCancelTransformAllowedTick;

        public CompProperties_AIWitchForm Props => (CompProperties_AIWitchForm)props;
        public Pawn Pawn => parent as Pawn;
        public bool IsWitchForm => state == WitchFormState.WitchForm || state == WitchFormState.Berserk;
        public bool IsBerserk => state == WitchFormState.Berserk;
        public int TransformCooldownRemainingTicks => GetRemainingTicks(nextTransformAllowedTick);
        public int CancelTransformCooldownRemainingTicks => GetRemainingTicks(nextCancelTransformAllowedTick);
        public int CurrentToggleCooldownRemainingTicks => state switch
        {
            WitchFormState.Normal => TransformCooldownRemainingTicks,
            WitchFormState.WitchForm => CancelTransformCooldownRemainingTicks,
            _ => 0
        };

        public float MaxWitchFactor
        {
            get
            {
                var upbringing = Pawn?.GetComp<CompAIUpbringing>();
                var affectionBonus = upbringing?.affection ?? 0f;
                return Props.baseMaxFactor + affectionBonus * Props.affectionToMaxFactor;
            }
        }

        public float TransformThresholdRatio
        {
            get
            {
                var affection = Mathf.Max(Pawn?.GetComp<CompAIUpbringing>()?.affection ?? 0f, 0f);
                var maxAffection = Mathf.Max(Props.affectionForMaxThreshold, 0.01f);
                var normalizedAffection = Mathf.Clamp01(affection / maxAffection);
                return Props.transformThresholdBaseRatio
                    + normalizedAffection * (Props.transformThresholdMaxRatio - Props.transformThresholdBaseRatio);
            }
        }

        public float TransformThresholdFactor => MaxWitchFactor * TransformThresholdRatio;

        public override void CompTickRare()
        {
            base.CompTickRare();

            if (Current.ProgramState != ProgramState.Playing || Pawn == null)
            {
                return;
            }

            if (IsBerserk && !Pawn.InMentalState)
            {
                Log.Warning($"[StellarisDaughter] {Pawn.NameShortColored} Berserk state ended externally, resetting witch state");
                state = WitchFormState.Normal;
                witchFactor = 0f;
                SwitchAppearance(false);
            }

            CalculateGrowth();
            CheckBerserk();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureFormHediffState();
            EnsureApparelLockState();
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            EnsureFormHediffState();
        }

        private void CalculateGrowth()
        {
            var growthRate = GetGrowthRate();
            if (Mathf.Approximately(growthRate, 0f))
            {
                return;
            }

            var newFactor = witchFactor + growthRate;
            witchFactor = Mathf.Clamp(newFactor, 0f, MaxWitchFactor);
        }

        private float GetGrowthRate()
        {
            var baseRate = Props.baseGrowthRate;
            var trust = Pawn?.GetComp<CompAIUpbringing>()?.trust ?? 0f;

            if (trust < 0f)
            {
                baseRate -= trust * Props.negativeTrustGrowthFactor;
            }

            baseRate = Mathf.Max(baseRate, 0f);

            if (IsWitchForm)
            {
                return baseRate * Props.witchFormGrowthMult;
            }

            if (trust > 0f && witchFactor > 0f)
            {
                return -Mathf.Min(
                    trust * Props.positiveTrustDecayFactor,
                    Props.baseGrowthRate * Props.positiveTrustDecayMaxBaseFraction);
            }

            if (witchFactor < TransformThresholdFactor)
            {
                return 0f;
            }

            return baseRate * Props.halfMaxGrowthMult;
        }

        private void CheckBerserk()
        {
            if (!IsBerserk && witchFactor >= MaxWitchFactor)
            {
                EnterBerserk();
            }
        }

        private void EnterBerserk()
        {
            var wasInWitchForm = state == WitchFormState.WitchForm;
            state = WitchFormState.Berserk;

            if (!wasInWitchForm)
            {
                SwitchAppearance(true);
            }

            if (Pawn.mindState != null && !Pawn.InMentalState)
            {
                var success = Pawn.mindState.mentalStateHandler.TryStartMentalState(SD_DefOf.SD_WitchBerserk, null, forceWake: true);
                if (!success)
                {
                    Log.Warning($"[StellarisDaughter] Failed to start WitchBerserk mental state for {Pawn.NameShortColored}");
                }
            }

            Messages.Message("SD_Witch_Berserk".Translate(Pawn.NameShortColored), Pawn, MessageTypeDefOf.NegativeEvent);
        }

        public void ToggleWitchForm()
        {
            if (!CanToggleWitchFormNow(out var reason))
            {
                if (!string.IsNullOrEmpty(reason))
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput);
                }
                return;
            }

            if (state == WitchFormState.Normal)
            {
                StartTransformCooldown();
                state = WitchFormState.WitchForm;
                SwitchAppearance(true);
            }
            else
            {
                StartCancelTransformCooldown();
                state = WitchFormState.Normal;
                SwitchAppearance(false);
            }
        }

        public bool CanToggleWitchFormNow(out string reason)
        {
            if (IsBerserk)
            {
                reason = "SD_Witch_CannotToggle_Berserk".Translate();
                return false;
            }

            if (state == WitchFormState.Normal)
            {
                var remainingTicks = TransformCooldownRemainingTicks;
                if (remainingTicks > 0)
                {
                    reason = "SD_Witch_TransformCooldownRemaining".Translate(FormatCooldownDuration(remainingTicks));
                    return false;
                }
            }
            else if (state == WitchFormState.WitchForm)
            {
                var remainingTicks = CancelTransformCooldownRemainingTicks;
                if (remainingTicks > 0)
                {
                    reason = "SD_Witch_CancelCooldownRemaining".Translate(FormatCooldownDuration(remainingTicks));
                    return false;
                }
            }

            reason = null;
            return true;
        }

        public string GetCurrentToggleCooldownDescription()
        {
            if (state == WitchFormState.Normal)
            {
                var remainingTicks = TransformCooldownRemainingTicks;
                if (remainingTicks > 0)
                {
                    return "SD_Witch_TransformCooldownRemaining".Translate(FormatCooldownDuration(remainingTicks));
                }

                if (Props.transformCooldownTicks > 0)
                {
                    return "SD_Witch_TransformCooldown".Translate(FormatCooldownDuration(Props.transformCooldownTicks));
                }
            }
            else if (state == WitchFormState.WitchForm)
            {
                var remainingTicks = CancelTransformCooldownRemainingTicks;
                if (remainingTicks > 0)
                {
                    return "SD_Witch_CancelCooldownRemaining".Translate(FormatCooldownDuration(remainingTicks));
                }

                if (Props.cancelTransformCooldownTicks > 0)
                {
                    return "SD_Witch_CancelCooldown".Translate(FormatCooldownDuration(Props.cancelTransformCooldownTicks));
                }
            }

            return null;
        }

        private void StartTransformCooldown()
        {
            if (Props.transformCooldownTicks > 0 && Find.TickManager != null)
            {
                nextTransformAllowedTick = Find.TickManager.TicksGame + Props.transformCooldownTicks;
            }
        }

        private void StartCancelTransformCooldown()
        {
            if (Props.cancelTransformCooldownTicks > 0 && Find.TickManager != null)
            {
                nextCancelTransformAllowedTick = Find.TickManager.TicksGame + Props.cancelTransformCooldownTicks;
            }
        }

        private int GetRemainingTicks(int targetTick)
        {
            if (Find.TickManager == null)
            {
                return 0;
            }

            return Mathf.Max(0, targetTick - Find.TickManager.TicksGame);
        }

        public static string FormatCooldownDuration(int ticks)
        {
            return ticks.ToStringTicksToPeriod(allowSeconds: true, shortForm: false, canUseDecimals: true, allowYears: false, canUseDecimalsShortForm: false);
        }

        private void SwitchAppearance(bool toWitchForm)
        {
            if (Pawn == null)
            {
                return;
            }

            SwitchWitchFormHediff(toWitchForm);

            if (Pawn.apparel != null && Pawn.story != null)
            {
                DropNonExclusiveApparel();
                SwitchExclusiveApparel(toWitchForm);
                SwitchHair(toWitchForm);
                EnsureApparelLockState();
            }

            Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        private HediffDef NormalFormHediffDef => Props.normalFormHediffDef;

        private HediffDef WitchFormHediffDef => Props.witchFormHediffDef ?? SD_DefOf.SD_Hediff_WitchForm;

        private void EnsureFormHediffState()
        {
            SyncFormHediff(IsWitchForm);
        }

        private void SwitchWitchFormHediff(bool toWitchForm)
        {
            SyncFormHediff(toWitchForm);
        }

        private void SyncFormHediff(bool toWitchForm)
        {
            if (Pawn?.health?.hediffSet == null)
            {
                return;
            }

            var desiredHediffDef = toWitchForm ? WitchFormHediffDef : NormalFormHediffDef;
            RemoveFormHediffIfPresent(NormalFormHediffDef, desiredHediffDef);
            RemoveFormHediffIfPresent(WitchFormHediffDef, desiredHediffDef);

            if (desiredHediffDef != null && Pawn.health.hediffSet.GetFirstHediffOfDef(desiredHediffDef) == null)
            {
                var hediff = HediffMaker.MakeHediff(desiredHediffDef, Pawn);
                Pawn.health.AddHediff(hediff);
            }
        }

        private void RemoveFormHediffIfPresent(HediffDef hediffDef, HediffDef keepDef)
        {
            if (hediffDef == null || hediffDef == keepDef)
            {
                return;
            }

            var existingHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existingHediff != null)
            {
                Pawn.health.RemoveHediff(existingHediff);
            }
        }

        private void DropNonExclusiveApparel()
        {
            var exclusiveDefs = new HashSet<ThingDef>
            {
                Props.normalApparelDef,
                Props.witchApparelDef
            };

            var keepDefs = new HashSet<ThingDef>();
            if (Props.keepApparelDefs != null)
            {
                foreach (var def in Props.keepApparelDefs)
                {
                    keepDefs.Add(def);
                }
            }

            var apparelsToDrop = Pawn.apparel.WornApparel
                .Where(a => !exclusiveDefs.Contains(a.def) && !keepDefs.Contains(a.def))
                .ToList();

            foreach (var apparel in apparelsToDrop)
            {
                Pawn.apparel.TryDrop(apparel, out _, Pawn.Position);
            }
        }

        private void SwitchExclusiveApparel(bool toWitchForm)
        {
            var exclusiveDefs = new HashSet<ThingDef>
            {
                Props.normalApparelDef,
                Props.witchApparelDef
            };

            var currentExclusive = Pawn.apparel.WornApparel.FirstOrDefault(a => exclusiveDefs.Contains(a.def));
            if (currentExclusive != null)
            {
                Pawn.apparel.Remove(currentExclusive);
                currentExclusive.Destroy(DestroyMode.Vanish);
            }

            var newApparelDef = toWitchForm ? Props.witchApparelDef : Props.normalApparelDef;
            if (newApparelDef != null)
            {
                var newApparel = ThingMaker.MakeThing(newApparelDef) as Apparel;
                if (newApparel != null)
                {
                    Pawn.apparel.Wear(newApparel, dropReplacedApparel: true, locked: true);
                }
            }
        }

        private void SwitchHair(bool toWitchForm)
        {
            var newHair = toWitchForm ? Props.witchHairDef : Props.normalHairDef;
            if (newHair != null)
            {
                Pawn.story.hairDef = newHair;
            }
        }

        private void EnsureApparelLockState()
        {
            if (Pawn?.apparel == null)
            {
                return;
            }

            foreach (var apparel in Pawn.apparel.WornApparel)
            {
                if (apparel.def == Props.normalApparelDef || apparel.def == Props.witchApparelDef)
                {
                    Pawn.apparel.Lock(apparel);
                    continue;
                }

                if (Props.keepApparelDefs != null && Props.keepApparelDefs.Contains(apparel.def))
                {
                    Pawn.apparel.Unlock(apparel);
                }
            }
        }

        public void ApplySuppression()
        {
            if (!IsBerserk)
            {
                Messages.Message("SD_Witch_NotBerserk".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (Pawn.InMentalState && Pawn.MentalStateDef == SD_DefOf.SD_WitchBerserk)
            {
                Pawn.MentalState.RecoverFromState();
            }

            witchFactor = 0f;
            state = WitchFormState.Normal;
            SwitchAppearance(false);
            Pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(SD_DefOf.SD_WitchBerserkRecovered);
            Messages.Message("SD_Witch_Suppressed".Translate(Pawn.NameShortColored), MessageTypeDefOf.PositiveEvent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref witchFactor, "witchFactor", 0f);
            Scribe_Values.Look(ref state, "state", WitchFormState.Normal);
            Scribe_Values.Look(ref nextTransformAllowedTick, "nextTransformAllowedTick", 0);
            Scribe_Values.Look(ref nextCancelTransformAllowedTick, "nextCancelTransformAllowedTick", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureFormHediffState();
                EnsureApparelLockState();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Gizmo_AIWitchForm(this);
            yield return new Command_ToggleWitchForm(this);

            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "SD_Witch_Debug_AddFactor_Label".Translate(),
                    defaultDesc = "SD_Witch_Debug_AddFactor_Desc".Translate(),
                    action = () => witchFactor = Mathf.Min(witchFactor + 100f, MaxWitchFactor)
                };

                yield return new Command_Action
                {
                    defaultLabel = "SD_Witch_Debug_ResetFactor_Label".Translate(),
                    defaultDesc = "SD_Witch_Debug_ResetFactor_Desc".Translate(),
                    action = () => witchFactor = 0f
                };

                yield return new Command_Action
                {
                    defaultLabel = "SD_Witch_Debug_ForceBerserk_Label".Translate(),
                    defaultDesc = "SD_Witch_Debug_ForceBerserk_Desc".Translate(),
                    action = () =>
                    {
                        witchFactor = MaxWitchFactor;
                        EnterBerserk();
                    }
                };
            }
        }

        public string GetDebugInfo()
        {
            var trust = Pawn.GetComp<CompAIUpbringing>()?.trust ?? 0f;
            return "SD_Witch_Debug_Info".Translate(
                witchFactor.ToString("F1"),
                MaxWitchFactor.ToString("F1"),
                state.ToString(),
                ((Pawn.GetComp<CompAIUpbringing>()?.affection ?? 0f) * 0.1f).ToString("F1"),
                trust.ToString("F1"));
        }

    }
}
