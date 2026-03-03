using System.Collections.Generic;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// CompProperties that holds a list of extra PawnRenderNodeProperties
    /// for apparel. These render nodes are added in ADDITION to the normal
    /// wornGraphicPath rendering, bypassing the vanilla limitation where
    /// apparel.renderNodeProperties replaces the default rendering and
    /// only uses the first entry.
    /// </summary>
    public class CompProperties_ExtraApparelRenderNodes : CompProperties
    {
        public List<PawnRenderNodeProperties> renderNodes;

        public CompProperties_ExtraApparelRenderNodes()
        {
            compClass = typeof(CompExtraApparelRenderNodes);
        }
    }
}
