using System.Collections.Generic;
using System.Linq;
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
        private HashSet<string> _processedMemories = new HashSet<string>();

        /// <summary> 已处理过的情境Thought key，首次出现时结算一次并持久化 </summary>
        private HashSet<string> _processedSituationalThoughts = new HashSet<string>();

        /// <summary> 记忆扫描临时集合，用于清理已消失 memory 的去重键 </summary>
        private HashSet<string> _scratchMemoryKeys = new HashSet<string>();

        /// <summary> 情境Thought（def+stage）的下次允许结算tick（内置冷却） </summary>
        private Dictionary<long, int> _situationalThoughtNextApplyTick = new Dictionary<long, int>();

        /// <summary> 情境Thought扫描临时集合（复用以减少分配） </summary>
        private HashSet<long> _scratchSituationalThoughtKeys = new HashSet<long>();

        /// <summary> Mood Thoughts 临时列表（Memory + Situational） </summary>
        private List<Thought> _tmpMoodThoughts = new List<Thought>();

        /// <summary> Social Thoughts 临时列表（按目标Pawn查询） </summary>
        private List<ISocialThought> _tmpSocialThoughts = new List<ISocialThought>();

        /// <summary> 社交Thought（def+stage+otherPawn）的下次允许结算tick（内置冷却） </summary>
        private Dictionary<long, int> _socialThoughtNextApplyTick = new Dictionary<long, int>();

        /// <summary> 连续独处tick计数 </summary>
        private int _ticksAloneCounter = 0;

        #endregion

        #region 属性

        public CompProperties_AIUpbringing Props => (CompProperties_AIUpbringing)props;

        /// <summary> 好感与信任的总和 </summary>
        public float CombinedValue => affection + trust;

        #endregion

        #region 生命周期

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (_processedMemories == null)
                _processedMemories = new HashSet<string>();
            if (_processedSituationalThoughts == null)
                _processedSituationalThoughts = new HashSet<string>();
            if (_scratchMemoryKeys == null)
                _scratchMemoryKeys = new HashSet<string>();
            if (_situationalThoughtNextApplyTick == null)
                _situationalThoughtNextApplyTick = new Dictionary<long, int>();
            if (_scratchSituationalThoughtKeys == null)
                _scratchSituationalThoughtKeys = new HashSet<long>();
            if (_tmpMoodThoughts == null)
                _tmpMoodThoughts = new List<Thought>();
            if (_tmpSocialThoughts == null)
                _tmpSocialThoughts = new List<ISocialThought>();
            if (_socialThoughtNextApplyTick == null)
                _socialThoughtNextApplyTick = new Dictionary<long, int>();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (Current.ProgramState != ProgramState.Playing) return;

            var ai = parent as Pawn;
            if (ai == null) return;
            if (ai.ageTracker.AgeBiologicalYears >= 18) return;

            ScanMemoriesForEvents(ai);
            ScanSituationalThoughtsForEvents(ai);
            // 社交Thought容易与记忆/情境路径语义重复，且会放大刷值；当前版本停用。
            // ScanSocialThoughtsForEvents(ai);
            
            // 被动惩罚系统已被移除
            // TickPassiveLoneliness(ai);
            // TickPassiveNeeds(ai);
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

        #endregion

        #region 存档

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref affection,        "affection",        0f);
            Scribe_Values.Look(ref trust,            "trust",            0f);
            Scribe_Values.Look(ref passiveNaturalAff,"passiveNaturalAff",0f);
            Scribe_Values.Look(ref passiveNaturalTrs,"passiveNaturalTrs",0f);
            Scribe_Values.Look(ref passiveLonelyAff, "passiveLonelyAff", 0f);
            Scribe_Values.Look(ref passiveLonelyTrs, "passiveLonelyTrs", 0f);
            Scribe_Values.Look(ref passiveNeedsTrs,  "passiveNeedsTrs",  0f);
            Scribe_Values.Look(ref passiveEnvTrs,    "passiveEnvTrs",    0f);
            Scribe_Collections.Look(ref eventLog, "eventLog", LookMode.Deep);

            List<string> processedMemoryKeys = null;
            List<string> processedSituationalKeys = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                processedMemoryKeys = _processedMemories?.ToList();
                processedSituationalKeys = _processedSituationalThoughts?.ToList();
            }
            Scribe_Collections.Look(ref processedMemoryKeys, "processedMemoryKeys", LookMode.Value);
            Scribe_Collections.Look(ref processedSituationalKeys, "processedSituationalThoughtKeys", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                _processedMemories = processedMemoryKeys != null
                    ? new HashSet<string>(processedMemoryKeys)
                    : new HashSet<string>();
                _processedSituationalThoughts = processedSituationalKeys != null
                    ? new HashSet<string>(processedSituationalKeys)
                    : new HashSet<string>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (_processedMemories == null)
                    _processedMemories = new HashSet<string>();
                if (_processedSituationalThoughts == null)
                    _processedSituationalThoughts = new HashSet<string>();
                if (_scratchMemoryKeys == null)
                    _scratchMemoryKeys = new HashSet<string>();
                if (_situationalThoughtNextApplyTick == null)
                    _situationalThoughtNextApplyTick = new Dictionary<long, int>();
                if (_scratchSituationalThoughtKeys == null)
                    _scratchSituationalThoughtKeys = new HashSet<long>();
                if (_tmpMoodThoughts == null)
                    _tmpMoodThoughts = new List<Thought>();
                if (_tmpSocialThoughts == null)
                    _tmpSocialThoughts = new List<ISocialThought>();
                if (_socialThoughtNextApplyTick == null)
                    _socialThoughtNextApplyTick = new Dictionary<long, int>();
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

        // 好感度/信任度已由 Gizmo_AIUpbringing 显示，不再在 Inspect 面板重复展示。


        #endregion

        #region 调试

        public string GetDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== AI女儿状态 ===");
            sb.AppendLine($"好感度: {affection:F1}");
            sb.AppendLine($"信任值: {trust:F1}");
            sb.AppendLine($"事件日志条数: {eventLog?.Count ?? 0}");
            return sb.ToString();
        }

        #endregion
    }
}
