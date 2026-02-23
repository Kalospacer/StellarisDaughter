# EndfieldPerlica 开发日志与修复总结

## 最近更新: 2026-01-31

### ✅ 浮游炮系统 (TurretGun System) [新增]
既然您提到了需要像 Milira 那样的浮游炮，我们已经完全重写并实现了一套属于本模组的浮游炮系统。
- **无需任何前置模组**：完全独立实现，不依赖 AncotLibrary。
- **功能特性**：
  - **漂浮动画**：支持 X/Y 轴的正弦波漂浮效果，可在 XML 中配置振幅和速度。
  - **自动攻击**：装备在身上的 Apparel 可作为独立炮塔自动索敌攻击。
  - **渲染分离**：使用 1.5+ 的 `PawnRenderNode` 系统，确保浮游炮独立于 Pawn 身体渲染和旋转。
  - **参数优化**：将原 Ancot 的具歧义参数 `combatDrone` 重命名为 `fixedRotation`（是否固定旋转），更加直观。
  - **Gizmo控制**：提供开关控制是否自动射击。
- **代码文件**：位于 `Source/EndfieldPerlica/Comps/TurretGun/`。

### ✅ 技能修复: 协议ω·雷击
- **视觉升级**：修改了闪电材质代码，现在闪电呈现**高亮金黄色**。
- **机制修正**：伤害类型回归原版 `Flame`（火焰），利用原版机制保证稳定性。
- **文案更新**：技能名称变更为“协议ω·雷击”，描述匹配设定图。

### ✅ 项目架构升级
- **Git 初始化**：项目现已纳入 Git 版本控制。
- **依赖管理**：通过 `Libs` 文件夹管理 Harmony/HAR/FA 等依赖，输出目录更干净。
- **编译修复**：解决了所有编译错误，现在可以一键编译。

---

## 📖 如何使用浮游炮系统 (XML配置指南)

在您的 `ThingDef` (Apparel) 中添加组件：

```xml
<ThingDef ParentName="ApparelBase">
  <!-- ...基础属性... -->
  <comps>
    <li Class="EndfieldPerlica.CompProperties_TurretGun">
      <turretDef>Your_Turret_Def_Name</turretDef>
      <float_yAxis>
        <floatAmplitude>0.15</floatAmplitude>
        <floatSpeed>0.08</floatSpeed> <!-- 调整速度 -->
      </float_yAxis>
    </li>
  </comps>
  <apparel>
    <renderNodeProperties>
      <li Class="EndfieldPerlica.PawnRenderNodeProperties_TurretGun">
        <texPath>Path/To/Your/Texture</texPath>
        <isApparel>true</isApparel>
        <drawData>
          <dataSouth><layer>-1</layer></dataSouth>
        </drawData>
      </li>
    </renderNodeProperties>
  </apparel>
</ThingDef>
```
