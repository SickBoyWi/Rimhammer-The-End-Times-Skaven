using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RimWorld;
using RimWorld.Planet;
using TheEndTimes_Magic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheEndTimes_Skaven
{
    public static class ModProps
    {
        public static string main = "TheEndTimes";
        public static string mod = "Skaven";
    }

    public static class SkavenUtil
    {
        private static List<Building> closedSet = new List<Building>();
        private static List<Building> openSet = new List<Building>();
        private static Dictionary<ThingDef, int> requiredParts;

        public static string Prefix => ModProps.main + " :: " + ModProps.mod + " :: ";
        public static void ErrorReport(string x) => Log.Error(Prefix + x);

        public static void DebugReport(string x)
        {
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                Log.Message(Prefix + x);
            }
        }

        public static bool IsSkaven(Pawn p)
        {
            if (p == null) 
                return false;
            if (p.RaceProps.Humanlike && (p.def.race.body.defName.Equals("RH_TET_Skaven_RatOgre") || p.def.race.body.defName.Equals("RH_TET_Skaven_RatOgreBland")
                 || p.def.race.body.defName.Equals("RH_TET_Skaven_SkavenHorned") || p.def.race.body.defName.Equals("RH_TET_Skaven_Skaven")))
            {
                return true;
            }
            return false;
        }

        public static bool IsMagicUser(Pawn pawn)
        {
            if (pawn == null)
            {
                SkavenUtil.DebugReport("IsMagicUser :: Pawn Null Exception");
                return false;
            }

            if (pawn.needs == null)
            {
                SkavenUtil.DebugReport("IsMagicUser :: Pawn Needs Null Exception");
                return false;
            }

            if (pawn.kindDef.defName.Equals("RH_TET_Skaven_WizardScenario")
                || pawn.kindDef.defName.Equals("RH_TET_Skaven_WizardStandard")
                || pawn.kindDef.defName.Equals("RH_TET_Skaven_WizardGreat"))
            {
                CompMagicUser compMagicUser = pawn.TryGetComp<CompMagicUser>();
                if (compMagicUser != null && compMagicUser.IsMagicUser)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindSpawnSpot(Map map, out IntVec3 spawnSpot)
        {
            return CellFinder.TryFindRandomEdgeCellWith((Predicate<IntVec3>)(c =>
            {
                if (map.reachability.CanReachColony(c))
                    return !c.Fogged(map);
                return false;
            }), map, CellFinder.EdgeRoadChance_Neutral, out spawnSpot);
        }

        public static bool TryFindSpawnCellForItem(CellRect rect, Map map, out IntVec3 result)
        {
            return CellFinder.TryFindRandomCellInsideWith(rect, (Predicate<IntVec3>)(c =>
            {
                if (c.GetFirstItem(map) != null)
                    return false;
                if (!c.Standable(map))
                {
                    switch (c.GetSurfaceType(map))
                    {
                        case SurfaceType.Item:
                        case SurfaceType.Eat:
                            break;
                        default:
                            return false;
                    }
                }
                return true;
            }), out result);
        }

        public static void SpawnWandererJoin(Building_ScreamingBell screamingBell)
        {
            Map map = screamingBell.Map;
            IncidentParms incidentParams = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, (IIncidentTarget)map);
            IntVec3 spawnSpot;

            if (!TryFindSpawnSpot(map, out spawnSpot))
                return;

            incidentParams.spawnCenter = spawnSpot;

            Find.Storyteller.incidentQueue.Add(new QueuedIncident(new FiringIncident(SkavenDefOf.RH_TET_Skaven_WandererJoin, (StorytellerComp)null, incidentParams), Find.TickManager.TicksGame + 500, 0));
        }

        public static void SpawnItemNearBuilding(Building_ScreamingBell bell, ThingDef thingDef, int stackCount = 1)
        {
            IntVec3 spawnCell2;
            CellRect cellRectAroundHerdstone2 = new CellRect(bell.InteractionCell.x - 4, bell.InteractionCell.z - 4, 10, 12);
            TryFindSpawnCellForItem(cellRectAroundHerdstone2, bell.Map, out spawnCell2);

            Thing thing = ThingMaker.MakeThing(thingDef);
            thing.stackCount = stackCount;
            GenSpawn.Spawn(thing, spawnCell2, bell.Map);
        }

        public static void SpawnRaidEvent(Building_ScreamingBell screamingBell, FloatRange strengthRange)
        {
            Map map = screamingBell.Map;
            Faction enemyFac;
            IntVec3 spawnSpot;

            if (!TryFindSpawnSpot(map, out spawnSpot) || !TryFindEnemyFaction(out enemyFac))
                return;

            int num = Rand.Int;
            float raidStrengthFactor = strengthRange.RandomInRange;
            IncidentCategoryDef cat = IncidentCategoryDefOf.ThreatSmall;
            if (raidStrengthFactor >= 1)
            {
                cat = IncidentCategoryDefOf.ThreatBig;
            }

            IncidentParms incidentParams = StorytellerUtility.DefaultParmsNow(cat, (IIncidentTarget)map);
            incidentParams.forced = true;
            incidentParams.faction = enemyFac;
            incidentParams.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            incidentParams.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            incidentParams.spawnCenter = spawnSpot;
            incidentParams.points = Mathf.Max(StorytellerUtility.DefaultThreatPointsNow(screamingBell.Map ?? (IIncidentTarget)Find.World) * raidStrengthFactor, enemyFac.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
            incidentParams.pawnGroupMakerSeed = new int?(num);

            PawnGroupMakerParms pawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, incidentParams, false);
            pawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(pawnGroupMakerParms.points, incidentParams.raidArrivalMode, incidentParams.raidStrategy, pawnGroupMakerParms.faction, PawnGroupKindDefOf.Combat);

            Find.Storyteller.incidentQueue.Add(new QueuedIncident(new FiringIncident(IncidentDefOf.RaidEnemy, (StorytellerComp)null, incidentParams), Find.TickManager.TicksGame + 1000, 0));
        }

        private static bool TryFindEnemyFaction(out Faction enemyFac)
        {
            return Find.FactionManager.AllFactions.Where<Faction>((Func<Faction, bool>)(f =>
            {
                if (!f.def.hidden && !f.defeated)
                    return f.HostileTo(Faction.OfPlayer);
                return false;
            })).TryRandomElement<Faction>(out enemyFac);
        }

        public static void GivePawnsHediff(List<Pawn> pawns, HediffDef hediff)
        {
            foreach(Pawn pawn in pawns)
            { 
                if (pawn != null && pawn.health != null)
                    pawn.health.AddHediff(hediff, (BodyPartRecord)null, new DamageInfo?(), (DamageWorker.DamageResult)null);
            }
        }

        internal static void GivePawnsInspiration(List<Pawn> pawns, InspirationDef inspDef)
        {
            foreach (Pawn pawn in pawns)
                pawn.mindState.inspirationHandler.TryStartInspiration(inspDef, null, false);
        }

        internal static void GivePawnsThought(List<Pawn> pawns, ThoughtDef thoughtDef)
        {
            foreach (Pawn pawn in pawns)
                pawn.needs.mood.thoughts.memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(thoughtDef), (Pawn)null);
        }

        internal static void SpawnRatSwarm(Faction faction, IntVec3 position, Map map, int pawnSpawnCount)
        {
            for (int i = 0; i < pawnSpawnCount; i++)
            {
                float biologicalAge = (float)RH_TET_MagicMod.random.Next(3, 6);

                PawnGenerationRequest pawnRequest = new PawnGenerationRequest(RH_TET_MagicDefOf.RH_TET_Rat_Medium, faction, PawnGenerationContext.PlayerStarter, -1,
                    true, false,
                    false, false, true,
                    0f, false, true, true,
                    false, false, false,
                    false, false, false,
                    0.0f, 0.0f, null, 0.0f,
                    null, null, null,
                    null, null, biologicalAge,
                    biologicalAge, null, null,
                    null, null, null);

                Pawn newPawn = (Pawn)PawnGenerator.GeneratePawn(pawnRequest);

                try
                {
                    //// Fully train? It makes this marginally more powerful, but rats cannot really do much.
                    //int trainableCount = 0;
                    //List<TrainableDef> trainableDefsInListOrder = TrainableUtility.TrainableDefsInListOrder;
                    //for (int j = 0; j < trainableDefsInListOrder.Count; ++j)
                    //{
                    //    bool visible = false;
                    //    AcceptanceReport ar = newPawn.training.CanAssignToTrain(trainableDefsInListOrder[j], out visible);
                    //    if (ar.Accepted)
                    //    {
                    //        trainableCount++;
                    //        newPawn.training.SetWantedRecursive(trainableDefsInListOrder[j], true);
                    //    }
                    //}

                    //switch (trainableCount)
                    //{
                    //    case 1:
                    //        trainableCount = 0;
                    //        break;
                    //    case 2:
                    //        trainableCount = 3;
                    //        break;
                    //    case 3:
                    //        trainableCount = 5;
                    //        break;
                    //    case 4:
                    //        trainableCount = 7;
                    //        break;
                    //    case 5:
                    //        trainableCount = 14;
                    //        break;
                    //    default:
                    //        break;
                    //}

                    //if (trainableCount > 0)
                    //{
                    //    for (int k = 0; k < trainableCount; k++)
                    //    {
                    //        TrainableDef train = newPawn.training.NextTrainableToTrain();
                    //        newPawn.training.Train(train, casterPawn, false);
                    //    }
                    //}

                    //RelationsUtility.TryDevelopBondRelation(casterPawn, newPawn, 1f);

                    GenSpawn.Spawn((Thing)newPawn, position, map, WipeMode.Vanish);
                }
                catch
                {
                    Log.Warning("Skaven Util call to SpawnRatSwarm failed.");
                }
            }
        }

        public static IEnumerable<string> ShootFailReasons(Building rootBuilding)
        {
            List<Building> cannonParts = SkavenUtil.CannonBuildingsAttachedTo(rootBuilding).ToList<Building>();
            foreach (KeyValuePair<ThingDef, int> requiredPart in SkavenUtil.RequiredParts())
            {
                KeyValuePair<ThingDef, int> partDef = requiredPart;
                int num = cannonParts.Count<Building>((Func<Building, bool>)(pa => pa.def == partDef.Key));
                if (num < partDef.Value)
                    yield return string.Format("{0}: {1}x {2} ({3} {4})", (object)"ShipReportMissingPart".Translate(), (object)(partDef.Value - num), (object)partDef.Key.label, (object)"ShipReportMissingPartRequires".Translate(), (object)partDef.Value);
            }
            foreach (Building thing in cannonParts)
            {
                CompHibernatable comp = thing.TryGetComp<CompHibernatable>();
                if (comp != null && comp.State == HibernatableStateDefOf.Hibernating)
                    yield return string.Format("{0}: {1}", (object)"ShipReportHibernating".Translate(), (object)thing.LabelCap);
                else if (comp != null && !comp.Running)
                    yield return string.Format("{0}: {1}", (object)"ShipReportNotReady".Translate(), (object)thing.LabelCap);
            }
        }

        public static List<Building> CannonBuildingsAttachedTo(Building root, bool includeSupports = true)
        {
            SkavenUtil.closedSet.Clear();
            if (root == null || root.Destroyed)
                return SkavenUtil.closedSet;
            SkavenUtil.openSet.Clear();
            SkavenUtil.openSet.Add(root);
            while (SkavenUtil.openSet.Count > 0)
            {
                Building open = SkavenUtil.openSet[SkavenUtil.openSet.Count - 1];
                SkavenUtil.openSet.Remove(open);
                SkavenUtil.closedSet.Add(open);
                foreach (IntVec3 c in GenAdj.CellsAdjacentCardinal((Thing)open))
                {
                    Building edifice = c.GetEdifice(open.Map);
                    if (edifice != null && IsCannonBuilding(edifice.def.defName) && ((includeSupports) || (!includeSupports && !edifice.def.defName.Equals("RH_TET_Skaven_Morskittar_Support"))) && (!SkavenUtil.closedSet.Contains(edifice) && !SkavenUtil.openSet.Contains(edifice)))
                        SkavenUtil.openSet.Add(edifice);
                }
            }
            return SkavenUtil.closedSet;
        }

        private static bool IsCannonBuilding (String defName)
        {
            if (defName != null)
            {
                if (defName.Contains("Skaven_Morskittar"))
                    return true;
            }

            return false;
        }
        public static Dictionary<ThingDef, int> RequiredParts()
        {
            if (SkavenUtil.requiredParts == null)
            {
                SkavenUtil.requiredParts = new Dictionary<ThingDef, int>();
                SkavenUtil.requiredParts[SkavenDefOf.RH_TET_Skaven_Morskittar_Cannon] = 1;
                SkavenUtil.requiredParts[SkavenDefOf.RH_TET_Skaven_Morskittar_Support] = 4;
                SkavenUtil.requiredParts[SkavenDefOf.RH_TET_Skaven_Morskittar_Control] = 1;
                SkavenUtil.requiredParts[SkavenDefOf.RH_TET_Skaven_Morskittar_Battery] = 7;
            }
            return SkavenUtil.requiredParts;
        }
        public static void SpawnRaidEvent(Building_MorskittarControl morskittar, FloatRange strengthRange)
        {
            Map map = morskittar.Map;
            Faction enemyFac;
            IntVec3 spawnSpot;

            if (!TryFindSpawnSpot(map, out spawnSpot) || !TryFindEnemyFaction(out enemyFac))
                return;

            int num = Rand.Int;
            float raidStrengthFactor = strengthRange.RandomInRange;
            IncidentCategoryDef cat = IncidentCategoryDefOf.ThreatSmall;
            if (raidStrengthFactor >= 1)
            {
                cat = IncidentCategoryDefOf.ThreatBig;
            }

            IncidentParms incidentParams = StorytellerUtility.DefaultParmsNow(cat, (IIncidentTarget)map);
            incidentParams.forced = true;
            incidentParams.faction = enemyFac;
            incidentParams.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            incidentParams.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            incidentParams.spawnCenter = spawnSpot;
            incidentParams.points = Mathf.Max(StorytellerUtility.DefaultThreatPointsNow(morskittar.Map ?? (IIncidentTarget)Find.World) * raidStrengthFactor, enemyFac.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
            incidentParams.pawnGroupMakerSeed = new int?(num);

            PawnGroupMakerParms pawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, incidentParams, false);
            pawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(pawnGroupMakerParms.points, incidentParams.raidArrivalMode, incidentParams.raidStrategy, pawnGroupMakerParms.faction, PawnGroupKindDefOf.Combat);

            Find.Storyteller.incidentQueue.Add(new QueuedIncident(new FiringIncident(IncidentDefOf.RaidEnemy, (StorytellerComp)null, incidentParams), Find.TickManager.TicksGame + 1000, 0));
        }
    }
}