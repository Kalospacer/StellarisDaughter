using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class HediffCompProperties_Regeneration : HediffCompProperties
    {
        public bool useRepairResource = false;
        public NeedDef repairResourceNeed;
        public List<HediffDef> additionalRepairableHediffs;
        public float repairCostPerHP = 0.03f;
        public int repairCooldownAfterDamage = 600;

        public HediffCompProperties_Regeneration()
        {
            compClass = typeof(HediffComp_Regeneration);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (useRepairResource && repairResourceNeed == null)
            {
                yield return "useRepairResource is enabled but repairResourceNeed is null";
            }

            if (repairCostPerHP < 0f)
            {
                yield return "repairCostPerHP must be non-negative";
            }
        }
    }

    public class HediffComp_Regeneration : HediffComp
    {
        private enum RegenState
        {
            Disabled,
            Cooldown,
            Repairing,
            Idle
        }

        private const int CheckInterval = 60;
        private int lastDamageTick = -9999;
        private bool repairSystemEnabled = true;
        private RegenState cachedState = RegenState.Idle;
        private int cachedCooldownTicks;

        public HediffCompProperties_Regeneration Props => (HediffCompProperties_Regeneration)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Find.TickManager.TicksGame % CheckInterval != 0)
            {
                return;
            }

            if (Pawn == null || Pawn.Dead)
            {
                cachedState = RegenState.Idle;
                cachedCooldownTicks = 0;
                return;
            }

            cachedState = EvaluateState(out cachedCooldownTicks);
            if (cachedState == RegenState.Repairing)
            {
                TryRepairDamage();
            }
        }

        public override string CompLabelInBracketsExtra
        {
            get
            {
                return cachedState switch
                {
                    RegenState.Repairing => "SD_Regeneration_State_Repairing".Translate(),
                    RegenState.Cooldown => "SD_Regeneration_State_Cooldown".Translate(),
                    RegenState.Disabled => "SD_Regeneration_State_Disabled".Translate(),
                    _ => "SD_Regeneration_State_Idle".Translate()
                };
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                return cachedState switch
                {
                    RegenState.Repairing => "SD_Regeneration_Tip_Repairing".Translate(),
                    RegenState.Cooldown => "SD_Regeneration_Tip_Cooldown".Translate(TicksToSeconds(cachedCooldownTicks)),
                    RegenState.Disabled => "SD_Regeneration_Tip_Disabled".Translate(),
                    _ => "SD_Regeneration_Tip_Idle".Translate()
                };
            }
        }

        private RegenState EvaluateState(out int cooldownTicks)
        {
            cooldownTicks = 0;
            if (!repairSystemEnabled)
            {
                return RegenState.Disabled;
            }

            int elapsed = Find.TickManager.TicksGame - lastDamageTick;
            cooldownTicks = Props.repairCooldownAfterDamage - elapsed;
            if (cooldownTicks > 0)
            {
                return RegenState.Cooldown;
            }

            cooldownTicks = 0;
            return HasDamageToRepair() ? RegenState.Repairing : RegenState.Idle;
        }

        private static int TicksToSeconds(int ticks)
        {
            if (ticks <= 0)
            {
                return 0;
            }
            return (ticks + 59) / 60;
        }

        private bool HasDamageToRepair()
        {
            if (Pawn?.health?.hediffSet == null)
            {
                return false;
            }

            if (Pawn.health.hediffSet.GetMissingPartsCommonAncestors().Count > 0)
            {
                return true;
            }

            foreach (var part in Pawn.RaceProps.body.AllParts)
            {
                if (Pawn.health.hediffSet.PartIsMissing(part))
                {
                    continue;
                }

                float maxHealth = part.def.GetMaxHealth(Pawn);
                float currentHealth = Pawn.health.hediffSet.GetPartHealth(part);
                if (currentHealth < maxHealth)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryRepairDamage()
        {
            if (TryRepairMissingParts())
            {
                return;
            }

            TryRepairDamagedParts();
        }

        private bool TryRepairMissingParts()
        {
            var missingParts = Pawn.health.hediffSet.GetMissingPartsCommonAncestors();
            if (missingParts == null || missingParts.Count == 0)
            {
                return false;
            }

            Hediff_MissingPart partToRepair = null;
            float minHealth = float.MaxValue;
            foreach (var missingPart in missingParts)
            {
                float partHealth = missingPart.Part.def.GetMaxHealth(Pawn);
                if (partHealth < minHealth)
                {
                    minHealth = partHealth;
                    partToRepair = missingPart;
                }
            }

            if (partToRepair == null)
            {
                return false;
            }

            float repairCost = minHealth * Props.repairCostPerHP;
            if (!CanAffordRepair(repairCost))
            {
                return false;
            }

            if (!ConvertMissingPartToInjury(partToRepair))
            {
                return false;
            }

            ConsumeRepairCost(repairCost);
            return true;
        }

        private bool TryRepairDamagedParts()
        {
            BodyPartRecord partToRepair = null;
            float minHealthRatio = float.MaxValue;

            foreach (var part in Pawn.RaceProps.body.AllParts)
            {
                if (Pawn.health.hediffSet.PartIsMissing(part))
                {
                    continue;
                }

                float maxHealth = part.def.GetMaxHealth(Pawn);
                float currentHealth = Pawn.health.hediffSet.GetPartHealth(part);
                if (currentHealth >= maxHealth)
                {
                    continue;
                }

                float healthRatio = currentHealth / maxHealth;
                if (healthRatio < minHealthRatio)
                {
                    minHealthRatio = healthRatio;
                    partToRepair = part;
                }
            }

            if (partToRepair == null)
            {
                return false;
            }

            float maxPartHealth = partToRepair.def.GetMaxHealth(Pawn);
            float currentPartHealth = Pawn.health.hediffSet.GetPartHealth(partToRepair);
            float healthToRepair = maxPartHealth - currentPartHealth;
            float repairCost = healthToRepair * Props.repairCostPerHP;
            if (!CanAffordRepair(repairCost))
            {
                return false;
            }

            if (!RepairDamagedPart(partToRepair))
            {
                return false;
            }

            ConsumeRepairCost(repairCost);
            return true;
        }

        private bool CanAffordRepair(float repairCost)
        {
            if (!Props.useRepairResource || repairCost <= 0f)
            {
                return true;
            }

            if (Props.repairResourceNeed == null)
            {
                return false;
            }

            Need resourceNeed = Pawn.needs?.TryGetNeed(Props.repairResourceNeed);
            return resourceNeed != null && resourceNeed.CurLevel >= repairCost;
        }

        private void ConsumeRepairCost(float repairCost)
        {
            if (!Props.useRepairResource || repairCost <= 0f || Props.repairResourceNeed == null)
            {
                return;
            }

            Need resourceNeed = Pawn.needs?.TryGetNeed(Props.repairResourceNeed);
            if (resourceNeed != null)
            {
                resourceNeed.CurLevel = Mathf.Max(0f, resourceNeed.CurLevel - repairCost);
            }
        }

        private bool RepairDamagedPart(BodyPartRecord part)
        {
            try
            {
                var hediffsOnPart = new List<Hediff>();
                foreach (var hediff in Pawn.health.hediffSet.hediffs)
                {
                    if (hediff.Part == part)
                    {
                        hediffsOnPart.Add(hediff);
                    }
                }

                if (hediffsOnPart.Count == 0)
                {
                    return false;
                }

                bool anyRepairDone = false;
                foreach (var hediff in hediffsOnPart)
                {
                    if (!CanRepairHediff(hediff))
                    {
                        continue;
                    }

                    Pawn.health.RemoveHediff(hediff);
                    anyRepairDone = true;
                }

                return anyRepairDone;
            }
            catch
            {
                return false;
            }
        }

        private bool CanRepairHediff(Hediff hediff)
        {
            if (IsDisease(hediff))
            {
                return false;
            }

            if (Props.additionalRepairableHediffs?.Contains(hediff.def) == true)
            {
                return true;
            }

            return hediff is Hediff_Injury;
        }

        private bool IsDisease(Hediff hediff)
        {
            string[] diseaseKeywords =
            {
                "Disease", "Flu", "Plague", "Infection", "Malaria",
                "SleepingSickness", "FibrousMechanites", "SensoryMechanites",
                "WoundInfection", "FoodPoisoning", "GutWorms", "MuscleParasites"
            };

            foreach (string keyword in diseaseKeywords)
            {
                if (hediff.def.defName.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ConvertMissingPartToInjury(Hediff_MissingPart missingPart)
        {
            try
            {
                float partMaxHealth = missingPart.Part.def.GetMaxHealth(Pawn);
                float injurySeverity = partMaxHealth - 1f;
                if (partMaxHealth <= 1f)
                {
                    injurySeverity = 0.5f;
                }

                Pawn.health.RemoveHediff(missingPart);
                HediffDef injuryDef = DefDatabase<HediffDef>.GetNamedSilentFail("Crush");
                if (injuryDef == null)
                {
                    return false;
                }

                Hediff injury = HediffMaker.MakeHediff(injuryDef, Pawn, missingPart.Part);
                injury.Severity = injurySeverity;
                Pawn.health.AddHediff(injury);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            lastDamageTick = Find.TickManager.TicksGame;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastDamageTick, "lastDamageTick", -9999);
            Scribe_Values.Look(ref repairSystemEnabled, "repairSystemEnabled", true);
        }
    }
}
