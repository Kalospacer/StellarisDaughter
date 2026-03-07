using RimWorld;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Text;

namespace StellarisDaughter
{
    public class CompProperties_AircraftStrike : CompProperties_AbilityEffect
    {
        public ThingDef requiredAircraftType; // 需要的战机类型
        public int aircraftCooldownTicks = 60000; // 战机冷却时间（默认1天）
        public int aircraftsPerUse = 1; // 每次使用消耗的战机数量
        
        public CompProperties_AircraftStrike()
        {
            compClass = typeof(CompAbilityEffect_AircraftStrike);
        }
    }

    public class CompAbilityEffect_AircraftStrike : CompAbilityEffect
    {
        public new CompProperties_AircraftStrike Props => (CompProperties_AircraftStrike)props;
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            
            // 获取全局战机管理器
            WorldComponent_AircraftManager aircraftManager = Find.World.GetComponent<WorldComponent_AircraftManager>();
            
            if (aircraftManager == null)
            {
                Log.Message("SD_AircraftManagerNotFound".Translate());
                return;
            }

            // 检查并消耗战机
            if (aircraftManager.TryUseAircraft(Props.requiredAircraftType, Props.aircraftsPerUse, parent.pawn.Faction, Props.aircraftCooldownTicks))
            {
                // 成功消耗战机，发送消息
                Messages.Message("SD_AircraftStrikeInitiated".Translate(Props.requiredAircraftType.LabelCap), MessageTypeDefOf.PositiveEvent);
                Log.Message("SD_AircraftStrikeSuccess".Translate(Props.aircraftsPerUse, Props.requiredAircraftType.LabelCap));
            }
            else
            {
                Messages.Message("SD_NoAvailableAircraft".Translate(Props.requiredAircraftType.LabelCap), MessageTypeDefOf.NegativeEvent);
                Log.Message("SD_AircraftStrikeFailed".Translate(Props.requiredAircraftType.LabelCap, parent.pawn.Faction?.Name ?? "SD_UnknownFaction".Translate()));
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // 检查是否有可用的战机
            WorldComponent_AircraftManager aircraftManager = Find.World.GetComponent<WorldComponent_AircraftManager>();
            
            return base.CanApplyOn(target, dest) && 
                   aircraftManager != null && 
                   aircraftManager.HasAvailableAircraft(Props.requiredAircraftType, Props.aircraftsPerUse, parent.pawn.Faction);
        }

        // 关键修改：重写 GizmoDisabled 方法，在不满足条件时禁用按钮
        public override bool GizmoDisabled(out string reason)
        {
            // 先检查基础条件
            if (base.GizmoDisabled(out reason))
                return true;

            // 检查战机可用性
            WorldComponent_AircraftManager aircraftManager = Find.World.GetComponent<WorldComponent_AircraftManager>();
            if (aircraftManager == null)
            {
                reason = "SD_AircraftSystemNotReady".Translate();
                return true;
            }

            if (!aircraftManager.HasAvailableAircraft(Props.requiredAircraftType, Props.aircraftsPerUse, parent.pawn.Faction))
            {
                int available = aircraftManager.GetAvailableAircraftCount(Props.requiredAircraftType, parent.pawn.Faction);
                int total = aircraftManager.GetTotalAircraftCount(Props.requiredAircraftType, parent.pawn.Faction);
                
                if (available == 0 && total == 0)
                {
                    reason = "SD_NoAvailableAircraftType".Translate(Props.requiredAircraftType.LabelCap);
                }
                else if (available < Props.aircraftsPerUse)
                {
                    reason = "SD_AircraftInsufficient".Translate(Props.requiredAircraftType.LabelCap, available, Props.aircraftsPerUse);
                }
                else
                {
                    reason = "SD_AircraftOnCooldown".Translate(Props.requiredAircraftType.LabelCap);
                }
                return true;
            }

            return false;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            WorldComponent_AircraftManager aircraftManager = Find.World.GetComponent<WorldComponent_AircraftManager>();
            
            if (aircraftManager != null)
            {
                int available = aircraftManager.GetAvailableAircraftCount(Props.requiredAircraftType, parent.pawn.Faction);
                int onCooldown = aircraftManager.GetCooldownAircraftCount(Props.requiredAircraftType, parent.pawn.Faction);
                
                // 使用符号显示飞机状态
                string availableSymbols = GetAircraftSymbols(available, "◆");
                string cooldownSymbols = GetAircraftSymbols(onCooldown, "◇");
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SD_AvailableAircraft".Translate(availableSymbols));
                sb.AppendLine("SD_CooldownAircraft".Translate(cooldownSymbols));
                sb.Append("SD_CostPerUse".Translate(Props.aircraftsPerUse));
                
                return sb.ToString();
            }
            
            return base.ExtraLabelMouseAttachment(target);
        }

        // 生成飞机符号表示
        private string GetAircraftSymbols(int count, string symbol)
        {
            if (count <= 0) return "—";
            
            StringBuilder sb = new StringBuilder();
            int displayCount = count;
            
            if (count > 10)
            {
                return $"{count}{symbol}";
            }
            
            for (int i = 0; i < displayCount; i++)
            {
                sb.Append(symbol);
            }
            
            return sb.ToString();
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            // 检查战机可用性
            WorldComponent_AircraftManager aircraftManager = Find.World.GetComponent<WorldComponent_AircraftManager>();
            if (aircraftManager == null || !aircraftManager.HasAvailableAircraft(Props.requiredAircraftType, Props.aircraftsPerUse, parent.pawn.Faction))
            {
                if (throwMessages)
                {
                    Messages.Message("SD_NoAircraftForStrike".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return true;
        }

        // 鼠标悬停时的工具提示
        public override string ExtraTooltipPart()
        {
            WorldComponent_AircraftManager aircraftManager = Find.World.GetComponent<WorldComponent_AircraftManager>();
            
            if (aircraftManager != null)
            {
                int available = aircraftManager.GetAvailableAircraftCount(Props.requiredAircraftType, parent.pawn.Faction);
                int onCooldown = aircraftManager.GetCooldownAircraftCount(Props.requiredAircraftType, parent.pawn.Faction);
                int total = available + onCooldown;
                
                // 将冷却时间从 tick 转换为小时
                float cooldownHours = TicksToHours(Props.aircraftCooldownTicks);
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SD_AircraftStatus".Translate());
                sb.AppendLine("SD_TotalAircraft".Translate(total));
                sb.AppendLine("SD_ReadyAircraft".Translate(available));
                sb.AppendLine("SD_CooldownAircraftCount".Translate(onCooldown));
                sb.AppendLine("SD_AircraftRequirement".Translate(Props.requiredAircraftType.LabelCap, Props.aircraftsPerUse));
                sb.AppendLine("SD_CooldownTime".Translate(cooldownHours.ToString("F1")));

                return sb.ToString();
            }
            
            return base.ExtraTooltipPart();
        }

        // 将 tick 转换为小时
        private float TicksToHours(int ticks)
        {
            return ticks / 2500f;
        }
    }
}
