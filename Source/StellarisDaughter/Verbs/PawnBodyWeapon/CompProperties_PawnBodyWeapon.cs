using RimWorld;
using Verse;

namespace StellarisDaughter
{
    public class CompProperties_PawnBodyWeapon : CompProperties
    {
        public float cleaveAngle = 90f;
        public float cleaveRange = 2.5f;
        public float cleaveDamageFactor = 0.7f;
        public bool damageDowned;
        public DamageDef cleaveDamageDef;

        public EffecterDef attackEffecter;
        public EffecterDef cleaveEffecter;
        public SoundDef attackSound;

        public HediffDef applyHediffOnHit;
        public float hediffSeverity = 0.1f;
        public float hediffChance = 1f;

        public bool requiresMeleeSkill;
        public int minMeleeSkillLevel;
        public bool onlyWhenDrafted;

        public CompProperties_PawnBodyWeapon()
        {
            compClass = typeof(CompPawnBodyWeapon);
        }
    }

    public class CompPawnBodyWeapon : ThingComp
    {
        public CompProperties_PawnBodyWeapon Props => (CompProperties_PawnBodyWeapon)props;

        private Pawn Pawn => parent as Pawn;

        public bool CanUseBodyWeapon(Verb verb = null)
        {
            if (Pawn == null || Pawn.Dead || Pawn.Downed)
            {
                return false;
            }

            if (Props.onlyWhenDrafted && !Pawn.Drafted)
            {
                return false;
            }

            if (Props.requiresMeleeSkill && Pawn.skills != null)
            {
                var meleeSkill = Pawn.skills.GetSkill(SkillDefOf.Melee);
                if (meleeSkill.Level < Props.minMeleeSkillLevel)
                {
                    return false;
                }
            }

            return true;
        }

        public float GetDamageFactor(Verb verb = null)
        {
            var factor = Props.cleaveDamageFactor;
            if (Pawn.skills != null)
            {
                var meleeSkill = Pawn.skills.GetSkill(SkillDefOf.Melee);
                factor *= 1f + meleeSkill.Level * 0.02f;
            }

            return factor;
        }

        public float GetCleaveRange(Verb verb = null)
        {
            var range = Props.cleaveRange;
            if (Pawn.BodySize > 1f)
            {
                range *= Pawn.BodySize * 0.5f;
            }

            return range;
        }

        public float GetCleaveAngle(Verb verb = null)
        {
            return Props.cleaveAngle;
        }
    }
}
