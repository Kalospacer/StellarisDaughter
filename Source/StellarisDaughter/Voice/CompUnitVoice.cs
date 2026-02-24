using Verse;
using Verse.Sound;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 单位语音运行时组件 — 管理冷却、彩蛋连点检测。
    /// 所有事件走统一入口 TryPlay(eventDef, ctx)。
    /// 不序列化（运行时状态，保存/加载后重置即可）。
    /// </summary>
    public class CompUnitVoice : ThingComp
    {
        public CompProperties_UnitVoice Props => (CompProperties_UnitVoice)props;

        // ---- 运行时状态（不序列化） ----

        /// <summary>上次成功播放语音的 tick</summary>
        private int lastPlayTick = -99999;

        /// <summary>时间窗口内的选中点击计数（用于彩蛋检测）</summary>
        private int recentClickCount;

        /// <summary>连点窗口起始 tick</summary>
        private int firstClickTick;

        /// <summary>
        /// 统一语音播放入口。Harmony Patch 构造 VoiceContext 后调用此方法。
        /// </summary>
        /// <param name="eventDef">事件类型</param>
        /// <param name="ctx">事件上下文</param>
        public void TryPlay(VoiceEventDef eventDef, VoiceContext ctx)
        {
            var pack = Props?.voicePack;
            if (pack == null) return;

            int now = GenTicks.TicksGame;

            // ---- 彩蛋连点检测（仅 Selected 事件） ----
            bool isSpamClick = false;
            if (eventDef == SD_VoiceEventDefOf.SD_VoiceEvent_Selected)
            {
                float windowTicks = pack.spamClickWindowSeconds * 60f;
                if (now - firstClickTick > windowTicks)
                {
                    // 窗口过期，重置
                    recentClickCount = 0;
                    firstClickTick = now;
                }
                recentClickCount++;

                if (recentClickCount >= pack.spamClickCount)
                {
                    isSpamClick = true;
                    recentClickCount = 0;
                    firstClickTick = now;
                }
            }

            // ---- 冷却检查（彩蛋无视冷却） ----
            if (!isSpamClick)
            {
                float cooldownTicks = pack.cooldownSeconds * 60f;
                if (now - lastPlayTick < cooldownTicks)
                    return;
            }

            // ---- 解析并播放 ----
            var targetEvent = isSpamClick ? SD_VoiceEventDefOf.SD_VoiceEvent_SpamClick : eventDef;
            SoundDef sound = pack.Resolve(targetEvent, ctx);

            // 彩蛋未配置（或文件夹为空）时回退到原事件
            if ((sound == null || !HasResolvedGrains(sound)) && isSpamClick)
                sound = pack.Resolve(eventDef, ctx);

            if (sound == null || !HasResolvedGrains(sound)) return;

            sound.PlayOneShotOnCamera(ctx.map);
            lastPlayTick = now;
        }
        /// <summary>检查 SoundDef 是否至少有一个可解析的音频文件（文件夹非空）</summary>
        private static bool HasResolvedGrains(SoundDef sound)
        {
            if (sound?.subSounds == null) return false;
            foreach (var sub in sound.subSounds)
            {
                if (sub?.grains == null) continue;
                foreach (var grain in sub.grains)
                    foreach (var _ in grain.GetResolvedGrains())
                        return true;
            }
            return false;
        }
    }
}
