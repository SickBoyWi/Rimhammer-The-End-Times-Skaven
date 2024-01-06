using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace TheEndTimes_Skaven
{
    public class GenStep_WarpstoneMeteor : GenStep_Scatterer
    {
        public ThingSetMakerDef thingSetMakerDef;
        private const int Size = 8;

        public override int SeedPart
        {
            get
            {
                return 913432291;
            }
        }

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            if (!base.CanScatterAt(c, map) || !c.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy) || !map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false, false, false)))
                return false;
            CellRect rect = CellRect.CenteredOn(c, Size, Size);
            List<CellRect> var;
            if (MapGenerator.TryGetVar<List<CellRect>>("UsedRects", out var) && var.Any<CellRect>((Predicate<CellRect>)(x => x.Overlaps(rect))))
                return false;
            foreach (IntVec3 c1 in rect)
            {
                if (!c1.InBounds(map) || c1.GetEdifice(map) != null)
                    return false;
            }
            return true;
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
        {
            CellRect var1 = CellRect.CenteredOn(loc, Size, Size).ClipInsideMap(map);
            List<CellRect> var2;
            if (!MapGenerator.TryGetVar<List<CellRect>>("UsedRects", out var2))
            {
                var2 = new List<CellRect>();
                MapGenerator.SetVar<List<CellRect>>("UsedRects", var2);
            }
            ResolveParams resolveParams = new ResolveParams();
            resolveParams.rect = var1;
            resolveParams.faction = map.ParentFaction;

            // Gen meteor here.
            foreach (IntVec3 cell in var1.Cells)
            {
                if (IsValidCellInRect(var1, cell))
                {
                    List<Thing> thingsInCell = cell.GetThingList(map);
                    if (thingsInCell != null && thingsInCell.Count > 0)
                    {
                        List<Thing> thingsToDestroy = new List<Thing>();
                        foreach (Thing t in cell.GetThingList(map))
                        {
                            try
                            {
                                thingsToDestroy.Add(t);
                            }
                            catch { }
                        }

                        foreach (Thing t in thingsToDestroy)
                        { 
                            if (t != null)
                                t.Destroy();
                        }
                    }

                    int rand = RH_TET_SkavenMod.random.Next(0, 10);
                    if (rand < 7)
                        GenSpawn.Spawn(SkavenDefOf.RH_TET_Skaven_MineableWarpstone, cell, map);
                    else if (rand < 9)
                        GenSpawn.Spawn(ThingDefOf.MineableSteel, cell, map);
                    else
                        GenSpawn.Spawn(ThingDefOf.MineableGold, cell, map);
                }
            }
        }

        private bool IsValidCellInRect(CellRect var1, IntVec3 cell)
        {
            int xmin = var1.minX;
            int zmin = var1.minZ;
            int xmax = var1.maxX;
            int zmax = var1.maxZ;

            if ((cell.x == xmin && (cell.z == zmax || cell.z == zmax - 1 || cell.z == zmin || cell.z == zmin + 1))
                || (cell.x == xmin + 1 && (cell.z == zmax || cell.z == zmin))
                || (cell.x == xmax && (cell.z == zmax || cell.z == zmax - 1 || cell.z == zmin || cell.z == zmin + 1))
                || (cell.x == xmax - 1 && (cell.z == zmax || cell.z == zmin)))
                return false;

            return true;
        }
    }
}
