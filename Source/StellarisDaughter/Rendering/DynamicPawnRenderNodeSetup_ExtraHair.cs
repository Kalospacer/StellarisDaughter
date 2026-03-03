using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StellarisDaughter
{
    public class DynamicPawnRenderNodeSetup_ExtraHair : DynamicPawnRenderNodeSetup
    {
        public override bool HumanlikeOnly => true;

        public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
        {
            if (pawn.story == null || pawn.story.hairDef == null)
            {
                yield break;
            }

            HairDef hairDef = pawn.story.hairDef;
            DefModExtension_ExtraHairNodes ext = hairDef.GetModExtension<DefModExtension_ExtraHairNodes>();
            
            if (ext != null && ext.renderNodes != null)
            {
                foreach (PawnRenderNodeProperties nodeProps in ext.renderNodes)
                {
                    if (!tree.ShouldAddNodeToTree(nodeProps))
                        continue;

                    PawnRenderNode renderNode;
                    try
                    {
                        renderNode = (PawnRenderNode)Activator.CreateInstance(
                            nodeProps.nodeClass, pawn, nodeProps, tree);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"[StellarisDaughter] Failed to create extra render node " +
                            $"for hair {hairDef.defName}: {ex}");
                        continue;
                    }

                    yield return (renderNode, null);
                }
            }
        }
    }
}
