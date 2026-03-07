using Verse;

namespace StellarisDaughter
{
    public class CompProperties_SkyfallerImpactEffect : CompProperties
    {
        public EffecterDef impactEffecter;
        public int impactEffectLifetimeTicks;

        public CompProperties_SkyfallerImpactEffect()
        {
            compClass = typeof(CompSkyfallerImpactEffect);
        }
    }

    public class CompSkyfallerImpactEffect : ThingComp
    {
        public CompProperties_SkyfallerImpactEffect Props => (CompProperties_SkyfallerImpactEffect)props;

        public void NotifyImpact()
        {
            if (Props.impactEffecter == null || parent?.MapHeld == null)
            {
                return;
            }

            Effecter effecter = Props.impactEffecter.Spawn();
            IntVec3 cell = parent.PositionHeld;
            TargetInfo target = new TargetInfo(cell, parent.MapHeld);

            if (Props.impactEffectLifetimeTicks > 0)
            {
                effecter.Trigger(target, parent);
                parent.MapHeld.effecterMaintainer.AddEffecterToMaintain(effecter, cell, Props.impactEffectLifetimeTicks);
                return;
            }

            effecter.Trigger(target, parent);
            effecter.Cleanup();
        }
    }
}
