# StellarisDaughter - 开发日志

## 项目状态：占位测试阶段（使用佩丽卡贴图）

---

## 当前架构

### 文件结构
```
StellarisDaughter/
├── 1.6/1.6/
│   ├── Assemblies/StellarisDaughter.dll       ✅ 已编译
│   ├── Defs/
│   │   ├── BodyTypeDefs/SD_BodyTypes.xml       ✅ 定义 SD_Youth 体型
│   │   ├── EndingDefs/                         ✅ AIEndingDef 结局系统
│   │   ├── HediffDefs/                         ✅
│   │   ├── LifeStageDefs/SD_LifeStages.xml     ✅ 四个生命阶段
│   │   ├── PawnKindDef/SD_PawnKinds.xml        ✅ 生成年龄 0~0（新生）
│   │   ├── ThingDef_Races/SD_AI_Daughter_Race.xml ✅
│   │   └── TraitDefs/
│   └── Languages/
├── 1.6/FA/Defs/                               ✅ Facial Animation 自定义 Defs
│   ├── AnimationDefs/
│   ├── FaceShapeDefs/
│   └── FaceTypeDefs/SD_AI_Daughter_Race/SD_FaceType.xml
├── Content/Textures/Endfield/Things/Perlica/  ✅ 占位贴图（佩丽卡）
│   ├── Bodies/
│   │   ├── Naked_Thin_*.png                   ← 成年体型
│   │   ├── Naked_Child_*.png                  ← Child 体型（Thin 副本）
│   │   ├── Naked_Baby_*.png                   ← Baby 体型（Thin 副本）
│   │   ├── Naked_Female_*.png                 ← Female 体型（Thin 副本）
│   │   └── Naked_SD_Youth_*.png               ← Youth 体型（Thin 副本，待换图）
│   ├── Heads/Female_AverageNormal_*.png
│   ├── Heads_Blank/Normal/Unisex/             ← FA 专用头部贴图
│   ├── Brows/, Eyes/, Lids/, Mouth/, Skins/   ← FA 表情组件贴图
│   └── Addons/Perlica_Feather_Ear             ← 耳饰 Addon
└── Source/StellarisDaughter/
    ├── Components/CompAIUpbringing.cs          ✅ 核心成长数据组件
    ├── LifeStageWorkers/
    │   ├── LifeStageWorker_AI_Childhood.cs    ✅ 初始化导师、赋予好奇特质
    │   ├── LifeStageWorker_AI_Youth.cs        ✅ 强制设 SD_Youth 体型
    │   ├── LifeStageWorker_AI_Adulthood.cs    ✅ 强制设 Thin 体型、锁定结局路线
    │   └── LifeStageWorker_AI_Final.cs        ✅ 触发最终结局
    └── ...
```

---

## 生命阶段系统

| 阶段 | 年龄 | developmentalStage | 体型 | 贴图 | 谁来赋值 |
|---|---|---|---|---|---|
| `SD_AI_Childhood` | 0–7 | `Child` | `Child` | `Naked_Child_*.png` | PawnGenerator 自动 |
| `SD_AI_Youth` | 8–14 | `Child` | `SD_Youth` | `Naked_SD_Youth_*.png` | LifeStageWorker 强制写入 |
| `SD_AI_Adulthood` | 15–17 | `Adult` | `Thin` | `Naked_Thin_*.png` | LifeStageWorker 强制写入 |
| `SD_AI_Final` | 18+ | `Adult` | `Thin` | `Naked_Thin_*.png` | 同上 |

- `silhouetteGraphicData`：Child/Youth → `Silhouette_HumanChild`，Adult/Final → `Silhouette_HumanAdult`
- HAR `bodyTypes` 只列 `[Thin]`，防止随机选出不该有的体型
- PawnKindDef `minGenerationAge: 0 / maxGenerationAge: 0`，每个女儿均以新生状态生成

---

## 关键设计决策

### 种族 SD_AI_Daughter_Race
- 基于佩丽卡（`EF_Perlica_Race`）结构移植
- `maleGenderProbability: 0`，全为女性
- `minAgeForAdulthood: 15`
- `growthFactorByAge`：0岁满速，12岁后减半
- 繁殖关闭（`gestatingGender: Male`，生育力全程为0）
- Facial Animation 组件通过 `MayRequire="Nals.FacialAnimation"` 条件加载

### LoadFolders.xml
```xml
<v1.6>
  <li>1.6/1.6</li>
  <li IfModActive="Nals.FacialAnimation">1.6/FA</li>
  <li>Content</li>
</v1.6>
```

### Facial Animation 集成
- `1.6/FA/Defs/FaceTypeDefs/SD_AI_Daughter_Race/SD_FaceType.xml` 定义了六个 TypeDef：
  - `BrowTypeDef`、`EyeballTypeDef`、`HeadTypeDef`、`LidTypeDef`、`MouthTypeDef`、`SkinTypeDef`
- 全部 `<raceName>SD_AI_Daughter_Race</raceName>`
- 贴图复用佩丽卡路径（占位）

---

## 已修复的 Bug 列表

1. **`ThingDefOf.ResearchBench` 编译报错** → 移除 `CompAIUpbringing.cs` 中的引用
2. **XML `<forcedHairDef>` 无效字段** → 从 PawnKindDef 删除
3. **`lifeStageAges` 升序校验失败** → 添加 `Inherit="False"`
4. **"Tried 300 times to generate age"** → 删除 `minGenerationAge/maxGenerationAge` 限制（已恢复为 0）
5. **Body NRE（Child/Baby 贴图缺失）** → 复制 Thin 贴图为 Child/Baby/Female 副本
6. **Head NRE（FA TypeDefs 缺失）** → 移植 Perlica FA Defs 并替换 raceName
7. **FA Defs 未加载（目录结构错误）** → 移动到正确的 `Defs/` 子目录
8. **`RenderPawnAt` NRE（CameraPlus silhouette）** → `bodyTypes` 移除多余体型
9. **`RenderPawnAt` NRE（silhouetteGraphicData 为 null）** → 四个 LifeStageDef 全部补充 `silhouetteGraphicData`
10. **刷出的女儿显示"完全觉醒"** → 生成年龄改为 0–0

---

## 待办

- [ ] 替换占位贴图为星规之女专属美术资源
  - `Naked_SD_Youth_*.png`（青年体型）
  - `Naked_Child_*.png`（童年体型）
  - 头部、FA 脸部贴图
- [ ] `SD_AI_Childhood` Worker 中 `SD_Curious` 特质 Def 尚需实现
- [ ] `AIEndingDef.ApplyRewards` / `leavesColony` 逻辑待完善
- [ ] 语言文本 Keys 补全（`SD_Letter_*`）
- [ ] `CompAIUpbringing` 的 `mentor` 选择逻辑在地图外生成时可能为 null
