using Verse;

namespace StellarisDaughter
{
    /// <summary>
    /// Legacy AI growth event record.
    /// Kept for compatibility with older save data and adjacent systems.
    /// </summary>
    public class AIEventRecord : IExposable
    {
        public string eventDefName;
        public int tickOccurred;
        public float syncRateChange;
        public float chaosLevelChange;
        public string description;

        public void ExposeData()
        {
            Scribe_Values.Look(ref eventDefName, "eventDefName");
            Scribe_Values.Look(ref tickOccurred, "tickOccurred");
            Scribe_Values.Look(ref syncRateChange, "syncRateChange");
            Scribe_Values.Look(ref chaosLevelChange, "chaosLevelChange");
            Scribe_Values.Look(ref description, "description");
        }

        public int DayOccurred => tickOccurred / 60000;
        public int YearOccurred => tickOccurred / 3600000;
    }
}
