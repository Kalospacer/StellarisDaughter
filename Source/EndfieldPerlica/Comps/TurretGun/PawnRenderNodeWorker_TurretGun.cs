using UnityEngine;
using Verse;

namespace EndfieldPerlica
{
    public class PawnRenderNodeWorker_TurretGun : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
            {
                return false;
            }
            var turretNode = node as PawnRenderNode_TurretGun;
            var props = node.Props as PawnRenderNodeProperties_TurretGun;
            if (props != null)
            {
                if (props.alwaysDraw || (turretNode?.turretComp != null && turretNode.turretComp.CanShoot))
                {
                    return true;
                }
            }
            return false;
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Quaternion result = base.RotationFor(node, parms);
            var turretNode = node as PawnRenderNode_TurretGun;
            var comp = turretNode?.apparel?.TryGetComp<CompTurretGun>() ?? parms.pawn.TryGetComp<CompTurretGun>();
            
            if (comp != null && comp.PawnOwner != null && comp.PawnOwner.Spawned)
            {
                float facingRotation = parms.facing.AsAngle;
                float targetRotation = 0f;

                if (comp.currentTarget == LocalTargetInfo.Invalid || !comp.CanShoot)
                {
                    // Idle 状态：跟随 Pawn 朝向
                    targetRotation = facingRotation;
                }
                else
                {
                    // 战斗状态：指向目标
                    targetRotation = comp.curRotation;
                }

                // 计算相对于 Graphic_Multi 当前贴图方向的偏移量
                // 因为 Graphic_Multi 已经根据 parms.facing 选择了对应的贴图（0, 90, 180, 270）
                // 我们只需要旋转“差值”即可实现 360 度平滑指向
                float delta = targetRotation - facingRotation;
                return result * Quaternion.Euler(0, delta, 0);
            }
            return result;
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            var turretNode = node as PawnRenderNode_TurretGun;
            var comp = turretNode?.apparel?.TryGetComp<CompTurretGun>() ?? parms.pawn.TryGetComp<CompTurretGun>();

            if (comp != null)
            {
                Vector3 offset = new Vector3(comp.floatOffset_xAxis, 0f, comp.floatOffset_yAxis);
                return base.OffsetFor(node, parms, out pivot) + offset;
            }
            return base.OffsetFor(node, parms, out pivot);
        }
    }
}
