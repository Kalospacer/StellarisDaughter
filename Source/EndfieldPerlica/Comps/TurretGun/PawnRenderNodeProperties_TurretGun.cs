using Verse;

namespace EndfieldPerlica
{
    public class PawnRenderNodeProperties_TurretGun : PawnRenderNodeProperties
    {
        public bool combatDrone = false;

        public bool drawUndrafted = true;

        public bool isApparel = false;

        public bool alwaysDraw = false;

        public PawnRenderNodeProperties_TurretGun()
        {
            nodeClass = typeof(PawnRenderNode_TurretGun);
            workerClass = typeof(PawnRenderNodeWorker_TurretGun);
        }
    }
}
