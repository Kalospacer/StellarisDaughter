using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 语音条件 Worker 基类。
    /// 子类 override Satisfied() 判断当前上下文是否满足此条件。
    /// 在 VoiceLineDef 的 conditionWorkerClass 中引用具体子类名即可扩展。
    /// </summary>
    public abstract class VoiceConditionWorker
    {
        /// <summary>反向引用所属 VoiceLineDef（由框架自动赋值）</summary>
        public VoiceLineDef def;

        /// <summary>当前上下文是否满足此条件</summary>
        public abstract bool Satisfied(VoiceContext ctx);
    }

    /// <summary>始终满足 — 用于默认/兜底语音条目</summary>
    public class VoiceConditionWorker_Always : VoiceConditionWorker
    {
        public override bool Satisfied(VoiceContext ctx) => true;
    }

    /// <summary>轻度精神崩溃风险 — 心情低于小崩溃阈值但高于中崩溃阈值</summary>
    public class VoiceConditionWorker_MinorBreakRisk : VoiceConditionWorker
    {
        public override bool Satisfied(VoiceContext ctx)
        {
            var breaker = ctx.pawn?.mindState?.mentalBreaker;
            if (breaker == null) return false;
            return breaker.CurMood < breaker.BreakThresholdMinor
                && breaker.CurMood >= breaker.BreakThresholdMajor;
        }
    }

    /// <summary>中度精神崩溃风险 — 心情低于中崩溃阈值但高于重崩溃阈值</summary>
    public class VoiceConditionWorker_MajorBreakRisk : VoiceConditionWorker
    {
        public override bool Satisfied(VoiceContext ctx)
        {
            var breaker = ctx.pawn?.mindState?.mentalBreaker;
            if (breaker == null) return false;
            return breaker.CurMood < breaker.BreakThresholdMajor
                && breaker.CurMood >= breaker.BreakThresholdExtreme;
        }
    }

    /// <summary>重度精神崩溃风险 — 心情低于重崩溃阈值</summary>
    public class VoiceConditionWorker_ExtremeBreakRisk : VoiceConditionWorker
    {
        public override bool Satisfied(VoiceContext ctx)
        {
            var breaker = ctx.pawn?.mindState?.mentalBreaker;
            if (breaker == null) return false;
            return breaker.CurMood < breaker.BreakThresholdExtreme;
        }
    }

    /// <summary>受伤状态（生命值低于 85%）</summary>
    public class VoiceConditionWorker_Injured : VoiceConditionWorker
    {
        public override bool Satisfied(VoiceContext ctx)
        {
            return ctx.pawn?.health?.summaryHealth?.SummaryHealthPercent < 0.85f;
        }
    }

    /// <summary>持有昂贵武器（市值 > 2500 或 传说品质）</summary>
    public class VoiceConditionWorker_HasExpensiveWeapon : VoiceConditionWorker
    {
        private const float MarketValueThreshold = 2500f;

        public override bool Satisfied(VoiceContext ctx)
        {
            var weapon = ctx.pawn?.equipment?.Primary;
            if (weapon == null) return false;

            if (weapon.MarketValue >= MarketValueThreshold) return true;

            var qualComp = weapon.TryGetComp<RimWorld.CompQuality>();
            if (qualComp != null && qualComp.Quality >= RimWorld.QualityCategory.Legendary)
                return true;

            return false;
        }
    }

    /// <summary>攻击目标为人类</summary>
    public class VoiceConditionWorker_TargetHuman : VoiceConditionWorker
    {
        public override bool Satisfied(VoiceContext ctx)
        {
            if (ctx.target is Pawn targetPawn)
                return targetPawn.RaceProps?.Humanlike == true;
            return false;
        }
    }

    /// <summary>攻击目标为机械体</summary>
    public class VoiceConditionWorker_TargetMechanoid : VoiceConditionWorker
    {
        public override bool Satisfied(VoiceContext ctx)
        {
            if (ctx.target is Pawn targetPawn)
                return targetPawn.RaceProps?.IsMechanoid == true;
            return false;
        }
    }
}
