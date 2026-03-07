using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// CompAIUpbringing：被动积累模块
    /// 每 TickRare 根据孤独/需求不足施加惩罚，不主动给予正向增长（增长只靠事件驱动）。
    /// </summary>
    public partial class CompAIUpbringing
    {
        // ── 孤独阈值 ──────────────────────────────────────────────────
        private const int AloneThreshold1 = 6 * 2500;   // 6 游戏小时
        private const int AloneThreshold2 = 1 * 60000;  // 1 游戏天
        private const int AloneThreshold3 = 3 * 60000;  // 3 游戏天

        // ── 孤独 / 陪伴 ───────────────────────────────────────────────

        /// <summary>
        /// 检测 15 格内是否有自由殖民者。
        /// 有人陪伴 → 重置计数器并给予小额正增益；
        /// 无人陪伴 → 计数器累加，超过阈值施加分级惩罚。
        /// </summary>
        private void TickPassiveLoneliness(Pawn ai)
        {
            bool hasNearby = false;
            if (ai.Map != null)
            {
                foreach (var col in ai.Map.mapPawns.FreeColonists)
                {
                    if (col == ai || col.Dead || !col.Spawned) continue;
                    if (ai.Position.DistanceTo(col.Position) <= 15f) { hasNearby = true; break; }
                }
            }

            if (hasNearby)
            {
                _ticksAloneCounter = 0;
                // 有人陪伴时仅重置计数器，不自然增长
            }
            else
            {
                _ticksAloneCounter += 250; // CompTickRare ≈ 250 tick/次
                float aff = 0f, trs = 0f;

                if      (_ticksAloneCounter >= AloneThreshold3) { aff = -0.30f; trs = -0.35f; }
                else if (_ticksAloneCounter >= AloneThreshold2) { aff = -0.15f; trs = -0.20f; }
                else if (_ticksAloneCounter >= AloneThreshold1) { aff = -0.05f; trs = -0.08f; }

                if (aff != 0f || trs != 0f)
                {
                    Apply(aff, trs, null);
                    passiveLonelyAff += aff;
                    passiveLonelyTrs += trs;
                }
            }
        }

        // ── 基础需求 & 环境 ───────────────────────────────────────────

        /// <summary>
        /// 根据食物/休息/舒适度/娱乐需求水平和当前居室美感给予信任细水长流增益。
        /// </summary>
        private void TickPassiveNeeds(Pawn ai)
        {
            if (ai.needs == null) return;
            float trs = 0f;

            var food    = ai.needs.food;
            var rest    = ai.needs.rest;
            var comfort = ai.needs.AllNeeds.Find(n => n.def.defName == "Comfort");
            var joy     = ai.needs.joy;

            // 只计算低于基准（0.5）的惩罚，满足需求不自然增长
            if (food    != null && food.CurLevel    < 0.5f) trs += (food.CurLevel    - 0.5f) * 0.04f;
            if (rest    != null && rest.CurLevel    < 0.5f) trs += (rest.CurLevel    - 0.5f) * 0.03f;
            if (comfort != null && comfort.CurLevel < 0.5f) trs += (comfort.CurLevel - 0.5f) * 0.02f;
            if (joy     != null && joy.CurLevel     < 0.5f) trs += (joy.CurLevel     - 0.5f) * 0.02f;

            if (trs < 0f)
            {
                Apply(0f, trs, null);
                passiveNeedsTrs += trs;
            }

            // 居室美感：只惩罚，不奖励
            Room room = ai.GetRoom();
            if (room != null)
            {
                float envTrs = room.GetStat(RoomStatDefOf.Beauty) * 0.002f;
                if (envTrs < 0f)
                {
                    Apply(0f, envTrs, null);
                    passiveEnvTrs += envTrs;
                }
            }
        }
    }
}
