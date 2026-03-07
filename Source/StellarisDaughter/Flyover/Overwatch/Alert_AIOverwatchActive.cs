using RimWorld;
using Verse;

namespace StellarisDaughter
{
    public class Alert_AIOverwatchActive : Alert_Critical
    {
        public Alert_AIOverwatchActive()
        {
            defaultLabel = "SD_AIOverwatch_Label".Translate();
            defaultExplanation = "SD_AIOverwatch_Desc".Translate(0);
        }

        public override AlertReport GetReport()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return AlertReport.Inactive;
            }

            MapComponent_AIOverwatch comp = map.GetComponent<MapComponent_AIOverwatch>();
            return comp != null && comp.IsEnabled ? AlertReport.Active : AlertReport.Inactive;
        }

        public override string GetLabel()
        {
            Map map = Find.CurrentMap;
            MapComponent_AIOverwatch comp = map?.GetComponent<MapComponent_AIOverwatch>();
            if (comp != null && comp.IsEnabled)
            {
                return "SD_AIOverwatch_Label".Translate() + $"\n({comp.DurationTicks / 60}s)";
            }

            return "SD_AIOverwatch_Label".Translate();
        }

        public override TaggedString GetExplanation()
        {
            Map map = Find.CurrentMap;
            MapComponent_AIOverwatch comp = map?.GetComponent<MapComponent_AIOverwatch>();
            if (comp != null && comp.IsEnabled)
            {
                return "SD_AIOverwatch_Desc".Translate(comp.DurationTicks / 60);
            }

            return base.GetExplanation();
        }
    }
}
