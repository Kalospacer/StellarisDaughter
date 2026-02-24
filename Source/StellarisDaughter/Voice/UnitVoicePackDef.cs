using System.Collections.Generic;
using System.Linq;
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

        // ---- 缓存：按事件分组并按 priority 降序排列 ----

        [Unsaved(false)]
        private Dictionary<VoiceEventDef, List<VoiceLineDef>> cacheByEvent;

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
            // 每组按 priority 降序排列
            foreach (var list in cacheByEvent.Values)
                list.Sort((a, b) => b.priority.CompareTo(a.priority));
        }

        /// <summary>
        /// 根据事件类型和上下文，返回第一个满足条件的 SoundDef。
        /// 返回 null 表示无匹配。
        /// </summary>
        public SoundDef Resolve(VoiceEventDef eventDef, VoiceContext ctx)
        {
            EnsureCache();
            if (!cacheByEvent.TryGetValue(eventDef, out var lines)) return null;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.ConditionWorker.Satisfied(ctx))
                    return line.sound;
            }
            return null;
        }
    }
}
