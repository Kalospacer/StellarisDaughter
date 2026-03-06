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
        public float minOwnerDistance = -1f;
        public float maxOwnerDistance = -1f;
        public float minTargetDistance = -1f;
        public float maxTargetDistance = -1f;
        public float preferredTargetDistance = 4f;
        public float targetDistanceJitter = 0.6f;
        public float squadronSpreadRadius = 0.65f;
        public string trailTexturePath = "StellarisDaughter/SRACompat/Projectile/ChargeLanceShot";
        public float trailWidth = 0.28f;
        public int trailMaxSegments = 7;
        public float trailPointSpacing = 0.18f;
        public float trailMinMovePerTick = 0.02f;
        public int preAttackAimTicks = 0;
        public int postAttackWaitTicks = 18;
        public int loiterTicksWithoutTarget = 1200;
        public int maxAttackCycles = -1;
        public float returnSpeedMultiplier = 1.25f;
        public float launchForwardDistance = 0.9f;
        public float launchSideOffset = 0.45f;
    }
}
