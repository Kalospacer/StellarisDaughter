using Verse;

namespace StellarisDaughter
{
    public class CompProperties_SD_ProjectileImpactEffect : CompProperties
    {
        public EffecterDef impactEffecter;

        public CompProperties_SD_ProjectileImpactEffect()
        {
            compClass = typeof(CompSD_ProjectileImpactEffect);
        }
    }

    public class CompSD_ProjectileImpactEffect : ThingComp
    {
        public CompProperties_SD_ProjectileImpactEffect Props => (CompProperties_SD_ProjectileImpactEffect)props;

        public void NotifyImpact()
        {
            if (Props.impactEffecter == null || parent?.MapHeld == null)
            {
                return;
            }

            Props.impactEffecter.Spawn().Trigger(new TargetInfo(parent.Position, parent.MapHeld), parent);
        }
    }
}
