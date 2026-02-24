# StellarisDaughter - 开发日志

> 最后更新：2026-02-23 | 编译状态：✅ Release 通过

## 项目状态：AI 养成系统核心架构重构完成，占位贴图阶段

---

## 一、数值系统

### 核心双轴

| 字段 | 范围 | 含义 |
|------|------|------|
| `affection` | -1000 ~ +1000 | 好感度：正=亲密，负=疏离/叛逆 |
| `trust` | -1000 ~ +1000 | 信任值：正=依赖殖民地，负=离心 |
| `lockedEnding` | 枚举 | 15 岁时锁定结局路线 |

> **旧架构（已废弃）**：syncRate / chaosLevel / awakeningProgress / mentor

### 数值来源

```
增长（正向） → 仅由 AIEventResponseDef 事件驱动（ThoughtDef 记忆匹配）
惩罚（负向） → 被动系统：孤独 / 需求不足 / 环境差
```

被动系统**不自然增长**，只惩罚：
- **孤独**：15 格内无殖民者，按时长分 3 级（6h / 1d / 3d）
- **需求**：食物/休息/舒适/娱乐低于 0.5 才扣信任
- **环境**：居室美感为负才扣信任

---

## 二、代码结构

```
Source/StellarisDaughter/
├── SD_Mod.cs                          # 入口：Harmony 注册
├── SD_DefOf.cs                        # DefOf 引用
├── Components/
│   ├── CompAIUpbringing.cs            # partial 核心：字段/生命周期/Apply/存档/Gizmo入口
│   ├── CompAIUpbringing.MemoryScan.cs # partial：TickRare 扫描 Thought_Memory → AIEventResponseDef
│   ├── CompAIUpbringing.Passive.cs    # partial：孤独+需求被动惩罚（纯惩罚，无自然增长）
│   ├── CompAIUpbringing.Tooltip.cs    # partial：Gizmo 悬浮窗文本
│   ├── CompProperties_AIUpbringing.cs
│   ├── Gizmo_AIUpbringing.cs          # 双向进度条 Gizmo（DrawBiBar，0居中，±1000）
│   ├── AIEventLogEntry.cs             # 事件流水日志（IExposable，最多30条）
│   └── AIEnums.cs                     # AIEndingRoute 枚举
├── Defs/
│   └── AIEventResponseDef.cs          # 事件对照表 Def 类
├── Endings/
│   └── AIEndingDef.cs                 # 结局定义：条件/奖励/文本
└── LifeStageWorkers/
    ├── LifeStageWorker_AI_Childhood.cs
    ├── LifeStageWorker_AI_Youth.cs
    ├── LifeStageWorker_AI_Adulthood.cs
    └── LifeStageWorker_AI_Final.cs
```

```
1.6/1.6/Defs/
├── AIEventResponseDefs/SD_AIEventResponses.xml  # 35+ 事件对照表（可 Patch 扩展）
├── EndingDefs/                                  # 结局 XML
├── LifeStageDefs/SD_LifeStages.xml              # 4 个生命阶段
├── ThingDef_Races/SD_AI_Daughter_Race.xml       # 种族定义（AlienRace）
└── ...
```

---

## 三、关键类说明

### `CompAIUpbringing`

- `Apply(float aff, float trs, string label)` — 统一数值入口；`label=null` 静默（不写日志）
- `CompTickRare()` — 约每 250 tick：ScanMemories → TickLoneliness → TickNeeds → CheckOmenLetter
- `PostExposeData()` — 序列化：affection / trust / lockedEnding / omenLetterSent / 6个被动分桶 / eventLog

**MemoryScan**：遍历 `Thought_Memory` 列表，用 `RuntimeHelpers.GetHashCode(memory)` 做 HashSet 去重（Session 内有效），匹配 `DefDatabase<AIEventResponseDef>` 后调用 Apply。

### `AIEventResponseDef`

```xml
<StellarisDaughter.AIEventResponseDef>
  <defName>AteLavishMeal</defName>   <!-- 对应原版 ThoughtDef.defName -->
  <eventLabel>享用了豪华餐</eventLabel>
  <affDelta>4</affDelta>
  <trustDelta>5</trustDelta>
</StellarisDaughter.AIEventResponseDef>
```

### `Gizmo_AIUpbringing`

双向进度条（0 居中，±1000）：上行好感度（粉/暗红），下行信任值（天蓝/深蓝）。悬浮窗展示近 10 条事件流水 + 被动分桶。

### `AIEndingDef`

`EndingConditionType`：`AffectionMinimum/Maximum`、`TrustMinimum/Maximum`、`LockedRoute`

---

## 四、生命阶段

| 阶段 | 年龄 | 关键行为 |
|------|------|---------|
| 童年期 | 0-7 | `Apply(+10,+10,"初次激活")`，发激活信件，赋予 SD_Curious 特质 |
| 青年期 | 8-14 | `Apply(+5,+5,"进入青年期")`，13-14 岁发征兆信件 |
| 成年期 | 15-17 | 锁定 `lockedEnding`，发路线锁定信件 |
| 完全觉醒 | 18+ | 按 priority 匹配 `AIEndingDef`，触发结局 |

---

## 五、语言键（新架构关键键）

| 键 | 用途 |
|----|------|
| `SD_GizmoAff` / `SD_GizmoTrs` | Gizmo 进度条标签 |
| `SD_GizmoAff_TipHeader` / `SD_GizmoTrs_TipHeader` | 悬浮窗标题 |
| `SD_TipEvents` / `SD_TipNoEvents` / `SD_TipPassive` | 悬浮窗区块标题 |
| `SD_InspectAffection` / `SD_InspectTrust` | 检查面板 |

---

## 六、种族与外观

- `SD_AI_Daughter_Race`（AlienRace）：全女性，`maleGenderProbability: 0`，`minAgeForAdulthood: 15`
- 贴图：占位复用 EndfieldPerlica 资源（`Endfield/Things/Perlica/`）
- Facial Animation：`MayRequire="Nals.FacialAnimation"` 条件加载，FA Defs 在 `1.6/FA/`

| 体型 | 年龄 | 贴图 |
|------|------|------|
| Child | 0-7 | Naked_Child_*.png |
| Thin | 8+ | Naked_Thin_*.png |

---

## 七、注意事项

- `Thought_Memory.age` 是已存活 tick 数，不存在 `creationTick`
- `_processedMemories` 不序列化，存档读取后重置，同一记忆存档后次日会再次触发一次（可接受）
- 旧的互动 Harmony Patch 已移除，互动增减值完全由 MemoryScan（Thought_Memory → AIEventResponseDef）驱动

---

## 八、构建

```powershell
cd "Source\StellarisDaughter"
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" StellarisDaughter.csproj -p:Configuration=Release -verbosity:minimal
# 输出：1.6/1.6/Assemblies/StellarisDaughter.dll
```

---

## 九、待办

- [ ] **ThoughtWorkers_SD.cs** — 5 个 Situation ThoughtWorker（HighAff/LowAff/HighTrs/LowTrs/Lonely）
- [ ] **SD_ThoughtDefs.xml** — 对应 5 条 ThoughtDef，带 `baseMoodEffect`，让心情面板反映养成状态
- [ ] 补充 5 个 ThoughtDef 语言键
- [ ] 替换占位贴图（童年体型、Thin体型、头部、FA 脸部）
- [ ] `AIEndingDef.ApplyRewards()` / `leavesColony` 逻辑完善
- [ ] `SD_AI_Childhood` Worker 中 `SD_Curious` 特质 Def 实现
- [ ] 清理旧占位语言键（SD_GizmoSync/Chaos/Awaken 等）
