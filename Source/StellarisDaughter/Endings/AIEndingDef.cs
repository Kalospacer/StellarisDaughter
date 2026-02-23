using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary> AI女儿结局定义 </summary>
    // ✨ 沐雪写的哦~
    public class AIEndingDef : Def
    {
        public string endingTitle;
        public string endingDescription;
        public string endingTexture;
        public List<EndingCondition> conditions;
        public List<EndingReward> rewards;
        public bool isPositive = true;
        public bool leavesColony = false;
        public bool createsFutureThreat = false;
        public int priority = 0;
        public AIEndingRoute requiredRoute = AIEndingRoute.NotYetDetermined;

        public bool MeetsConditions(CompAIUpbringing comp)
        {
            if (requiredRoute != AIEndingRoute.NotYetDetermined && comp.lockedEnding != requiredRoute)
                return false;
            if (conditions == null || conditions.Count == 0)
                return true;
            return conditions.All(c => c.IsMet(comp));
        }

        public void ApplyRewards(Pawn pawn)
        {
            if (rewards == null) return;
            foreach (var reward in rewards)
                reward.Apply(pawn);
        }

        public string GetEndingText(CompAIUpbringing comp)
        {
            return endingDescription.Translate(
                comp.affection.ToString("F1"),
                comp.trust.ToString("F1"));
        }
    }

    /// <summary> 结局条件 </summary>
    public class EndingCondition
    {
        public EndingConditionType conditionType;
        public float value;

        public bool IsMet(CompAIUpbringing comp)
        {
            switch (conditionType)
            {
                case EndingConditionType.AffectionMinimum: return comp.affection >= value;
                case EndingConditionType.AffectionMaximum: return comp.affection <= value;
                case EndingConditionType.TrustMinimum:     return comp.trust >= value;
                case EndingConditionType.TrustMaximum:     return comp.trust <= value;
                case EndingConditionType.LockedRoute:      return (int)comp.lockedEnding == (int)value;
                default: return false;
            }
        }
    }

    public enum EndingConditionType
    {
        AffectionMinimum,
        AffectionMaximum,
        TrustMinimum,
        TrustMaximum,
        LockedRoute
    }

    /// <summary> 结局奖励 </summary>
    public class EndingReward
    {
        public EndingRewardType rewardType;
        public string defName;
        public float value;

        public void Apply(Pawn pawn)
        {
            switch (rewardType)
            {
                case EndingRewardType.Trait:
                    var traitDef = DefDatabase<TraitDef>.GetNamed(defName, false);
                    if (traitDef != null)
                        pawn.story?.traits?.GainTrait(new Trait(traitDef));
                    break;
                case EndingRewardType.Hediff:
                    var hediffDef = DefDatabase<HediffDef>.GetNamed(defName, false);
                    if (hediffDef != null)
                        pawn.health?.AddHediff(hediffDef);
                    break;
                case EndingRewardType.StatBoost:
                case EndingRewardType.WorldEvent:
                case EndingRewardType.Ability:
                    // TODO: 后续实现
                    break;
            }
        }
    }

    public enum EndingRewardType
    {
        Trait,
        Hediff,
        StatBoost,
        WorldEvent,
        Ability
    }
}
