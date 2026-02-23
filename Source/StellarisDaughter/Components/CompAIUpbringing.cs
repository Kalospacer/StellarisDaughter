using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// AI女儿养成系统核心组件（Core）
    /// 字段声明、生命周期、Apply、存档、Gizmo 入口。
    /// 其余逻辑分布在同名 partial 文件中：
    ///   - CompAIUpbringing.MemoryScan.cs  记忆扫描
    ///   - CompAIUpbringing.Passive.cs     被动积累
    ///   - CompAIUpbringing.Tooltip.cs     Gizmo 悬浮窗文本
    /// </summary>
    public partial class CompAIUpbringing : ThingComp
    {
        #region 核心数值

        /// <summary> 好感度 -1000~+1000，正值越高越亲密，负值越低越疏离甚至叛逆 </summary>
        public float affection = 0f;

        /// <summary> 信任值 -1000~+1000，正值越高越依赖殖民地，负值越低越离心离德 </summary>
        public float trust = 0f;

        /// <summary> 锁定的结局路线 - 15岁时确定 </summary>
        public AIEndingRoute lockedEnding = AIEndingRoute.NotYetDetermined;

        /// <summary> 是否已发送征兆信件 </summary>
        public bool omenLetterSent = false;

        /// <summary> 成长事件日志，最多保留30条 </summary>
        public List<AIEventLogEntry> eventLog = new List<AIEventLogEntry>();

        #endregion

        #region 被动积累分桶（用于Tooltip显示来源分析）

        public float passiveNaturalAff = 0f;   // 自然陪伴 → 好感
        public float passiveNaturalTrs = 0f;   // 自然陪伴 → 信任
        public float passiveLonelyAff  = 0f;   // 孤独 → 好感
        public float passiveLonelyTrs  = 0f;   // 孤独 → 信任
        public float passiveNeedsTrs   = 0f;   // 需求满足 → 信任
        public float passiveEnvTrs     = 0f;   // 环境质量 → 信任

        #endregion

        #region 内部状态（不序列化）

        /// <summary> 已处理的思绪记忆key，防止重复计算 </summary>
        private HashSet<long> _processedMemories = new HashSet<long>();

        /// <summary> 连续独处tick计数 </summary>
        private int _ticksAloneCounter = 0;

        #endregion

        #region 属性

        public CompProperties_AIUpbringing Props => (CompProperties_AIUpbringing)props;

        /// <summary> 当前数值偏向的结局 </summary>
        public AIEndingRoute CurrentLeaning =>
            (affection + trust) >= 0f ? AIEndingRoute.FatherBond : AIEndingRoute.DarkCorruption;

        #endregion

        #region 生命周期

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (_processedMemories == null)
                _processedMemories = new HashSet<long>();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (Current.ProgramState != ProgramState.Playing) return;

            var ai = parent as Pawn;
            if (ai == null) return;
            if (ai.ageTracker.AgeBiologicalYears >= 18) return;

            ScanMemoriesForEvents(ai);
            TickPassiveLoneliness(ai);
            TickPassiveNeeds(ai);
            CheckOmenLetter(ai);
        }

        #endregion



        #region 核心Apply + OmenLetter + 锁定结局

        /// <summary>
        /// 应用好感度/信任值变化，并记录事件日志（label为null则为被动积累，不记录）
        /// </summary>
        public void Apply(float aff, float trs, string label)
        {
            affection = Mathf.Clamp(affection + aff, -1000f, 1000f);
            trust     = Mathf.Clamp(trust     + trs, -1000f, 1000f);

            if (label.NullOrEmpty()) return;

            var entry = new AIEventLogEntry
            {
                label    = label,
                affDelta = aff,
                trsDelta = trs,
                tick     = Find.TickManager.TicksGame
            };
            eventLog.Add(entry);
            if (eventLog.Count > 30)
                eventLog.RemoveAt(0);
        }

        private void CheckOmenLetter(Pawn ai)
        {
            if (omenLetterSent) return;
            if (ai.ageTracker.AgeBiologicalYears < 13 || ai.ageTracker.AgeBiologicalYears >= 15) return;

            omenLetterSent = true;
            string title = "SD_Letter_Omen_Title".Translate(ai.NameShortColored);
            string text = CurrentLeaning == AIEndingRoute.FatherBond
                ? "SD_Letter_Omen_Positive".Translate(ai.NameShortColored)
                : "SD_Letter_Omen_Negative".Translate(ai.NameShortColored);
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent, ai);
        }

        public void LockEndingRoute()
        {
            if (lockedEnding != AIEndingRoute.NotYetDetermined) return;
            lockedEnding = CurrentLeaning;
        }

        #endregion

        #region 存档

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref affection,        "affection",        0f);
            Scribe_Values.Look(ref trust,            "trust",            0f);
            Scribe_Values.Look(ref lockedEnding,     "lockedEnding",     AIEndingRoute.NotYetDetermined);
            Scribe_Values.Look(ref omenLetterSent,   "omenLetterSent",   false);
            Scribe_Values.Look(ref passiveNaturalAff,"passiveNaturalAff",0f);
            Scribe_Values.Look(ref passiveNaturalTrs,"passiveNaturalTrs",0f);
            Scribe_Values.Look(ref passiveLonelyAff, "passiveLonelyAff", 0f);
            Scribe_Values.Look(ref passiveLonelyTrs, "passiveLonelyTrs", 0f);
            Scribe_Values.Look(ref passiveNeedsTrs,  "passiveNeedsTrs",  0f);
            Scribe_Values.Look(ref passiveEnvTrs,    "passiveEnvTrs",    0f);
            Scribe_Collections.Look(ref eventLog, "eventLog", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (_processedMemories == null)
                    _processedMemories = new HashSet<long>();
                if (eventLog == null)
                    eventLog = new List<AIEventLogEntry>();
            }
        }

        #endregion

        #region UI / Gizmos

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Gizmo_AIUpbringing(this);
        }

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"SD_InspectAffection".Translate()}: {affection:+F1;-F1;+0.0}");
            sb.AppendLine($"{"SD_InspectTrust".Translate()}: {trust:+F1;-F1;+0.0}");
            return sb.ToString().TrimEndNewlines();
        }

        #endregion

        #region 调试

        public string GetDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== AI女儿状态 ===");
            sb.AppendLine($"好感度: {affection:F1}");
            sb.AppendLine($"信任值: {trust:F1}");
            sb.AppendLine($"偏向: {CurrentLeaning}");
            sb.AppendLine($"锁定结局: {lockedEnding}");
            sb.AppendLine($"征兆信件: {(omenLetterSent ? "已发送" : "未发送")}");
            sb.AppendLine($"事件日志条数: {eventLog?.Count ?? 0}");
            return sb.ToString();
        }

        #endregion
    }
}
