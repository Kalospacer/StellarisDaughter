using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace StellarisDaughter;

public class QuestNode_Root_SDAIDaughterCryocasket : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return QuestGen_Get.GetMap() != null || Find.AnyPlayerHomeMap != null;
    }

    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;

        quest.Signal(quest.InitiateSignal, delegate
        {
            Map map = QuestGen_Get.GetMap() ?? Find.AnyPlayerHomeMap;
            if (map == null)
            {
                QuestGen_End.End(quest, QuestEndOutcome.Fail);
                return;
            }

            Building_SDQuestCryptosleepCasket casket = (Building_SDQuestCryptosleepCasket)ThingMaker.MakeThing(SD_DefOf.SD_Quest_AIDaughterCryptosleepCasket);
            Pawn daughter = PawnGenerator.GeneratePawn(SD_DefOf.SD_AI_Daughter, Faction.OfAncients);
            daughter.SetFaction(Faction.OfAncients);
            casket.pendingBeaconDef = SD_DefOf.SD_Fake_Spear_Of_Galaxy_Zenith_Beacon_Building;
            casket.TryAcceptThing(daughter, allowSpecialEffects: false);

            quest.DropPods(
                map.Parent,
                new List<Thing> { casket },
                customLetterLabel: "SD_Quest_AIDaughterCryocasket_DropLetterLabel".Translate().ToString(),
                customLetterText: "SD_Quest_AIDaughterCryocasket_DropLetterText".Translate().ToString(),
                sendStandardLetter: true,
                dropAllInSamePod: true);

            QuestGen_End.End(quest, QuestEndOutcome.Success);
        });
    }
}
