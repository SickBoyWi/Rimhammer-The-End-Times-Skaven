using Verse;

namespace TheEndTimes_Skaven
{
    public class HediffComp_Ageing : HediffComp
    {
        public HediffCompProperties_Ageing Props
        {
            get
            {
                return (HediffCompProperties_Ageing)this.props;
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            this.DebugMake1YearOlder(this.parent.pawn, 3L);
        }


        private void DebugMake1YearOlder(Pawn pawn, long years)
        {
            pawn.ageTracker.AgeBiologicalTicks = pawn.ageTracker.AgeBiologicalTicks + (3600000L * years);
            pawn.ageTracker.BirthAbsTicks = pawn.ageTracker.BirthAbsTicks - (3600000L * years);
        }
    }
}
