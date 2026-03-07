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

        public CompProperties_AIWitchForm Props => (CompProperties_AIWitchForm)props;
        public Pawn Pawn => parent as Pawn;
        public bool IsWitchForm => state == WitchFormState.WitchForm || state == WitchFormState.Berserk;
        public bool IsBerserk => state == WitchFormState.Berserk;

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
            if (IsBerserk)
            {
                Messages.Message("SD_Witch_CannotToggle_Berserk".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (state == WitchFormState.Normal)
            {
                state = WitchFormState.WitchForm;
                SwitchAppearance(true);
            }
            else
            {
                state = WitchFormState.Normal;
                SwitchAppearance(false);
            }
        }

        private void SwitchAppearance(bool toWitchForm)
        {
            if (Pawn?.apparel == null || Pawn.story == null)
            {
                return;
            }

            DropNonExclusiveApparel();
            SwitchExclusiveApparel(toWitchForm);
            SwitchHair(toWitchForm);
            SwitchWitchFormHediff(toWitchForm);
            Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        private void SwitchWitchFormHediff(bool toWitchForm)
        {
            if (Pawn?.health?.hediffSet == null)
            {
                return;
            }

            var existingHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(SD_DefOf.SD_Hediff_WitchForm);
            if (toWitchForm)
            {
                if (existingHediff == null)
                {
                    var hediff = HediffMaker.MakeHediff(SD_DefOf.SD_Hediff_WitchForm, Pawn);
                    Pawn.health.AddHediff(hediff);
                }
            }
            else if (existingHediff != null)
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
            Messages.Message("SD_Witch_Suppressed".Translate(Pawn.NameShortColored), MessageTypeDefOf.PositiveEvent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref witchFactor, "witchFactor", 0f);
            Scribe_Values.Look(ref state, "state", WitchFormState.Normal);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Gizmo_AIWitchForm(this);
            yield return new Command_ToggleWitchForm(this);

            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: +100 因子",
                    defaultDesc = "增加100魔女因子用于测试",
                    action = () => witchFactor = Mathf.Min(witchFactor + 100f, MaxWitchFactor)
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: 重置因子",
                    defaultDesc = "重置魔女因子为0",
                    action = () => witchFactor = 0f
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: 强制发狂",
                    defaultDesc = "强制进入发狂状态",
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
            return $"=== 魔女化系统 ===\n" +
                   $"因子值: {witchFactor:F1} / {MaxWitchFactor:F1}\n" +
                   $"状态: {state}\n" +
                   $"好感影响: {(Pawn.GetComp<CompAIUpbringing>()?.affection ?? 0f) * 0.1f:F1}\n" +
                   $"信任值: {trust:F1}";
        }
    }
}
