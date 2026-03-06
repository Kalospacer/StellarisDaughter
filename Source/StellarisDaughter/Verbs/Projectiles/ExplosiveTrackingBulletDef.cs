using RimWorld;
using Verse;

namespace StellarisDaughter
{
    public class ExplosiveTrackingBulletDef : DefModExtension
    {
        public float explosionRadius = 1.9f;
        public DamageDef damageDef;
        public int explosionDelay = 0;
        public SoundDef soundExplode;
        public FleckDef preExplosionFlash;
        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance = 0f;
        public int postExplosionSpawnThingCount = 1;
        public GasType? gasType;
        public ThingDef postExplosionSpawnThingDefWater;
        public ThingDef preExplosionSpawnThingDef;
        public float preExplosionSpawnChance = 0f;
        public int preExplosionSpawnThingCount = 0;
        public float screenShakeFactor = 1f;
        public bool applyDamageToExplosionCellsNeighbors = false;
        public bool doExplosionDamageAfterThingDestroyed = false;
        public float preExplosionSpawnMinMeleeThreat = -1f;
        public float explosionChanceToStartFire = 0f;
        public bool explosionDamageFalloff = false;
        public bool doExplosionVFX = true;
    }
}
