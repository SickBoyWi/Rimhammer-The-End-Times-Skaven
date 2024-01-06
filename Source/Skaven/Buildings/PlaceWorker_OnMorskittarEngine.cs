using RimWorld;
using System.Collections.Generic;
using Verse;

namespace TheEndTimes_Skaven
{
    public class PlaceWorker_OnMorskittarEngine : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
          BuildableDef checkingDef,
          IntVec3 loc,
          Rot4 rot,
          Map map,
          Thing thingToIgnore = null,
          Thing thing = null)
        {
            for (int index1 = 0; index1 < 4; ++index1)
            {
                IntVec3 c = loc + GenAdj.CardinalDirections[index1];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int index2 = 0; index2 < thingList.Count; ++index2)
                    {
                        ThingDef thingDef = GenConstruct.BuiltDefOf(thingList[index2].def) as ThingDef;
                        if (thingDef != null && thingDef.building != null && thingDef.defName != null && thingDef.Equals(SkavenDefOf.RH_TET_Skaven_Morskittar_Cannon))
                            return (AcceptanceReport)true;
                    }
                }
            }
            return (AcceptanceReport)"RH_TET_Skaven_MustPlaceOnMorskittar".Translate();
        }
    }
}
