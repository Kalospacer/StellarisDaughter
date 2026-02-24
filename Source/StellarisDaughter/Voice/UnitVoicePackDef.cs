using System.Collections.Generic;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 语音包 Def — 聚合一套完整的语音配置。
    /// 挂载在 CompProperties_UnitVoice 中被 ThingComp 引用。
    /// </summary>
    public class UnitVoicePackDef : Def
    {
        /// <summary>该语音包包含的所有语音条目</summary>
        public List<VoiceLineDef> voiceLines = new List<VoiceLineDef>();

        /// <summary>两句台词之间最短间隔（秒）</summary>
        public float cooldownSeconds = 2.5f;

        /// <summary>连点多少次触发彩蛋</summary>
        public int spamClickCount = 5;

        /// <summary>连点计数的时间窗口（秒）</summary>
        public float spamClickWindowSeconds = 3f;

        // ---- 缓存：按事件分组 ----

        [Unsaved(false)]
        private Dictionary<VoiceEventDef, List<VoiceLineDef>> cacheByEvent;

        /// <summary>临时列表，用于收集匹配的 SoundDef（避免每次分配）</summary>
        [Unsaved(false)]
        private List<SoundDef> tmpMatchedSounds = new List<SoundDef>();

        private void EnsureCache()
        {
            if (cacheByEvent != null) return;
            cacheByEvent = new Dictionary<VoiceEventDef, List<VoiceLineDef>>();
            foreach (var line in voiceLines)
            {
                if (line?.eventDef == null) continue;
                if (!cacheByEvent.TryGetValue(line.eventDef, out var list))
                {
                    list = new List<VoiceLineDef>();
                    cacheByEvent[line.eventDef] = list;
                }
                list.Add(line);
            }
        }

        /// <summary>
        /// 根据事件类型和上下文，收集所有满足条件的 SoundDef 到池中，随机选一个。
        /// base 语音（Always 条件）永远入池，addon 语音条件满足时叠加入池。
        /// 返回 null 表示无匹配。
        /// </summary>
        public SoundDef Resolve(VoiceEventDef eventDef, VoiceContext ctx)
        {
            EnsureCache();
            if (!cacheByEvent.TryGetValue(eventDef, out var lines)) return null;

            tmpMatchedSounds.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.sound != null && line.ConditionWorker.Satisfied(ctx))
                    tmpMatchedSounds.Add(line.sound);
            }

            if (tmpMatchedSounds.Count == 0) return null;
            return tmpMatchedSounds.RandomElement();
        }
    }
}
