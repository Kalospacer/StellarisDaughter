using System.Collections.Generic;
using Verse;

namespace StellarisDaughter
{
    // 鉁?娌愰洩鍐欑殑鍝
    public class SD_DroneTypeDef : Def
    {
        public ThingDef droneDef;
        public List<VerbProperties> verbs;
        public int rechargeTicks = 180;
        public float orbitRadius = 1.8f;
        public float moveSpeed = 0.18f;
        public int deployTicks = 24;
    }
}
