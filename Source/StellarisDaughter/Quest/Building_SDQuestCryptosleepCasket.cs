using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter;

public class Building_SDQuestCryptosleepCasket : Building_CryptosleepCasket
{
    public ThingDef pendingBeaconDef;

    private bool releaseTriggered;

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (!Spawned || Destroyed || !HasAnyContents)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "SD_Quest_AIDaughterCryocasket_OpenLabel".Translate().ToString(),
            defaultDesc = "SD_Quest_AIDaughterCryocasket_OpenDesc".Translate().ToString(),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject"),
            action = Open
        };
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref pendingBeaconDef, "pendingBeaconDef");
        Scribe_Values.Look(ref releaseTriggered, "releaseTriggered", defaultValue: false);
    }

    public override void EjectContents()
    {
        TriggerReleaseContents();
        base.EjectContents();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!releaseTriggered && HasAnyContents && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
        {
            TriggerReleaseContents();
            base.EjectContents();
        }

        base.Destroy(mode);
    }

    private void TriggerReleaseContents()
    {
        if (releaseTriggered)
        {
            return;
        }

        releaseTriggered = true;

        foreach (Thing thing in innerContainer)
        {
            if (thing is Pawn pawn && pawn.Faction != Faction.OfPlayer)
            {
                pawn.SetFaction(Faction.OfPlayer);
            }
        }

        if (pendingBeaconDef == null || Map == null)
        {
            return;
        }

        Thing beacon = ThingMaker.MakeThing(pendingBeaconDef);
        GenPlace.TryPlaceThing(beacon, Position, Map, ThingPlaceMode.Near, null, cell => cell != Position && cell != InteractionCell);
        pendingBeaconDef = null;
    }
}
