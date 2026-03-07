using RimWorld;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    public class CompAllSkillsMinorPassion : ThingComp
    {
        private bool _initialized;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            TryInitializeAllSkillsMinorPassion();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _initialized, "allSkillsMinorPassionInitialized", false);
        }

        private void TryInitializeAllSkillsMinorPassion()
        {
            if (_initialized) return;
            if (!(parent is Pawn pawn)) return;

            var skillTracker = pawn.skills;
            if (skillTracker?.skills == null) return;

            bool changed = false;
            for (int i = 0; i < skillTracker.skills.Count; i++)
            {
                SkillRecord skill = skillTracker.skills[i];
                if (skill == null || skill.TotallyDisabled) continue;
                if (skill.passion == Passion.Minor) continue;

                skill.passion = Passion.Minor;
                changed = true;
            }

            if (changed)
                skillTracker.DirtyAptitudes();

            _initialized = true;
        }
    }
}
