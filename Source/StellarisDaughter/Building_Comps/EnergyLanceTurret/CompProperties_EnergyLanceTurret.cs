using RimWorld;
using Verse;

namespace StellarisDaughter
{
    public class CompProperties_EnergyLanceTurret : CompProperties
    {
        public ThingDef energyLanceDef;
        public int energyLanceDuration = 600;
        public float energyLanceMoveDistance = 15f;
        public float detectionRange = 30f;
        public bool targetHostileFactions = true;
        public bool targetNeutrals;
        public bool targetAnimals;
        public bool targetMechs = true;
        public bool requireLineOfSight;
        public int targetUpdateInterval = 60;
        public float targetSwitchRange = 25f;
        public int warmupTicks = 30;
        public int cooldownTicks = 120;

        public CompProperties_EnergyLanceTurret()
        {
            compClass = typeof(CompEnergyLanceTurret);
        }
    }
}
