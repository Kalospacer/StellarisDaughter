using System;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    /// <summary>
    /// 语音条目 Def — 将事件类型(VoiceEventDef) + 条件(VoiceConditionWorker) + 音效(SoundDef) 三者绑定。
    /// 在 UnitVoicePackDef 的 voiceLines 列表中引用。
    /// 按 priority 降序匹配，第一个满足条件的生效。
    /// </summary>
    public class VoiceLineDef : Def
    {
        /// <summary>响应哪个事件</summary>
        public VoiceEventDef eventDef;

        /// <summary>条件 Worker 类型（默认 VoiceConditionWorker_Always）</summary>
        public Type conditionWorkerClass = typeof(VoiceConditionWorker_Always);

        /// <summary>播放的随机语音 SoundDef（推荐使用 AudioGrain_Folder）</summary>
        public SoundDef sound;

        /// <summary>优先级（高值优先匹配，相同时先声明者优先）</summary>
        public int priority = 0;

        // ---- 懒加载 Worker 实例（标准 RimWorld Worker 模式） ----

        [Unsaved(false)]
        private VoiceConditionWorker workerInt;

        public VoiceConditionWorker ConditionWorker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (VoiceConditionWorker)Activator.CreateInstance(conditionWorkerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }
    }
}
