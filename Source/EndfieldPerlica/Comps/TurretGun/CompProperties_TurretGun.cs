using System.Collections.Generic;
using RimWorld;
using Verse;

namespace EndfieldPerlica
{
    public class CompProperties_TurretGun : CompProperties
    {
        public ThingDef turretDef;
        public float angleOffset;
        public bool autoAttack = true;
        public bool attackUndrafted = true;
        public int consumeChargeAmountPerShot = 0;
        public FloatGraph float_yAxis;
        public FloatGraph float_xAxis;
        public List<PawnRenderNodeProperties> renderNodeProperties;
        public string saveKeysPrefix;
        public string statLabelPostfix;
        public string gizmoLabel;
        public string gizmoDesc;
        public string gizmoIconPath;

        public CompProperties_TurretGun()
        {
            compClass = typeof(CompTurretGun);
        }
    }
}
