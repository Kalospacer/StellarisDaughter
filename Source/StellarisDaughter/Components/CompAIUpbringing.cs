using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// AI女儿养成系统核心组件
    /// 追踪同步率、混沌度、觉醒进度，驱动三阶段成长和双结局系统
    /// </summary>
    // ✨ 沐雪写的哦~
    public class CompAIUpbringing : ThingComp
    {
        #region 核心数值

        /// <summary> 同步率 - 情感羁绊程度（0-100） </summary>
        public float syncRate = 50f;

        /// <summary> 混沌度 - 觉醒扭曲程度（0-100） </summary>
        public float chaosLevel = 0f;

        /// <summary> 觉醒进度 - 整体觉醒程度（0-100），18年约达100 </summary>
        public float awakeningProgress = 0f;

        /// <summary> 锁定的结局路线 - 15岁时确定 </summary>
        public AIEndingRoute lockedEnding = AIEndingRoute.NotYetDetermined;

        #endregion

        #region 状态追踪

        /// <summary> 引导者 </summary>
        public Pawn mentor;

        /// <summary> 成长事件记录 </summary>
        public List<AIEventRecord> eventRecords = new List<AIEventRecord>();

        /// <summary> 上次引导者距离因子 </summary>
        private float lastMentorProximity = 0f;

        /// <summary> 累计与引导者互动ticks </summary>
        public int totalMentorInteractionTicks = 0;

        /// <summary> 是否已发送征兆信件 </summary>
        public bool omenLetterSent = false;

        #endregion

        #region 属性

        public CompProperties_AIUpbringing Props => (CompProperties_AIUpbringing)props;

        /// <summary> 当前倾向 </summary>
        public AITendency CurrentTendency
        {
            get
            {
                float diff = syncRate - chaosLevel;
                if (diff > 60f) return AITendency.Devoted;
                if (diff > 20f) return AITendency.Attached;
                if (diff < -60f) return AITendency.Corrupted;
                if (diff < -20f) return AITendency.Unstable;
                return AITendency.Neutral;
            }
        }

        /// <summary> 当前阶段描述 </summary>
        public string CurrentStageDescription
        {
            get
            {
                var ai = parent as Pawn;
                if (ai == null) return "";
                int age = ai.ageTracker.AgeBiologicalYears;
                if (age < 8) return "SD_Stage_Childhood".Translate();
                if (age < 15) return "SD_Stage_Youth".Translate();
                if (age < 18) return "SD_Stage_Adulthood".Translate();
                return "SD_Stage_Final".Translate();
            }
        }

        /// <summary> 当前数值偏向的结局 </summary>
        public AIEndingRoute CurrentLeaning =>
            syncRate > chaosLevel ? AIEndingRoute.FatherBond : AIEndingRoute.DarkCorruption;

        #endregion

        #region 生命周期

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && mentor == null)
            {
                InitializeMentor();
            }
        }

        private void InitializeMentor()
        {
            var ai = parent as Pawn;
            if (ai?.Faction == null) return;

            // 优先选择派系领袖
            var leader = ai.Faction.leader;
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

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (Current.ProgramState != ProgramState.Playing) return;

            var ai = parent as Pawn;
            if (ai == null) return;

            // 18岁后停止数值计算
            if (ai.ageTracker.AgeBiologicalYears >= 18) return;

            CalculateTickChange();
            CheckOmenLetter(ai);
        }

        #endregion

        #region 数值计算

        private void CalculateTickChange()
        {
            var ai = parent as Pawn;
            if (ai == null) return;

            float mentorProximity = CalculateMentorProximity();
            float environmentScore = CalculateEnvironmentScore();
            float emotionalState = CalculateEmotionalState(ai);
            float workActivity = CalculateWorkActivity(ai);

            // 同步率变化
            float syncChange = 0f;
            if (mentorProximity > 0)
            {
                syncChange += mentorProximity * 0.3f;
                totalMentorInteractionTicks += 250;
            }
            if (environmentScore > 0.7f) syncChange += 0.1f;
            if (emotionalState > 0.6f) syncChange += 0.1f;

            // 混沌度变化
            float chaosChange = 0f;
            if (mentorProximity == 0 && lastMentorProximity == 0) chaosChange += 0.2f;
            if (environmentScore < 0.3f) chaosChange += (0.3f - environmentScore) * 0.5f;
            if (emotionalState < 0.4f) chaosChange += (0.4f - emotionalState) * 0.3f;

            // 觉醒进度
            float awakeningChange = 0.05f;
            awakeningChange += workActivity * 0.1f;
            if (environmentScore > 0.8f) awakeningChange += 0.1f;

            // 应用
            syncRate = Mathf.Clamp(syncRate + syncChange, 0f, 100f);
            chaosLevel = Mathf.Clamp(chaosLevel + chaosChange, 0f, 100f);
            awakeningProgress = Mathf.Clamp(awakeningProgress + awakeningChange, 0f, 100f);
            lastMentorProximity = mentorProximity;
        }

        private void CheckOmenLetter(Pawn ai)
        {
            if (omenLetterSent) return;
            int age = ai.ageTracker.AgeBiologicalYears;
            if (age < 13 || age >= 15) return;

            omenLetterSent = true;

            string title = "SD_Letter_Omen_Title".Translate(ai.NameShortColored);
            string text = CurrentLeaning == AIEndingRoute.FatherBond
                ? "SD_Letter_Omen_Positive".Translate(ai.NameShortColored, mentor?.NameShortColored ?? "SD_Unknown".Translate())
                : "SD_Letter_Omen_Negative".Translate(ai.NameShortColored);

            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent, ai);
        }

        /// <summary> 15岁时锁定结局路线 </summary>
        public void LockEndingRoute()
        {
            if (lockedEnding != AIEndingRoute.NotYetDetermined) return;
            lockedEnding = CurrentLeaning;

            RecordEvent("Route_Locked",
                description: "结局路线锁定：" +
                    (lockedEnding == AIEndingRoute.FatherBond
                        ? "SD_Route_FatherBond".Translate().ToString()
                        : "SD_Route_DarkCorruption".Translate().ToString()));
        }

        private float CalculateMentorProximity()
        {
            if (mentor == null || mentor.Dead) return 0f;
            var ai = parent as Pawn;
            if (ai?.Map == null || mentor.Map != ai.Map) return 0f;

            float distance = ai.Position.DistanceTo(mentor.Position);
            if (distance > 15f) return 0f;
            return 1f - (distance / 15f);
        }

        private float CalculateEnvironmentScore()
        {
            var ai = parent as Pawn;
            if (ai?.Map == null) return 0.5f;

            Room room = ai.GetRoom();
            if (room == null) return 0.3f;

            float score = 0.5f;
            score += Mathf.Clamp(room.GetStat(RoomStatDefOf.Beauty) / 100f, -0.2f, 0.2f);
            score += Mathf.Clamp(room.GetStat(RoomStatDefOf.Cleanliness) / 10f, -0.1f, 0.1f);
            score += Mathf.Clamp(room.GetStat(RoomStatDefOf.Space) / 100f, 0f, 0.1f);
            return Mathf.Clamp01(score);
        }

        private float CalculateEmotionalState(Pawn ai)
        {
            float score = 0.5f;
            var moodNeed = ai.needs?.mood;
            if (moodNeed != null) score = moodNeed.CurLevel;
            float health = ai.health?.summaryHealth?.SummaryHealthPercent ?? 1f;
            return Mathf.Clamp01((score + health) / 2f);
        }

        private float CalculateWorkActivity(Pawn ai)
        {
            if (ai.CurJob == null) return 0f;
            if (ai.CurJob.def == JobDefOf.Research) return 1f;
            if (ai.CurJob.def == JobDefOf.DoBill) return 0.8f;
            return 0.3f;
        }

        #endregion

        #region 事件记录

        public void RecordEvent(string eventName, float syncChange = 0f, float chaosChange = 0f, string description = null)
        {
            eventRecords.Add(new AIEventRecord
            {
                eventDefName = eventName,
                tickOccurred = Find.TickManager.TicksGame,
                syncRateChange = syncChange,
                chaosLevelChange = chaosChange,
                description = description
            });

            if (syncChange != 0f) syncRate = Mathf.Clamp(syncRate + syncChange, 0f, 100f);
            if (chaosChange != 0f) chaosLevel = Mathf.Clamp(chaosLevel + chaosChange, 0f, 100f);

            Messages.Message(
                $"SD_Event_{eventName}".Translate(parent.LabelShort, syncChange, chaosChange),
                parent as Pawn, MessageTypeDefOf.NeutralEvent);
        }

        #endregion

        #region 存档

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref syncRate, "syncRate", 50f);
            Scribe_Values.Look(ref chaosLevel, "chaosLevel", 0f);
            Scribe_Values.Look(ref awakeningProgress, "awakeningProgress", 0f);
            Scribe_Values.Look(ref lockedEnding, "lockedEnding", AIEndingRoute.NotYetDetermined);
            Scribe_Values.Look(ref lastMentorProximity, "lastMentorProximity", 0f);
            Scribe_Values.Look(ref totalMentorInteractionTicks, "totalMentorInteractionTicks", 0);
            Scribe_Values.Look(ref omenLetterSent, "omenLetterSent", false);
            Scribe_References.Look(ref mentor, "mentor");
            Scribe_Collections.Look(ref eventRecords, "eventRecords", LookMode.Deep);
        }

        #endregion

        #region UI / Gizmos

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"SD_InspectSyncRate".Translate()}: {syncRate:F0}%");
            sb.AppendLine($"{"SD_InspectChaosLevel".Translate()}: {chaosLevel:F0}%");
            sb.AppendLine($"{"SD_InspectAwakening".Translate()}: {awakeningProgress:F0}%");
            sb.AppendLine($"{"SD_InspectStage".Translate()}: {CurrentStageDescription}");
            if (mentor != null)
                sb.AppendLine($"{"SD_InspectMentor".Translate()}: {mentor.NameShortColored}");
            if (lockedEnding != AIEndingRoute.NotYetDetermined)
            {
                string route = lockedEnding == AIEndingRoute.FatherBond
                    ? "SD_Route_FatherBond".Translate() : "SD_Route_DarkCorruption".Translate();
                sb.AppendLine($"{"SD_InspectRoute".Translate()}: {route}");
            }
            return sb.ToString().TrimEndNewlines();
        }

        #endregion

        #region 调试

        public string GetDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== AI女儿状态 ===");
            sb.AppendLine($"同步率: {syncRate:F1}%");
            sb.AppendLine($"混沌度: {chaosLevel:F1}%");
            sb.AppendLine($"觉醒进度: {awakeningProgress:F1}%");
            sb.AppendLine($"当前倾向: {CurrentTendency}");
            sb.AppendLine($"偏向: {CurrentLeaning}");
            sb.AppendLine($"锁定结局: {lockedEnding}");
            sb.AppendLine($"引导者: {mentor?.LabelShort ?? "无"}");
            sb.AppendLine($"征兆信件: {(omenLetterSent ? "已发送" : "未发送")}");
            return sb.ToString();
        }

        #endregion
    }
}
