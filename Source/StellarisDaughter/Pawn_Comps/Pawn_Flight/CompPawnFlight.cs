using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// A marker component that holds custom flight properties.
    /// The actual flight logic is handled by Harmony patches that check for this component
    /// and use its properties to override or trigger vanilla flight behavior.
    /// </summary>
    public class CompPawnFlight : ThingComp
    {
        public bool flightEnabled = true;

        public CompProperties_PawnFlight Props => (CompProperties_PawnFlight)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref flightEnabled, "flightEnabled", true);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Props.showFlightToggle && parent is Pawn pawn && pawn.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "SD_FlightToggle".Translate(),
                    defaultDesc = "SD_FlightToggleDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/SD_FlightToggle", false)
                           ?? TexCommand.Draft,
                    isActive = () => flightEnabled,
                    toggleAction = () =>
                    {
                        flightEnabled = !flightEnabled;
                        // If disabling flight while flying, force land
                        if (!flightEnabled && pawn.flight != null && pawn.flight.Flying)
                        {
                            pawn.flight.ForceLand();
                            if (pawn.CurJob != null)
                                pawn.CurJob.flying = false;
                        }
                    },
                    hotKey = KeyBindingDefOf.Command_ColonistDraft,
                };
            }
        }
    }
}
