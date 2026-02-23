using System.Collections.Generic;
using RimWorld;
using Verse;

namespace EndfieldPerlica
{
    public class PawnRenderNode_TurretGun : PawnRenderNode_Apparel
    {
        public CompTurretGun turretComp;

        public PawnRenderNode_TurretGun(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree, null)
        {
            useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
            meshSet = MeshSetFor(pawn);
        }

        public PawnRenderNode_TurretGun(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
            : base(pawn, props, tree, apparel)
        {
            base.apparel = apparel;
            useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
            meshSet = MeshSetFor(pawn);
        }

        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            var turretGunProps = Props as PawnRenderNodeProperties_TurretGun;
            if (turretGunProps != null && turretComp == null)
            {
                turretComp = turretGunProps.isApparel ? apparel?.TryGetComp<CompTurretGun>() : pawn.TryGetComp<CompTurretGun>();
            }

            if (Props.texPath != null)
            {
                yield return GraphicDatabase.Get<Graphic_Multi>(Props.texPath, ShaderDatabase.Cutout);
            }
            else if (turretComp?.Props?.turretDef?.graphicData != null)
            {
                yield return GraphicDatabase.Get<Graphic_Single>(turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.Cutout);
            }
        }
    }
}
