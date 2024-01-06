using RimWorld;
using Verse;

namespace TheEndTimes_Skaven
{
    public class HediffCompProperties_SpawnOnDeath : HediffCompProperties
    {
        public int moteCount = 3;
        public FloatRange moteOffsetRange = new FloatRange(0.2f, 0.4f);
        public int filthCount = 4;
        public FleckDef fleck;
        public ThingDef filth;
        public SoundDef sound;
        public PawnKindDef pawnKind;
        public bool destroyBody;
        public FactionDef forcedFaction;
        public bool usePlayerFaction;

        public HediffCompProperties_SpawnOnDeath()
        {
            this.compClass = typeof(HediffComp_SpawnOnDeath);
        }
    }
}
