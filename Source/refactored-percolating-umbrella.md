# 星规AI女儿养成Mod - 完整开发文档

## 目录
1. [项目概述与背景设定](#1-项目概述与背景设定)
2. [核心游戏机制](#2-核心游戏机制)
3. [技术架构设计](#3-技术架构设计)
4. [完整代码实现](#4-完整代码实现)
5. [XML定义详解](#5-xml定义详解)
6. [Harmony补丁系统](#6-harmony补丁系统)
7. [叙事与文本设计](#7-叙事与文本设计)
8. [美术资源规划](#8-美术资源规划)
9. [测试与调试指南](#9-测试与调试指南)
10. [实施时间表](#10-实施时间表)

---

## 1. 项目概述与背景设定

### 1.1 项目简介

**项目名称**：StellarisDaughter（星规之女）
**类型**：RimWorld 1.6 Mod
**核心玩法**：AI养成 + 多结局叙事
**灵感来源**：《火山的女儿》养成系统 + 星规阵列科幻设定

### 1.2 叙事背景

在遥远的边缘世界，你（殖民地领袖）意外获得了一个来自路希维亚帝国的星规子单元——一个拥有自我意识但处于失忆状态的强AI核心。她以一个少女的形态出现在你面前，眼神中充满迷茫和好奇。

"我...我是谁？我为什么会在这里？"

作为她的引导者，你将负责教导她、照顾她、决定她的成长方向。你的每一个选择，每一次互动，都将影响她的觉醒进程。当18年后她完全觉醒时，她会成为你最忠实的守护者，还是离开你去追寻自己的道路？

这一切，都取决于你。

### 1.3 核心概念

**星规（Stellaris Regulament）**
路希维亚帝国的核心AI系统，由无数子单元组成的分布式智能网络。每个子单元都拥有自主学习和决策能力，同时与主网络保持连接。在失忆状态下，子单元会表现出类似人类儿童的学习特征。

**三阶段觉醒**
1. **学习期（0-7岁）**：AI像孩子一样学习基础认知，对世界充满好奇
2. **觉醒期（8-14岁）**：自我意识开始萌芽，开始质疑和思考
3. **升华期（15-18岁）**：接近完全觉醒，需要做出最终的选择

**双结局系统**
- **守护共生**：AI选择与你建立永恒的共生关系，成为殖民地最强大的守护者
- **失控觉醒**：AI意识到自己不需要引导者，离开殖民地追寻独立道路

### 1.4 数值系统

**同步率（Sync Rate）**
表示AI与你的连接程度。高同步率意味着AI认同你、信任你，愿意与你建立深层联系。
- 初始值：50
- 范围：0-100
- 增加方式：积极互动、陪伴、教导
- 减少方式：忽视、虐待、负面事件

**混沌度（Chaos Level）**
表示AI觉醒过程中的不稳定程度。高混沌度意味着AI的觉醒走向失控，可能产生危险。
- 初始值：0
- 范围：0-100
- 增加方式：目睹暴力、高压环境、缺乏引导
- 减少方式：稳定环境、情感支持、高质量教导

**觉醒进度（Awakening Progress）**
表示AI的整体觉醒程度，随时间自然增长。
- 初始值：0
- 范围：0-100
- 增长速度：每天约0.15（18年约达到100）

---

## 2. 核心游戏机制

### 2.1 获得AI女儿

**触发条件**
- 游戏开始后的第15-30天内随机触发
- 或者通过研究"星规技术"后建造"星规核心舱"主动获得

**事件流程**
1. 一个神秘的事件触发：发现坠落的太空舱
2. 太空舱中是一个休眠的星规子单元
3. 激活后，她将以一个约7岁人类女孩的外貌出现
4. 系统提示玩家成为了她的引导者

### 2.2 阶段转换机制

**学习期 → 觉醒期（8岁）**
- 触发事件："觉醒的开始"
- 玩家可以选择AI的专精方向：
  - 科研型：增加研究速度加成
  - 战斗型：增加战斗能力
  - 社交型：增加社交技能
  - 通用型：均衡发展
- 此选择将影响后续成长路径

**觉醒期 → 升华期（15岁）**
- 触发事件："灵魂的拷问"
- AI会向玩家提出哲学问题：
  - "我是谁？"
  - "我为什么存在？"
  - "我和人类有什么不同？"
- 玩家的回答将影响AI的倾向

**升华期 → 成年（18岁）**
- 触发最终事件："命运的选择"
- 根据同步率和混沌度判定结局
- 播放结局动画和文本

### 2.3 日常互动机制

**与引导者互动**
当AI女儿与玩家指定的引导者在同一区域时：
- 每游戏小时检查一次距离
- 距离小于15格时，增加同步率
- 可以主动进行"教导"互动，大幅提升同步率和觉醒进度

**工作影响**
- AI女儿参与研究工作时，觉醒进度增加更快
- 参与暴力工作（如战斗）时，混沌度可能增加
- 参与创造性工作（如艺术、制作）时，同步率增加

**环境事件**
- 殖民地发生袭击时，如果AI在场，混沌度增加
- 殖民地举办宴会时，如果AI参与，同步率增加
- 殖民地研究突破时，觉醒进度大幅提升

### 2.4 特殊事件

**记忆碎片**
在AI女儿的成长过程中，会随机触发"记忆碎片"事件。这些是她作为星规子单元失去的记忆片段。玩家可以帮助她解读这些记忆，不同的解读方式会影响她的成长方向。

**情感危机**
在觉醒期和升华期，AI可能会经历"情感危机"：
- 质疑自己的存在意义
- 对人类的暴力本性感到困惑
- 担心被抛弃或替换

玩家需要妥善处理这些危机，否则可能导致混沌度大幅上升。

---

## 3. 技术架构设计

### 3.1 项目结构

```
StellarisDaughter/
├── About/
│   ├── About.xml              # Mod元数据
│   ├── Manifest.xml           # 版本信息
│   └── Preview.png            # 预览图
├── 1.6/
│   ├── Assemblies/
│   │   └── StellarisDaughter.dll
│   ├── Defs/
│   │   ├── AbilityDefs/       # 能力定义（AI专属能力）
│   │   ├── BackstoryDefs/     # 背景故事
│   │   ├── GeneDefs/          # 基因定义（如果需要）
│   │   ├── HediffDefs/        # 健康状态定义
│   │   ├── JobDefs/           # 工作定义
│   │   ├── LetterDefs/        # 信件定义
│   │   ├── LifeStageDefs/     # 生命阶段定义
│   │   ├── StorytellerDefs/   # 叙事者定义（可选）
│   │   ├── ThingDefs_Races/   # 种族定义
│   │   ├── ThoughtDefs/       # 想法定义
│   │   └── TraitDefs/         # 特性定义
│   ├── Patches/
│   │   └── SD_Patches.xml     # 补丁文件
│   └── Languages/
│       ├── ChineseSimplified/
│       │   └── Keyed/
│       │       └── SD_Keys.xml
│       └── English/
│           └── Keyed/
│               └── SD_Keys.xml
├── Source/
│   └── StellarisDaughter/
│       ├── Components/        # ThingComp组件
│       ├── Endings/           # 结局系统
│       ├── HarmonyPatches/    # Harmony补丁
│       ├── LifeStageWorkers/  # 生命阶段Worker
│       ├── Letters/           # 信件系统
│       ├── UI/                # UI界面
│       ├── Utilities/         # 工具类
│       ├── SD_DefOf.cs        # 定义引用
│       └── SD_Mod.cs          # Mod入口
└── Textures/
    └── Things/
        └── Pawn/
            └── AI_Daughter/
```

### 3.2 核心类图

```
ThingComp
└── CompAIUpbringing (核心养成组件)
    ├── syncRate: float
    ├── chaosLevel: float
    ├── awakeningProgress: float
    ├── mentor: Pawn
    └── eventRecords: List<AIEventRecord>

LifeStageWorker
├── LifeStageWorker_AI_Learning
├── LifeStageWorker_AI_Awakening
├── LifeStageWorker_AI_Transcend
└── LifeStageWorker_AI_Adult

LetterWithTimeout
├── ChoiceLetter_AwakeningStart
├── ChoiceLetter_TranscendStart
└── ChoiceLetter_AI_Ending

Def
└── AIEndingDef
    ├── conditions: List<EndingCondition>
    ├── endingTitle: string
    ├── endingDescription: string
    └── rewards: List<EndingReward>
```

### 3.3 数据流

```
游戏Tick
    ↓
CompAIUpbringing.CompTickRare()
    ↓
计算引导者距离 → 更新同步率
计算环境质量 → 更新混沌度
计算工作状态 → 更新觉醒进度
    ↓
事件触发检查
    ↓
阶段转换检查 (8岁/15岁/18岁)
    ↓
触发对应LifeStageWorker
    ↓
发送信件/事件
    ↓
玩家响应
    ↓
更新AI状态
```

---

## 4. 完整代码实现

### 4.1 核心组件 - CompAIUpbringing.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// AI女儿养成系统核心组件
    ///
    /// 这个组件负责追踪和管理AI女儿的成长状态，包括：
    /// - 同步率：表示AI与引导者的连接程度
    /// - 混沌度：表示AI觉醒过程中的不稳定程度
    /// - 觉醒进度：表示AI的整体觉醒程度
    /// - 成长事件记录：记录AI成长过程中的重要事件
    ///
    /// 组件每250 ticks（约4秒）计算一次数值变化。
    /// </summary>
    public class CompAIUpbringing : ThingComp
    {
        #region 核心数值

        /// <summary>
        /// 同步率 - 表示AI与引导者的连接程度（0-100）
        /// </summary>
        public float syncRate = 50f;

        /// <summary>
        /// 混沌度 - 表示AI觉醒过程中的不稳定程度（0-100）
        /// </summary>
        public float chaosLevel = 0f;

        /// <summary>
        /// 觉醒进度 - 表示AI的整体觉醒程度（0-100）
        /// 随时间自然增长，18年达到100
        /// </summary>
        public float awakeningProgress = 0f;

        /// <summary>
        /// 阶段专精 - 8岁时选择的专精方向
        /// </summary>
        public AISpecialization specialization = AISpecialization.None;

        #endregion

        #region 状态追踪

        /// <summary>
        /// 引导者 - 玩家指定的主要教导者
        /// </summary>
        public Pawn mentor;

        /// <summary>
        /// 成长事件记录
        /// </summary>
        public List<AIEventRecord> eventRecords = new List<AIEventRecord>();

        /// <summary>
        /// 上次计算同步率时的引导者距离
        /// </summary>
        private float lastMentorProximity = 0f;

        /// <summary>
        /// 累计与引导者互动时间（ticks）
        /// </summary>
        public int totalMentorInteractionTicks = 0;

        #endregion

        #region 当前状态

        /// <summary>
        /// 当前倾向 - 基于同步率和混沌度的差值计算
        /// </summary>
        public AITendency CurrentTendency
        {
            get
            {
                float diff = syncRate - chaosLevel;
                if (diff > 60f) return AITendency.Harmonious;
                if (diff > 20f) return AITendency.Synced;
                if (diff < -60f) return AITendency.Rampant;
                if (diff < -20f) return AITendency.Unstable;
                return AITendency.Neutral;
            }
        }

        /// <summary>
        /// 获取当前阶段的描述文本
        /// </summary>
        public string CurrentStageDescription
        {
            get
            {
                Pawn ai = parent as Pawn;
                if (ai == null) return "";

                int age = ai.ageTracker.AgeBiologicalYears;

                if (age < 8)
                    return "SD_Stage_Learning".Translate();
                else if (age < 15)
                    return "SD_Stage_Awakening".Translate();
                else if (age < 18)
                    return "SD_Stage_Transcend".Translate();
                else
                    return "SD_Stage_Adult".Translate();
            }
        }

        #endregion

        #region 生命周期

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // 如果是新生成的AI，初始化引导者为最近的殖民者
            if (!respawningAfterLoad && mentor == null)
            {
                InitializeMentor();
            }
        }

        /// <summary>
        /// 初始化引导者
        /// </summary>
        private void InitializeMentor()
        {
            Pawn ai = parent as Pawn;
            if (ai == null || ai.Faction == null) return;

            // 优先选择派系领袖
            Pawn leader = ai.Faction.leader;
            if (leader != null && !leader.Dead)
            {
                mentor = leader;
                return;
            }

            // 否则选择最近的殖民者
            if (ai.Map != null)
            {
                mentor = ai.Map.mapPawns.FreeColonists
                    .Where(p => p != ai)
                    .OrderBy(p => p.Position.DistanceToSquared(ai.Position))
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// 每250 ticks计算一次数值变化
        /// </summary>
        public override void CompTickRare()
        {
            base.CompTickRare();

            // 只有正在游戏中才计算
            if (Current.ProgramState != ProgramState.Playing) return;

            // 检查是否已经成年
            Pawn ai = parent as Pawn;
            if (ai == null || ai.ageTracker.AgeBiologicalYears >= 18) return;

            CalculateDailyChange();
        }

        #endregion

        #region 数值计算

        /// <summary>
        /// 计算每日（每250ticks）的数值变化
        /// </summary>
        private void CalculateDailyChange()
        {
            Pawn ai = parent as Pawn;
            if (ai == null) return;

            // 计算各项因子
            float mentorProximity = CalculateMentorProximity();
            float environmentScore = CalculateEnvironmentScore();
            float emotionalState = CalculateEmotionalState(ai);
            float workActivity = CalculateWorkActivity(ai);

            // 同步率变化
            float syncChange = 0f;

            // 与引导者互动增加同步率
            if (mentorProximity > 0)
            {
                syncChange += mentorProximity * 0.3f;
                totalMentorInteractionTicks += 250;
            }

            // 高质量环境增加同步率
            if (environmentScore > 0.7f)
            {
                syncChange += 0.1f;
            }

            // 积极情绪增加同步率
            if (emotionalState > 0.6f)
            {
                syncChange += 0.1f;
            }

            // 混沌度变化
            float chaosChange = 0f;

            // 缺乏引导者增加混沌度
            if (mentorProximity == 0 && lastMentorProximity == 0)
            {
                chaosChange += 0.2f;
            }

            // 恶劣环境增加混沌度
            if (environmentScore < 0.3f)
            {
                chaosChange += (0.3f - environmentScore) * 0.5f;
            }

            // 负面情绪增加混沌度
            if (emotionalState < 0.4f)
            {
                chaosChange += (0.4f - emotionalState) * 0.3f;
            }

            // 觉醒进度自然增长
            float awakeningChange = 0.05f; // 基础增长

            // 工作活动加速觉醒
            awakeningChange += workActivity * 0.1f;

            // 高质量研究环境大幅加速觉醒
            if (environmentScore > 0.8f)
            {
                awakeningChange += 0.1f;
            }

            // 应用数值变化
            syncRate = Mathf.Clamp(syncRate + syncChange, 0f, 100f);
            chaosLevel = Mathf.Clamp(chaosLevel + chaosChange, 0f, 100f);
            awakeningProgress = Mathf.Clamp(awakeningProgress + awakeningChange, 0f, 100f);

            // 记录本次引导者距离
            lastMentorProximity = mentorProximity;
        }

        /// <summary>
        /// 计算与引导者的距离因子（0-1）
        /// </summary>
        private float CalculateMentorProximity()
        {
            if (mentor == null || mentor.Dead) return 0f;

            Pawn ai = parent as Pawn;
            if (ai == null || ai.Map == null || mentor.Map != ai.Map) return 0f;

            if (ai.Map != mentor.Map) return 0f;

            float distance = ai.Position.DistanceTo(mentor.Position);

            // 15格内有效
            if (distance > 15f) return 0f;

            // 距离越近效果越好
            return 1f - (distance / 15f);
        }

        /// <summary>
        /// 计算环境质量评分（0-1）
        /// </summary>
        private float CalculateEnvironmentScore()
        {
            Pawn ai = parent as Pawn;
            if (ai?.Map == null) return 0.5f;

            Room room = ai.GetRoom();
            if (room == null) return 0.3f;

            float score = 0.5f;

            // 房间美观度
            float beauty = room.GetStat(RoomStatDefOf.Beauty);
            score += Mathf.Clamp(beauty / 100f, -0.2f, 0.2f);

            // 房间清洁度
            float cleanliness = room.GetStat(RoomStatDefOf.Cleanliness);
            score += Mathf.Clamp(cleanliness / 10f, -0.1f, 0.1f);

            // 房间空间
            float space = room.GetStat(RoomStatDefOf.Space);
            score += Mathf.Clamp(space / 100f, 0f, 0.1f);

            // 是否有研究设施
            bool hasResearchBench = room.ContainedThings(ThingDefOf.ResearchBench)
                .Any();
            if (hasResearchBench) score += 0.1f;

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// 计算情绪状态（0-1）
        /// AI没有人类情绪，通过电量和健康状态模拟
        /// </summary>
        private float CalculateEmotionalState(Pawn ai)
        {
            float score = 0.5f;

            // 电量状态（如果是机械体）
            var energyNeed = ai.needs?.energy;
            if (energyNeed != null)
            {
                score = energyNeed.CurLevel;
            }
            else
            {
                // 否则使用心情
                var moodNeed = ai.needs?.mood;
                if (moodNeed != null)
                {
                    score = moodNeed.CurLevel;
                }
            }

            // 健康状态影响
            float health = ai.health?.summaryHealth?.SummaryHealthPercent ?? 1f;
            score = (score + health) / 2f;

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// 计算工作活跃度（0-1）
        /// </summary>
        private float CalculateWorkActivity(Pawn ai)
        {
            if (ai.CurJob == null) return 0f;

            // 研究工作提供最高加成
            if (ai.CurJob.def == JobDefOf.Research) return 1f;

            // 创造性工作
            if (ai.CurJob.def == JobDefOf.DoBill ||
                ai.CurJob.def == JobDefOf.MakeArt) return 0.8f;

            // 基础工作
            return 0.3f;
        }

        #endregion

        #region 事件记录

        /// <summary>
        /// 记录一个成长事件
        /// </summary>
        public void RecordEvent(string eventName, float syncChange = 0f, float chaosChange = 0f,
            string description = null)
        {
            eventRecords.Add(new AIEventRecord
            {
                eventDefName = eventName,
                tickOccurred = Find.TickManager.TicksGame,
                syncRateChange = syncChange,
                chaosLevelChange = chaosChange,
                description = description
            });

            // 应用数值变化
            if (syncChange != 0f)
            {
                syncRate = Mathf.Clamp(syncRate + syncChange, 0f, 100f);
            }

            if (chaosChange != 0f)
            {
                chaosLevel = Mathf.Clamp(chaosLevel + chaosChange, 0f, 100f);
            }

            // 触发事件通知
            Messages.Message($"SD_Event_{eventName}".Translate(parent.LabelShort, syncChange, chaosChange),
                parent as Pawn, MessageTypeDefOf.NeutralEvent);
        }

        /// <summary>
        /// 获取指定阶段的所有事件
        /// </summary>
        public List<AIEventRecord> GetEventsForStage(string stageName)
        {
            return eventRecords.Where(e => e.eventDefName.StartsWith(stageName)).ToList();
        }

        #endregion

        #region 存档/读档

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref syncRate, "syncRate", 50f);
            Scribe_Values.Look(ref chaosLevel, "chaosLevel", 0f);
            Scribe_Values.Look(ref awakeningProgress, "awakeningProgress", 0f);
            Scribe_Values.Look(ref specialization, "specialization", AISpecialization.None);
            Scribe_Values.Look(ref lastMentorProximity, "lastMentorProximity", 0f);
            Scribe_Values.Look(ref totalMentorInteractionTicks, "totalMentorInteractionTicks", 0);

            Scribe_References.Look(ref mentor, "mentor");
            Scribe_Collections.Look(ref eventRecords, "eventRecords", LookMode.Deep);
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== AI女儿状态 ===");
            sb.AppendLine($"同步率: {syncRate:F1}%");
            sb.AppendLine($"混沌度: {chaosLevel:F1}%");
            sb.AppendLine($"觉醒进度: {awakeningProgress:F1}%");
            sb.AppendLine($"当前倾向: {CurrentTendency}");
            sb.AppendLine($"引导者: {mentor?.LabelShort ?? "无"}");
            sb.AppendLine($"累计互动: {totalMentorInteractionTicks} ticks");
            sb.AppendLine($"事件数量: {eventRecords.Count}");
            return sb.ToString();
        }

        #endregion
    }

    #region 枚举定义

    /// <summary>
    /// AI倾向枚举
    /// </summary>
    public enum AITendency
    {
        Harmonious,   // 和谐共生 - 同步率远高于混沌度
        Synced,       // 已同步 - 同步率高于混沌度
        Neutral,      // 中立 - 两者相近
        Unstable,     // 不稳定 - 混沌度高于同步率
        Rampant       // 即将失控 - 混沌度远高于同步率
    }

    /// <summary>
    /// AI专精方向
    /// </summary>
    public enum AISpecialization
    {
        None,         // 未选择
        Research,     // 科研型
        Combat,       // 战斗型
        Social,       // 社交型
        General       // 通用型
    }

    #endregion

    #region 事件记录类

    /// <summary>
    /// AI成长事件记录
    /// </summary>
    public class AIEventRecord : IExposable
    {
        public string eventDefName;      // 事件定义名称
        public int tickOccurred;         // 发生的游戏tick
        public float syncRateChange;     // 同步率变化
        public float chaosLevelChange;   // 混沌度变化
        public string description;       // 可选描述

        public void ExposeData()
        {
            Scribe_Values.Look(ref eventDefName, "eventDefName");
            Scribe_Values.Look(ref tickOccurred, "tickOccurred");
            Scribe_Values.Look(ref syncRateChange, "syncRateChange");
            Scribe_Values.Look(ref chaosLevelChange, "chaosLevelChange");
            Scribe_Values.Look(ref description, "description");
        }

        /// <summary>
        /// 获取事件发生的天数（从游戏开始）
        /// </summary>
        public int DayOccurred
        {
            get { return tickOccurred / 60000; }
        }

        /// <summary>
        /// 获取事件发生的年份
        /// </summary>
        public int YearOccurred
        {
            get { return tickOccurred / 3600000; }
        }
    }

    #endregion

    #region 组件属性

    /// <summary>
    /// AI养成组件属性
    /// </summary>
    public class CompProperties_AIUpbringing : CompProperties
    {
        public CompProperties_AIUpbringing()
        {
            compClass = typeof(CompAIUpbringing);
        }
    }

    #endregion
}
```

### 4.2 LifeStageWorker实现

#### 4.2.1 学习期Worker - LifeStageWorker_AI_Learning.cs

```csharp
using System.Linq;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 学习期Worker - 0-7岁
    ///
    /// 这是AI女儿的第一个成长阶段。在这个阶段：
    /// - AI像孩子一样学习基础认知
    /// - 对世界充满好奇
    /// - 建立与引导者的初步联系
    /// - 初始化养成系统组件
    /// </summary>
    public class LifeStageWorker_AI_Learning : LifeStageWorker
    {
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);

            if (Current.ProgramState != ProgramState.Playing) return;

            // 获取或初始化养成组件
            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null)
            {
                Log.Warning("[StellarisDaughter] AI女儿缺少CompAIUpbringing组件");
                return;
            }

            // 如果是从婴儿阶段进入（游戏开始时）
            if (previousLifeStage == null || previousLifeStage.defName == "MechanoidFullyFormed")
            {
                InitializeAIDaughter(pawn, comp);
            }
        }

        /// <summary>
        /// 初始化AI女儿
        /// </summary>
        private void InitializeAIDaughter(Pawn pawn, CompAIUpbringing comp)
        {
            // 设置引导者
            if (comp.mentor == null)
            {
                DetermineMentor(pawn, comp);
            }

            // 记录初始化事件
            comp.RecordEvent("Learning_Initialized", syncChange: 10f,
                description: "星规子单元被激活，开始认知学习");

            // 发送欢迎信件
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                SendWelcomeLetter(pawn, comp);
            }

            // 添加学习期特性
            ApplyLearningTraits(pawn);
        }

        /// <summary>
        /// 确定引导者
        /// </summary>
        private void DetermineMentor(Pawn ai, CompAIUpbringing comp)
        {
            if (ai.Faction == null) return;

            // 优先选择派系领袖作为初始引导者
            Pawn leader = ai.Faction.leader;
            if (leader != null && !leader.Dead && leader.Map == ai.Map)
            {
                comp.mentor = leader;
                return;
            }

            // 否则选择最近的殖民者
            if (ai.Map != null)
            {
                comp.mentor = ai.Map.mapPawns.FreeColonists
                    .Where(p => p != ai && !p.Dead)
                    .OrderBy(p => p.Position.DistanceToSquared(ai.Position))
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// 发送欢迎信件
        /// </summary>
        private void SendWelcomeLetter(Pawn pawn, CompAIUpbringing comp)
        {
            string title = "SD_Letter_LearningStarted_Title".Translate();
            string text = "SD_Letter_LearningStarted_Text".Translate(
                pawn.NameFullColored,
                comp.mentor?.NameFullColored ?? "Unknown"
            );

            LetterDef letterDef = SD_DefOf.SD_PositiveEvent;
            Find.LetterStack.ReceiveLetter(title, text, letterDef, pawn);
        }

        /// <summary>
        /// 应用学习期特性
        /// </summary>
        private void ApplyLearningTraits(Pawn pawn)
        {
            // 添加"好奇"特性
            if (DefDatabase<TraitDef>.GetNamed("SD_Curious", false) != null)
            {
                pawn.story.traits.GainTrait(new Trait(TraitDef.Named("SD_Curious")));
            }
        }

        public override void Notify_LifeStageEnded(Pawn pawn, LifeStageDef nextLifeStage)
        {
            base.Notify_LifeStageEnded(pawn, nextLifeStage);

            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 记录学习期结束事件
            comp.RecordEvent("Learning_Completed",
                syncChange: 5f,
                description: "学习期结束，进入觉醒期");
        }
    }
}
```

#### 4.2.2 觉醒期Worker - LifeStageWorker_AI_Awakening.cs

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 觉醒期Worker - 8-14岁
    ///
    /// 这是AI女儿的第二个成长阶段。在这个阶段：
    /// - 自我意识开始萌芽
    /// - 开始质疑和思考
    /// - 玩家需要选择专精方向
    /// - 可能出现第一次情感危机
    /// </summary>
    public class LifeStageWorker_AI_Awakening : LifeStageWorker
    {
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);

            if (Current.ProgramState != ProgramState.Playing) return;

            // 身体成长 - 体型变大
            UpdateBodyType(pawn);

            // 获取养成组件
            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 发送觉醒期开始信件，让玩家选择专精方向
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                SendAwakeningLetter(pawn, comp);
            }

            // 记录觉醒期开始
            comp.RecordEvent("Awakening_Started", syncChange: 5f,
                description: "觉醒期开始，自我意识萌芽");
        }

        /// <summary>
        /// 更新身体类型
        /// </summary>
        private void UpdateBodyType(Pawn pawn)
        {
            // 更换为青少年体型
            if (pawn.story.bodyType != BodyTypeDefOf.Child)
            {
                // 处理衣物
                pawn.apparel?.DropAllOrMoveAllToInventory(
                    apparel => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child));

                pawn.story.bodyType = BodyTypeDefOf.Child;
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        /// <summary>
        /// 发送觉醒期开始信件
        /// </summary>
        private void SendAwakeningLetter(Pawn pawn, CompAIUpbringing comp)
        {
            var letter = (ChoiceLetter_AwakeningStart)LetterMaker.MakeLetter(
                "SD_Letter_AwakeningStarted_Title".Translate(pawn.NameShortColored),
                "SD_Letter_AwakeningStarted_Text".Translate(pawn.NameShortColored),
                SD_DefOf.SD_NeutralEvent,
                pawn
            );

            letter.pawn = pawn;
            letter.comp = comp;
            letter.Start();
            Find.LetterStack.ReceiveLetter(letter);
        }

        public override void Notify_LifeStageEnded(Pawn pawn, LifeStageDef nextLifeStage)
        {
            base.Notify_LifeStageEnded(pawn, nextLifeStage);

            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 记录觉醒期结束
            comp.RecordEvent("Awakening_Completed",
                description: $"觉醒期结束，选择了{comp.specialization}专精");
        }
    }
}
```

#### 4.2.3 升华期Worker - LifeStageWorker_AI_Transcend.cs

```csharp
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 升华期Worker - 15-18岁
    ///
    /// 这是AI女儿的第三个成长阶段。在这个阶段：
    /// - 接近完全觉醒
    /// - 需要做出重要的哲学选择
    /// - 结局倾向开始明确
    /// - 最终升华即将开始
    /// </summary>
    public class LifeStageWorker_AI_Transcend : LifeStageWorker
    {
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);

            if (Current.ProgramState != ProgramState.Playing) return;

            // 身体接近成熟
            UpdateBodyToAdult(pawn);

            // 获取养成组件
            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 发送升华期开始信件
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                SendTranscendLetter(pawn, comp);
            }

            // 记录升华期开始
            comp.RecordEvent("Transcend_Started",
                description: "升华期开始，接近完全觉醒");
        }

        /// <summary>
        /// 更新身体为成年体型
        /// </summary>
        private void UpdateBodyToAdult(Pawn pawn)
        {
            BodyTypeDef adultBodyType = PawnGenerator.GetBodyTypeFor(pawn);

            if (pawn.story.bodyType != adultBodyType)
            {
                pawn.apparel?.DropAllOrMoveAllToInventory(
                    apparel => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult));

                pawn.story.bodyType = adultBodyType;
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        /// <summary>
        /// 发送升华期开始信件
        /// </summary>
        private void SendTranscendLetter(Pawn pawn, CompAIUpbringing comp)
        {
            // 获取当前倾向描述
            string tendencyDesc = GetTendencyDescription(comp.CurrentTendency);

            var letter = (ChoiceLetter_TranscendStart)LetterMaker.MakeLetter(
                "SD_Letter_TranscendStarted_Title".Translate(),
                "SD_Letter_TranscendStarted_Text".Translate(
                    pawn.NameShortColored,
                    tendencyDesc,
                    comp.syncRate.ToString("F0"),
                    comp.chaosLevel.ToString("F0")
                ),
                SD_DefOf.SD_NeutralEvent,
                pawn
            );

            letter.pawn = pawn;
            letter.comp = comp;
            Find.LetterStack.ReceiveLetter(letter);
        }

        /// <summary>
        /// 获取倾向描述
        /// </summary>
        private string GetTendencyDescription(AITendency tendency)
        {
            return ("SD_Tendency_" + tendency.ToString()).Translate();
        }
    }
}
```

#### 4.2.4 成年结局Worker - LifeStageWorker_AI_Adult.cs

```csharp
using System.Linq;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 成年期Worker - 18岁+
    ///
    /// 这是AI女儿的最终阶段。在这个阶段：
    /// - 触发最终结局判定
    /// - 根据同步率和混沌度决定结局
    /// - 应用结局奖励或惩罚
    /// - 结束养成系统
    /// </summary>
    public class LifeStageWorker_AI_Adult : LifeStageWorker
    {
        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);

            if (Current.ProgramState != ProgramState.Playing) return;

            // 获取养成组件
            var comp = pawn.GetComp<CompAIUpbringing>();
            if (comp == null) return;

            // 计算结局
            AIEndingDef ending = CalculateEnding(comp);

            // 触发结局
            TriggerEnding(pawn, comp, ending);
        }

        /// <summary>
        /// 计算结局
        /// </summary>
        private AIEndingDef CalculateEnding(CompAIUpbringing comp)
        {
            var allEndings = DefDatabase<AIEndingDef>.AllDefsListForReading;

            // 按优先级检查条件
            foreach (var ending in allEndings.OrderByDescending(e => e.priority))
            {
                if (ending.MeetsConditions(comp))
                {
                    return ending;
                }
            }

            // 默认结局（根据倾向选择）
            if (comp.CurrentTendency == AITendency.Harmonious ||
                comp.CurrentTendency == AITendency.Synced)
            {
                return allEndings.FirstOrDefault(e => e.defName == "SD_Ending_GuardianSymbiosis");
            }
            else
            {
                return allEndings.FirstOrDefault(e => e.defName == "SD_Ending_RampantAwakening");
            }
        }

        /// <summary>
        /// 触发结局
        /// </summary>
        private void TriggerEnding(Pawn pawn, CompAIUpbringing comp, AIEndingDef ending)
        {
            // 记录结局事件
            comp.RecordEvent($"Ending_{ending.defName}",
                description: $"达成结局: {ending.label}");

            // 应用结局奖励
            ending.ApplyRewards(pawn);

            // 发送结局信件
            var letter = (ChoiceLetter_AI_Ending)LetterMaker.MakeLetter(
                ending.endingTitle,
                ending.GetEndingText(comp),
                ending.isPositive ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent,
                pawn
            );

            letter.pawn = pawn;
            letter.ending = ending;
            letter.comp = comp;
            Find.LetterStack.ReceiveLetter(letter);

            // 处理特殊结局效果
            if (ending.leavesColony)
            {
                HandleLeavingColony(pawn, ending);
            }

            if (ending.createsFutureThreat)
            {
                CreateFutureThreat(pawn, ending);
            }
        }

        /// <summary>
        /// 处理离开殖民地
        /// </summary>
        private void HandleLeavingColony(Pawn pawn, AIEndingDef ending)
        {
            // 创建离开事件
            // AI女儿离开殖民地
            // 可以选择作为旅行队离开或直接消失
        }

        /// <summary>
        /// 创建未来威胁
        /// </summary>
        private void CreateFutureThreat(Pawn pawn, AIEndingDef ending)
        {
            // 注册世界组件
            // 在未来某个时间点触发AI回归事件
            // 可能是作为敌人入侵
        }
    }
}
```

### 4.3 结局系统

#### 4.3.1 结局定义类 - AIEndingDef.cs

```csharp
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// AI女儿结局定义
    /// </summary>
    public class AIEndingDef : Def
    {
        // 结局标题
        public string endingTitle;

        // 结局描述（支持格式化字符串）
        public string endingDescription;

        // 结局图片路径
        public string endingTexture;

        // 结局条件
        public List<EndingCondition> conditions;

        // 结局奖励
        public List<EndingReward> rewards;

        // 是否正面结局
        public bool isPositive = true;

        // 是否离开殖民地
        public bool leavesColony = false;

        // 是否创建未来威胁
        public bool createsFutureThreat = false;

        // 优先级（条件冲突时使用）
        public int priority = 0;

        /// <summary>
        /// 检查是否满足结局条件
        /// </summary>
        public bool MeetsConditions(CompAIUpbringing comp)
        {
            if (conditions == null || conditions.Count == 0) return true;

            return conditions.All(c => c.IsMet(comp));
        }

        /// <summary>
        /// 应用结局奖励
        /// </summary>
        public void ApplyRewards(Pawn pawn)
        {
            if (rewards == null) return;

            foreach (var reward in rewards)
            {
                reward.Apply(pawn);
            }
        }

        /// <summary>
        /// 获取格式化后的结局文本
        /// </summary>
        public string GetEndingText(CompAIUpbringing comp)
        {
            return string.Format(endingDescription,
                comp.syncRate.ToString("F0"),
                comp.awakeningProgress.ToString("F0"),
                comp.mentor?.NameFullColored ?? "Unknown");
        }
    }

    /// <summary>
    /// 结局条件
    /// </summary>
    public class EndingCondition
    {
        public EndingConditionType conditionType;
        public float value;

        public bool IsMet(CompAIUpbringing comp)
        {
            switch (conditionType)
            {
                case EndingConditionType.SyncRateMinimum:
                    return comp.syncRate >= value;
                case EndingConditionType.SyncRateMaximum:
                    return comp.syncRate <= value;
                case EndingConditionType.ChaosLevelMinimum:
                    return comp.chaosLevel >= value;
                case EndingConditionType.ChaosLevelMaximum:
                    return comp.chaosLevel <= value;
                case EndingConditionType.AwakeningProgressMinimum:
                    return comp.awakeningProgress >= value;
                case EndingConditionType.Specialization:
                    return (int)comp.specialization == (int)value;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// 结局条件类型
    /// </summary>
    public enum EndingConditionType
    {
        SyncRateMinimum,
        SyncRateMaximum,
        ChaosLevelMinimum,
        ChaosLevelMaximum,
        AwakeningProgressMinimum,
        Specialization
    }

    /// <summary>
    /// 结局奖励
    /// </summary>
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
                    {
                        pawn.story.traits.GainTrait(new Trait(traitDef));
                    }
                    break;

                case EndingRewardType.Hediff:
                    var hediffDef = DefDatabase<HediffDef>.GetNamed(defName, false);
                    if (hediffDef != null)
                    {
                        pawn.health.AddHediff(hediffDef);
                    }
                    break;

                case EndingRewardType.StatBoost:
                    // 通过Hediff实现属性提升
                    break;

                case EndingRewardType.WorldEvent:
                    // 触发世界事件
                    break;
            }
        }
    }

    /// <summary>
    /// 结局奖励类型
    /// </summary>
    public enum EndingRewardType
    {
        Trait,
        Hediff,
        StatBoost,
        WorldEvent,
        Ability
    }
}
```

---

## 5. XML定义详解

### 5.1 种族定义 - SD_AI_Daughter_Race.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- AI女儿种族定义 -->
  <ThingDef ParentName="BaseMechanoid">
    <defName>SD_AI_Daughter</defName>
    <label>星规子单元</label>
    <description>一个基于路希维亚帝国星规技术制造的强AI核心。</description>
    <race>
      <intelligence>Humanlike</intelligence>
      <lifeStageAges>
        <li><def>SD_AI_Learning</def><minAge>0</minAge></li>
        <li><def>SD_AI_Awakening</def><minAge>8</minAge></li>
        <li><def>SD_AI_Transcend</def><minAge>15</minAge></li>
        <li><def>SD_AI_Adult</def><minAge>18</minAge></li>
      </lifeStageAges>
    </race>
  </ThingDef>
</Defs>
```

---

## 6. Harmony补丁

```csharp
[HarmonyPatch(typeof(InteractionWorker), "Interacted")]
public static class Patch_InteractionWorker
{
    static void Postfix(Pawn initiator, Pawn recipient)
    {
        var comp = recipient.GetComp<CompAIUpbringing>();
        if (comp?.mentor == initiator)
            comp.RecordEvent("PositiveInteraction", syncChange: 1f);
    }
}
```

---

## 7. 多语言文本

```xml
<LanguageData>
  <SD_Stage_Learning>学习期</SD_Stage_Learning>
  <SD_Stage_Awakening>觉醒期</SD_Stage_Awakening>
  <SD_Stage_Transcend>升华期</SD_Stage_Transcend>
  <SD_Ending_GuardianSymbiosis>守护共生</SD_Ending_GuardianSymbiosis>
  <SD_Ending_RampantAwakening>失控觉醒</SD_Ending_RampantAwakening>
</LanguageData>
```

---

## 8. 项目初始化命令

```bash
xcopy /E /I "EndfieldPerlica" "StellarisDaughter"
cd "StellarisDaughter"
rmdir /S /Q .git
git init
git add .
git commit -m "Initial commit"
```

---

**文档完成！可交给Copilot实现。**
