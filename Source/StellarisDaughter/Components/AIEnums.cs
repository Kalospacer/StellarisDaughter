namespace StellarisDaughter
{
    /// <summary> AI倾向枚举 </summary>
    public enum AITendency
    {
        Devoted,      // 深深依恋
        Attached,     // 亲近依赖
        Neutral,      // 中立
        Unstable,     // 不稳定
        Corrupted     // 扭曲黑化
    }

    /// <summary> 结局路线枚举 </summary>
    public enum AIEndingRoute
    {
        NotYetDetermined,  // 尚未确定（15岁前）
        FatherBond,        // 父嫁（正面结局）
        DarkCorruption     // 黑化恶堕（负面结局）
    }
}
