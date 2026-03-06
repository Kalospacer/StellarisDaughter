using System.Collections.Generic;
using Verse;

namespace StellarisDaughter
{
    public class SD_DroneTypeDef : Def
    {
        public ThingDef droneDef;
        public List<VerbProperties> verbs;
        public int rechargeTicks = 180;
        public float orbitRadius = 1.8f;
        public float moveSpeed = 0.18f;
        public int deployTicks = 24;
        public float preferredTargetDistance = 4f;
        public float targetDistanceJitter = 0.6f;
        public int preAttackAimTicks = 0;
        public int postAttackWaitTicks = 18;
        public int maxAttackCycles = -1;
        public float returnSpeedMultiplier = 1.25f;
        public float launchForwardDistance = 0.9f;
        public float launchSideOffset = 0.45f;
    }
}
