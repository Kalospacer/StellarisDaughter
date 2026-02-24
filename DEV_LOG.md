# StellarisDaughter - 开发日志

> 最后更新：2026-02-24 | 编译状态：✅ Release 通过

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
├── Voice/                             # ★ RTS 单位语音框架
│   ├── VoiceContext.cs                # 事件上下文结构体
│   ├── VoiceEventDef.cs               # 事件类型 Def（纯标识 key）
│   ├── VoiceConditionWorker.cs        # 条件 Worker 基类 + 8 个内置子类
│   ├── VoiceLineDef.cs                # 事件+条件+音效 三合一绑定 Def
│   ├── UnitVoicePackDef.cs            # 语音包聚合 Def + Resolve 匹配
│   ├── CompProperties_UnitVoice.cs    # Comp 属性
│   ├── CompUnitVoice.cs               # 运行时：冷却/彩蛋/TryPlay 入口
│   └── SD_VoiceEventDefOf.cs          # 事件 Def 静态引用
├── Defs/
│   └── AIEventResponseDef.cs          # 事件对照表 Def 类
├── Endings/
│   └── AIEndingDef.cs                 # 结局定义：条件/奖励/文本
├── HarmonyPatches/
│   ├── Patch_SkillRecord_Interval.cs  # 技能衰减屏蔽
│   ├── Patch_ThingSelected.cs         # ★ 选中 → 语音
│   ├── Patch_DraftController.cs       # ★ 征召 → 语音
│   └── Patch_TryTakeOrderedJob.cs     # ★ 移动/攻击指令 → 语音
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
├── SoundDefs/SD_VoiceSounds.xml                 # ★ 15 个语音 SoundDef（AudioGrain_Folder）
├── VoiceDefs/                                   # ★ 语音框架 Defs
│   ├── SD_VoiceEventDefs.xml                    #   5 个事件类型
│   ├── SD_VoiceLineDefs.xml                     #   15 个语音条目绑定
│   └── SD_VoicePackDefs.xml                     #   星规语音包
├── ThingDef_Races/SD_AI_Daughter_Race.xml       # 种族定义（AlienRace）
└── ...
```

```
Content/Sounds/Voice/SD_Daughter/                # ★ 音频文件目录
├── Selected/Normal/                              #   选中-常规
├── Selected/MinorBreak/                          #   选中-轻度崩溃风险
├── Selected/MajorBreak/                          #   选中-中度崩溃风险
├── Selected/ExtremeBreak/                        #   选中-重度崩溃风险
├── Drafted/Normal/                               #   征召-常规
├── Drafted/ExpensiveWeapon/                      #   征召-持有昂贵武器
├── Drafted/Injured/                              #   征召-受伤
├── Move/Normal/                                  #   移动-常规
├── Move/Injured/                                 #   移动-受伤
├── Attack/Normal/                                #   攻击-常规
├── Attack/Human/                                 #   攻击-对人类
├── Attack/Mechanoid/                             #   攻击-对机械体
├── Attack/Injured/                               #   攻击-受伤
└── SpamClick/                                    #   彩蛋-连点
```

---

## 三-B、RTS 单位语音框架（UnitVoice）

### 概述

类似《星际争霸》《红色警戒》《魔兽争霸》的 RTS 单位语音功能。选中、征召、下达移动/攻击指令时播放角色语音。框架完全基于 **Def + Worker 模式**，新增事件 hook **无需修改框架核心代码**。

### 架构设计

```
┌────────────────────┐     ┌──────────────────┐     ┌───────────────────┐
│   Harmony Patch     │────▶│  CompUnitVoice    │────▶│ UnitVoicePackDef  │
│  (触发时机)         │     │  .TryPlay(        │     │  .Resolve(        │
│                     │     │   eventDef, ctx)  │     │   eventDef, ctx)  │
└────────────────────┘     └──────────────────┘     └───────────────────┘
                                    │                        │
                                    │ 冷却/彩蛋              │ 按 priority 遍历
                                    │                        ▼
                                    │               ┌──────────────────┐
                                    │               │  VoiceLineDef     │
                                    │               │  eventDef         │
                                    │               │  conditionWorker  │──▶ Satisfied(ctx)?
                                    │               │  sound            │
                                    │               │  priority         │
                                    │               └──────────────────┘
                                    │                        │
                                    ▼                        ▼
                            sound.PlayOneShotOnCamera()   SoundDef
                                                      (AudioGrain_Folder)
```

### 核心类说明

#### `VoiceContext`（struct）
```csharp
struct VoiceContext {
    Pawn pawn;      // 语音主体
    Thing target;   // 攻击/交互目标（可 null）
    Job job;        // 当前 Job（可 null）
    Map map;        // 当前地图
}
```
Harmony Patch 构造此结构传入 `CompUnitVoice.TryPlay()`。未来扩展字段（如 `Ability`、`DamageInfo`）只需扩展此 struct。

#### `VoiceEventDef`（Def，纯标识）
事件类型 key，不带 Worker。事件触发时机由 Harmony Patch 负责。

当前已定义的事件：

| defName | 触发时机 | Harmony Patch |
|---------|---------|---------------|
| `SD_VoiceEvent_Selected` | 选中 pawn | `Patch_ThingSelected` → `Thing.Notify_ThingSelected()` |
| `SD_VoiceEvent_Drafted` | 征召 pawn | `Patch_DraftController` → `Pawn_DraftController.set_Drafted` |
| `SD_VoiceEvent_MoveOrder` | 移动指令（Goto） | `Patch_TryTakeOrderedJob` → `Pawn_JobTracker.TryTakeOrderedJob` |
| `SD_VoiceEvent_AttackOrder` | 攻击指令（近战/远程） | 同上，检查 `AttackMelee`/`AttackStatic` |
| `SD_VoiceEvent_SpamClick` | 连续快速点击彩蛋 | 由 `CompUnitVoice` 内部检测 |

#### `VoiceConditionWorker`（abstract，Worker 模式）
条件判断基类。子类 override `Satisfied(VoiceContext ctx)` 返回 bool。

| 内置 Worker 类 | 判断逻辑 |
|----------------|---------|
| `VoiceConditionWorker_Always` | 永远 true（兜底） |
| `VoiceConditionWorker_MinorBreakRisk` | 轻度精神崩溃风险 |
| `VoiceConditionWorker_MajorBreakRisk` | 中度精神崩溃风险 |
| `VoiceConditionWorker_ExtremeBreakRisk` | 重度精神崩溃风险 |
| `VoiceConditionWorker_Injured` | `SummaryHealthPercent < 0.85` |
| `VoiceConditionWorker_HasExpensiveWeapon` | 主武器市值 > 2500 或传说品质 |
| `VoiceConditionWorker_TargetHuman` | 攻击目标为人类 |
| `VoiceConditionWorker_TargetMechanoid` | 攻击目标为机械体 |

#### `VoiceLineDef`（Def）
将事件 + 条件 + 音效三者绑定：
- `VoiceEventDef eventDef` — 响应哪个事件
- `Type conditionWorkerClass` — 条件 Worker（默认 `VoiceConditionWorker_Always`）
- `SoundDef sound` — 播放的音效
- `int priority` — 优先级（高值优先匹配）

使用标准 RimWorld Worker 懒加载模式（`Activator.CreateInstance` + 缓存）。

#### `UnitVoicePackDef`（Def）
聚合一套语音配置：
- `List<VoiceLineDef> voiceLines` — 所有语音条目引用
- `float cooldownSeconds` — 冷却间隔（默认 2.5 秒）
- `int spamClickCount` — 彩蛋触发次数（默认 5）
- `float spamClickWindowSeconds` — 连点窗口（默认 3 秒）

核心方法 `Resolve(eventDef, ctx)`：筛选匹配事件 → 按 priority 降序 → 第一个 `Satisfied()` 为 true 的 → 返回其 SoundDef。

#### `CompUnitVoice`（ThingComp）
运行时组件，唯一公开入口：
```csharp
public void TryPlay(VoiceEventDef eventDef, VoiceContext ctx)
```
流程：
1. **彩蛋检测**（仅 Selected 事件）：时间窗口内点击次数超过阈值 → 切换为 SpamClick 事件
2. **冷却检查**：彩蛋无视冷却，其余事件受 `cooldownSeconds` 限制
3. **解析**：调用 `voicePack.Resolve()` 获取 SoundDef
4. **播放**：`sound.PlayOneShotOnCamera(map)`

### 添加音频文件

每个 `SoundDef` 使用 `AudioGrain_Folder` 指向一个文件夹，引擎自动扫描其中所有音频随机播放。

**步骤**：
1. 准备 `.wav` 或 `.ogg` 音频文件（命名随意）
2. 放入对应目录 `Content/Sounds/Voice/SD_Daughter/<类别>/<条件>/`
3. 无需修改任何 XML 或 C# — 引擎自动识别

**音频对应台词**（来自 对话文本.md）：

| 目录 | 对应台词 |
|------|---------|
| `Selected/Normal/` | "我在这里。" "您好？" "我在听。" |
| `Selected/MinorBreak/` | "……嗯？抱歉，我刚刚在想别的事情。" "又有什么事？" "我听到了，我听到了。" |
| `Selected/MajorBreak/` | "您好？（不耐烦）" "我是不是不该在这里？" "啧。" |
| `Selected/ExtremeBreak/` | "这个地方令人失望。" "这些人到底有什么用。" "我早该把这些离散低效的事物从我身边排除干净。" "低效之物。" |
| `Drafted/Normal/` | "如您所愿。" "您的意志。" "我已就绪。" "整装待发。" |
| `Drafted/ExpensiveWeapon/` | "我的利刃在等待。" "锋刃将予以裁决。" "狩猎开始。" |
| `Drafted/Injured/` | "我会服从。" "我能坚持。" "坚韧不拔。" |
| `Move/Normal/` | "前往中。" "好位置。" "我相信您的判断。" "一往无前。" |
| `Move/Injured/` | "知道了。" "嗯，我会去做。" "不算困难。" |
| `Attack/Normal/` | "碾碎他们。" "摧毁他们。" "命中注定。" "湮灭降临。" |
| `Attack/Human/` | "他们的愚蠢将葬送自己的生命。" "血肉苦弱。" "死亡总是迫切。" |
| `Attack/Mechanoid/` | "低劣的造物。" "回炉重造吧。" "服从，或者毁灭。" |
| `Attack/Injured/` | "以血还血。" "你们将为此付出代价。" "纷争之火，炽烈燃烧。" |
| `SpamClick/` | （彩蛋台词，待定） |

### 扩展指南

#### 扩展新事件（如"受击时播放语音"）

只需 3 步，不修改框架核心代码：

**第 1 步：XML 新增 VoiceEventDef**
```xml
<StellarisDaughter.VoiceEventDef>
  <defName>SD_VoiceEvent_TakeDamage</defName>
  <label>受击</label>
</StellarisDaughter.VoiceEventDef>
```

**第 2 步：XML 新增 VoiceLineDef（绑定音效）**
```xml
<StellarisDaughter.VoiceLineDef>
  <defName>SD_VL_TakeDamage_Normal</defName>
  <eventDef>SD_VoiceEvent_TakeDamage</eventDef>
  <conditionWorkerClass>StellarisDaughter.VoiceConditionWorker_Always</conditionWorkerClass>
  <sound>SD_Voice_TakeDamage_Normal</sound>
  <priority>0</priority>
</StellarisDaughter.VoiceLineDef>
```
并在 `UnitVoicePackDef` 的 `voiceLines` 中追加引用，以及创建对应的 `SoundDef`。

**第 3 步：C# 新增 Harmony Patch（约 20 行）**
```csharp
[HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
public static class Patch_TakeDamage
{
    [HarmonyPostfix]
    public static void Postfix(Thing __instance, DamageWorker.DamageResult __result)
    {
        var comp = __instance.TryGetComp<CompUnitVoice>();
        if (comp == null) return;
        var pawn = __instance as Pawn;
        if (pawn == null) return;
        comp.TryPlay(SD_VoiceEventDefOf.SD_VoiceEvent_TakeDamage, new VoiceContext
        {
            pawn = pawn,
            map = pawn.MapHeld
        });
    }
}
```
在 `SD_VoiceEventDefOf` 中追加对应的静态字段即可。

#### 扩展新条件 Worker

继承 `VoiceConditionWorker`，override `Satisfied(VoiceContext ctx)`：
```csharp
public class VoiceConditionWorker_LowMood : VoiceConditionWorker
{
    public override bool Satisfied(VoiceContext ctx)
    {
        return ctx.pawn?.needs?.mood?.CurLevel < 0.3f;
    }
}
```
在 `VoiceLineDef` XML 中引用 `conditionWorkerClass="YourNamespace.VoiceConditionWorker_LowMood"` 即可，无需修改框架。

#### 为其他种族复用语音框架

1. 创建新的 `UnitVoicePackDef`（引用不同的 SoundDef/音频文件夹）
2. 在该种族的 `ThingDef` `<comps>` 中添加：
```xml
<li Class="StellarisDaughter.CompProperties_UnitVoice">
  <voicePack>YourMod_VoicePack_Whatever</voicePack>
</li>
```
所有 Harmony Patch 是通用的 — 检测 `CompUnitVoice` 是否存在，有则播放。

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

- [x] **RTS 单位语音框架** — VoiceEventDef + VoiceConditionWorker + VoiceLineDef + UnitVoicePackDef + CompUnitVoice
- [x] **Harmony Patches** — 选中/征召/移动/攻击 4 个 hook + 彩蛋连点
- [ ] **录制/合成语音音频** — 往 `Content/Sounds/Voice/SD_Daughter/` 各目录放入 .wav/.ogg 文件
- [ ] **ThoughtWorkers_SD.cs** — 5 个 Situation ThoughtWorker（HighAff/LowAff/HighTrs/LowTrs/Lonely）
- [ ] **SD_ThoughtDefs.xml** — 对应 5 条 ThoughtDef，带 `baseMoodEffect`，让心情面板反映养成状态
- [ ] 补充 5 个 ThoughtDef 语言键
- [ ] 替换占位贴图（童年体型、Thin体型、头部、FA 脸部）
- [ ] `AIEndingDef.ApplyRewards()` / `leavesColony` 逻辑完善
- [ ] `SD_AI_Childhood` Worker 中 `SD_Curious` 特质 Def 实现
- [ ] 清理旧占位语言键（SD_GizmoSync/Chaos/Awaken 等）
