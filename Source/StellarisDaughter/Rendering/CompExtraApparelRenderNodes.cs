using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// ThingComp that goes on Apparel ThingDefs.
    /// It simply holds the CompProperties_ExtraApparelRenderNodes data.
    /// The actual render node injection is done by DynamicPawnRenderNodeSetup_ExtraApparel.
    /// </summary>
    public class CompExtraApparelRenderNodes : ThingComp
    {
        public CompProperties_ExtraApparelRenderNodes Props =>
            (CompProperties_ExtraApparelRenderNodes)props;
    }
}
