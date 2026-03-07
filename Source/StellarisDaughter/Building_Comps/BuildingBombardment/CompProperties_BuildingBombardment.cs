using RimWorld;
using Verse;

namespace StellarisDaughter
{
    public class CompProperties_BuildingBombardment : CompProperties
    {
        public float radius = 15f;
        public bool targetEnemies = true;
        public bool targetNeutrals;
        public bool targetAnimals;
        public int burstCount = 3;
        public int innerBurstIntervalTicks = 10;
        public int burstIntervalTicks = 60;
        public float randomOffset = 2f;
        public ThingDef skyfallerDef;
        public ThingDef projectileDef;

        public CompProperties_BuildingBombardment()
        {
            compClass = typeof(CompBuildingBombardment);
        }
    }
}
