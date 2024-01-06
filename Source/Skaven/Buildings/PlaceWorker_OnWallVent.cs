using Verse;

namespace TheEndTimes_Skaven
{
    public class PlaceWorker_OnWallVent : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
          BuildableDef checkingDef,
          IntVec3 loc,
          Rot4 rot,
          Map map,
          Thing thingToIgnore = null,
          Thing thing = null)
        {
            Building edifice = loc.GetEdifice(map);
            if (edifice == null)
                return (AcceptanceReport)"RH_TET_Skaven_WallNeeded".Translate();
            if (edifice.def == null)
                return (AcceptanceReport)"RH_TET_Skaven_WallNeeded".Translate();
            if (edifice.def.IsSmoothed)
                return AcceptanceReport.WasAccepted;
            if (edifice.def.graphicData == null)
                return (AcceptanceReport)"RH_TET_Skaven_WallNeeded".Translate();
            if ((edifice.def.graphicData.linkFlags & LinkFlags.Wall) == LinkFlags.None)
                return (AcceptanceReport)"RH_TET_Skaven_WallNeeded".Translate();
            return AcceptanceReport.WasAccepted;
        }
    }
}
