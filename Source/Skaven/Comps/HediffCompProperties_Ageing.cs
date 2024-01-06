using Verse;

namespace TheEndTimes_Skaven
{
    public class HediffCompProperties_Ageing : HediffCompProperties
    {
        public int timetoage = 1;

        public HediffCompProperties_Ageing()
        {
            this.compClass = typeof(HediffComp_Ageing);
        }
    }
}