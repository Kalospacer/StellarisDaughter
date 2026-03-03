using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// Custom DynamicPawnRenderNodeSetup that scans worn apparel for
    /// CompExtraApparelRenderNodes and injects all defined render nodes
    /// into the pawn's render tree.
    /// 
    /// This runs AFTER the vanilla apparel setup so wornGraphicPath
    /// is already handled normally, and we only add extra nodes.
    /// </summary>
    public class DynamicPawnRenderNodeSetup_ExtraApparel : DynamicPawnRenderNodeSetup
    {
        public override bool HumanlikeOnly => true;

        // Ensure this runs after the vanilla apparel setup
        public override List<Type> SetupAfter => new List<Type>
        {
            typeof(DynamicPawnRenderNodeSetup_Apparel)
        };

        public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(
            Pawn pawn, PawnRenderTree tree)
        {
            if (pawn.apparel == null || pawn.apparel.WornApparelCount == 0)
                yield break;

            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompExtraApparelRenderNodes>();
                if (comp?.Props?.renderNodes == null)
                    continue;

                foreach (var nodeProps in comp.Props.renderNodes)
                {
                    if (!tree.ShouldAddNodeToTree(nodeProps))
                        continue;

                    PawnRenderNode renderNode;
                    try
                    {
                        // Use 3-arg constructor (PawnRenderNode default)
                        // since these are extra overlay nodes, not standard apparel nodes
                        renderNode = (PawnRenderNode)Activator.CreateInstance(
                            nodeProps.nodeClass, pawn, nodeProps, tree);
                        renderNode.apparel = apparel;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"[StellarisDaughter] Failed to create extra render node " +
                            $"for apparel {apparel.def.defName}: {ex}");
                        continue;
                    }

                    // parent is null => PawnRenderTree.AddChild will use parentTagDef to find parent
                    yield return (renderNode, null);
                }
            }
        }
    }
}
