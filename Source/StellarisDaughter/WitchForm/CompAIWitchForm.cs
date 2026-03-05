using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// 魔女化系统核心组件
    /// </summary>
    public class CompAIWitchForm : ThingComp
    {
        #region 核心数据

        /// <summary> 当前魔女因子值 </summary>
        public float witchFactor = 0f;

        /// <summary> 当前状态 </summary>
        public WitchFormState state = WitchFormState.Normal;

        #endregion

        #region 属性

        public CompProperties_AIWitchForm Props => (CompProperties_AIWitchForm)props;

        public Pawn Pawn => parent as Pawn;

        /// <summary> 当前是否在魔女形态（包括发狂） </summary>
        public bool IsWitchForm => state == WitchFormState.WitchForm || state == WitchFormState.Berserk;

        /// <summary> 是否处于发狂状态 </summary>
        public bool IsBerserk => state == WitchFormState.Berserk;

        /// <summary> 动态上限 </summary>
        public float MaxWitchFactor
        {
            get
            {
                var upbringing = Pawn?.GetComp<CompAIUpbringing>();
                float affectionBonus = upbringing?.affection ?? 0f;
                return Props.baseMaxFactor + affectionBonus * 0.1f;
            }
        }

        /// <summary> 半值点 </summary>
        public float HalfMaxFactor => MaxWitchFactor * 0.5f;

        #endregion

        #region 生命周期

        public override void CompTickRare()
        {
            base.CompTickRare();

            if (Current.ProgramState != ProgramState.Playing) return;
            if (Pawn == null) return;

            // 状态同步检查：如果精神状态结束但我们还在Berserk，自动恢复
            if (IsBerserk && !Pawn.InMentalState)
            {
                Log.Warning($"[StellarisDaughter] {Pawn.NameShortColored} Berserk state ended externally, resetting witch state");
                state = WitchFormState.Normal;
                witchFactor = 0f;
                SwitchAppearance(false);
            }

            // 计算增长
            CalculateGrowth();

            // 检查是否发狂
            CheckBerserk();
        }

        #endregion

        #region 魔女因子增长

        private void CalculateGrowth()
        {
            float growthRate = GetGrowthRate();
            if (growthRate <= 0f) return;

            float newFactor = witchFactor + growthRate;
            float max = MaxWitchFactor;

            // 未变身且低于50%时，只增长到50%
            if (!IsWitchForm && witchFactor < HalfMaxFactor && newFactor >= HalfMaxFactor)
            {
                newFactor = HalfMaxFactor;
            }

            // 上限限制
            witchFactor = Mathf.Clamp(newFactor, 0f, max);
        }

        private float GetGrowthRate()
        {
            float baseRate = Props.baseGrowthRate;

            // 信任值影响基础速度（负信任加快，正信任减慢）
            var upbringing = Pawn?.GetComp<CompAIUpbringing>();
            float trustModifier = upbringing?.trust ?? 0f;
            baseRate -= trustModifier * 0.001f;

            // 确保基础速度不为负
            baseRate = Mathf.Max(baseRate, 0f);

            // 变身状态快速增长
            if (IsWitchForm)
            {
                return baseRate * Props.witchFormGrowthMult;
            }

            // 超过50%后减慢
            if (witchFactor >= HalfMaxFactor)
            {
                return baseRate * Props.halfMaxGrowthMult;
            }

            return baseRate;
        }

        #endregion

        #region 发狂检测

        private void CheckBerserk()
        {
            if (IsBerserk) return;

            if (witchFactor >= MaxWitchFactor)
            {
                EnterBerserk();
            }
        }

        private void EnterBerserk()
        {
            // 先检查当前是否在魔女形态（在修改state之前）
            bool wasInWitchForm = (state == WitchFormState.WitchForm);

            // 设置发狂状态
            state = WitchFormState.Berserk;

            // 如果之前不在魔女形态，强制切换外观
            if (!wasInWitchForm)
            {
                SwitchAppearance(true);
            }

            // 触发发狂精神状态（攻击所有人）
            if (Pawn.mindState != null && !Pawn.InMentalState)
            {
                bool success = Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, forceWake: true);
                if (!success)
                {
                    Log.Warning($"[StellarisDaughter] Failed to start Berserk mental state for {Pawn.NameShortColored}");
                }
            }

            Messages.Message("SD_Witch_Berserk".Translate(Pawn.NameShortColored), Pawn, MessageTypeDefOf.NegativeEvent);
        }

        #endregion

        #region 形态切换

        /// <summary> 主动切换形态 </summary>
        public void ToggleWitchForm()
        {
            if (IsBerserk)
            {
                Messages.Message("SD_Witch_CannotToggle_Berserk".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (state == WitchFormState.Normal)
            {
                state = WitchFormState.WitchForm;
                SwitchAppearance(true);
            }
            else
            {
                state = WitchFormState.Normal;
                SwitchAppearance(false);
            }
        }

        /// <summary> 切换外观装备和头发 </summary>
        private void SwitchAppearance(bool toWitchForm)
        {
            if (Pawn?.apparel == null || Pawn.story == null) return;

            // 脱下非专属装备（丢到地上）
            DropNonExclusiveApparel();

            // 销毁当前专属衣服并穿上新的
            SwitchExclusiveApparel(toWitchForm);

            // 切换头发
            SwitchHair(toWitchForm);

            // 刷新渲染
            Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        private void DropNonExclusiveApparel()
        {
            var exclusiveDefs = new HashSet<ThingDef>
            {
                Props.normalApparelDef,
                Props.witchApparelDef
            };

            var apparelsToDrop = Pawn.apparel.WornApparel
                .Where(a => !exclusiveDefs.Contains(a.def))
                .ToList();

            foreach (var apparel in apparelsToDrop)
            {
                Pawn.apparel.TryDrop(apparel, out _, Pawn.Position);
            }
        }

        private void SwitchExclusiveApparel(bool toWitchForm)
        {
            // 销毁当前专属衣服
            var exclusiveDefs = new HashSet<ThingDef>
            {
                Props.normalApparelDef,
                Props.witchApparelDef
            };

            var currentExclusive = Pawn.apparel.WornApparel
                .FirstOrDefault(a => exclusiveDefs.Contains(a.def));

            if (currentExclusive != null)
            {
                Pawn.apparel.Remove(currentExclusive);
                currentExclusive.Destroy(DestroyMode.Vanish);
            }

            // 穿上新衣服
            var newApparelDef = toWitchForm ? Props.witchApparelDef : Props.normalApparelDef;
            if (newApparelDef != null)
            {
                var newApparel = ThingMaker.MakeThing(newApparelDef) as Apparel;
                if (newApparel != null)
                {
                    Pawn.apparel.Wear(newApparel, dropReplacedApparel: true, locked: true);
                }
            }
        }

        private void SwitchHair(bool toWitchForm)
        {
            var newHair = toWitchForm ? Props.witchHairDef : Props.normalHairDef;
            if (newHair != null)
            {
                Pawn.story.hairDef = newHair;
            }
        }

        #endregion

        #region 镇压系统

        /// <summary> 镇压发狂状态，重置因子 </summary>
        public void ApplySuppression()
        {
            if (!IsBerserk)
            {
                Messages.Message("SD_Witch_NotBerserk".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            // 结束发狂精神状态
            if (Pawn.InMentalState && Pawn.MentalStateDef == MentalStateDefOf.Berserk)
            {
                Pawn.MentalState.RecoverFromState();
            }

            // 重置因子
            witchFactor = 0f;

            // 恢复正常形态
            state = WitchFormState.Normal;
            SwitchAppearance(false);

            Messages.Message("SD_Witch_Suppressed".Translate(Pawn.NameShortColored), MessageTypeDefOf.PositiveEvent);
        }

        #endregion

        #region 存档

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref witchFactor, "witchFactor", 0f);
            Scribe_Values.Look(ref state, "state", WitchFormState.Normal);
        }

        #endregion

        #region Gizmos

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Gizmo_AIWitchForm(this);

            // 开发模式调试按钮
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: +100 因子",
                    defaultDesc = "增加100魔女因子用于测试",
                    action = () => witchFactor = Mathf.Min(witchFactor + 100f, MaxWitchFactor)
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: 重置因子",
                    defaultDesc = "重置魔女因子为0",
                    action = () => witchFactor = 0f
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: 强制发狂",
                    defaultDesc = "强制进入发狂状态",
                    action = () =>
                    {
                        witchFactor = MaxWitchFactor;
                        EnterBerserk();
                    }
                };
            }
        }

        #endregion

        #region 调试

        public string GetDebugInfo()
        {
            return $"=== 魔女化系统 ===\n" +
                   $"因子值: {witchFactor:F1} / {MaxWitchFactor:F1}\n" +
                   $"状态: {state}\n" +
                   $"好感影响: {(Pawn.GetComp<CompAIUpbringing>()?.affection ?? 0f) * 0.1f:F1}\n" +
                   $"信任影响: {(Pawn.GetComp<CompAIUpbringing>()?.trust ?? 0f) * 0.001f:F1}";
        }

        #endregion
    }
}
