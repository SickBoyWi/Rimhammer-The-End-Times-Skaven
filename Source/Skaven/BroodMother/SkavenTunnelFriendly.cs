using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using TheEndTimes_Magic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheEndTimes_Skaven
{
    public class SkavenTunnelFriendly : ThingWithComps, ILoadReferenceable
    {
        public const int pawnSpawnRadius = 2;
        public const float searchRadius = 5f;
        public int nextTickEvent = Find.TickManager.TicksGame + new IntRange(180000, 300000).RandomInRange; // 3 to 5 days.

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (this.Faction != null)
                return;
            if (map.ParentFaction != null)
                this.SetFaction(map.ParentFaction, (Pawn)null);
        }

        public override void Tick()
        {
            base.Tick();
            if (!this.Spawned || this.Position.Fogged(this.Map))
                return;
            if (this.Faction == null || this.Faction != Faction.OfPlayer)
                return;

            if (Find.TickManager.TicksGame >= nextTickEvent)
            {
                DoTunnelHappening();

                nextTickEvent = Find.TickManager.TicksGame + new IntRange(180000, 300000).RandomInRange;
            }
        }

        private void DoTunnelHappening()
        {
            // Get all cells in radius
            Map thisMap = this.Map;
            IEnumerable<IntVec3> cellsAround = GenRadial.RadialCellsAround(this.Position, SkavenTunnelFriendly.searchRadius, true);
            Room room = this.GetRoom(RegionType.Set_Passable);
            float adjustAmount = 0f;
            foreach (IntVec3 nextCell in cellsAround)
            {
                if (room.ContainsCell(nextCell) && nextCell.GetThingList(thisMap) != null)
                {
                    foreach (Thing thing in nextCell.GetThingList(thisMap))
                    {
                        // count warpstone stuff
                        if (thing.def != null && thing.def.defName != null && thing.def.defName.Contains("Warpstone"))
                        {
                            float val = thing.MarketValue;
                            if (val > 0)
                            {
                                if (thing.stackCount > 1)
                                {
                                    val += thing.stackCount * thing.MarketValue;
                                }
                            }

                            adjustAmount += val;
                        }
                    }
                }
            }

            adjustAmount *= .25f;

            // count food (unique food? as lure?)
            // Make good stuff happen at higher numbers of the next random, and increase the random number based on those counts.

            // spawn goodies
            // steal goodies
            // spawn joiner
            // spawn raid from tunnel? Like infestation, claim tunnel, spawn enemies, maybe more tunnels, defend.

            int rando = RH_TET_SkavenMod.random.Next(0, 1000);
            int randoSave = rando;
            rando += (int)adjustAmount;

            if (randoSave < 30 ||  rando < 200) // Always a chance of something bad.
            {
                this.DoRaid(thisMap);
            }
            if (randoSave < 60 || rando < 350) // Always a chance of something bad.
            {
                this.DoMadSkaven(thisMap);
            }
            else if (randoSave < 80 || rando < 500) // Always a chance of something bad.
            {
                this.DoGasCloud(thisMap);
            }
            else if (rando < 700)
            {
                // slave joins

                PawnKindDef pawnKind = SkavenDefOf.RH_TET_Skaven_SlaveRatMelee;
                this.GenerateAndSpawnPawn(pawnKind, thisMap);
            }
            else if (rando < 800)
            {
                // clan rat joins
                PawnKindDef pawnKind = SkavenDefOf.RH_TET_Skaven_ClanratLow;
                this.GenerateAndSpawnPawn(pawnKind, thisMap);
            }
            else if (rando < 925)
            {
                // stormvermin joins
                PawnKindDef pawnKind = SkavenDefOf.RH_TET_Skaven_ClanratHigh;
                this.GenerateAndSpawnPawn(pawnKind, thisMap);
            }
            else
            {
                int seerCount = 0;
                List<Pawn> pawns = PawnsFinder.AllMaps;
                foreach (Pawn candidate in pawns)
                {
                    if (SkavenUtil.IsMagicUser(candidate) && !candidate.Dead)
                    {
                        seerCount++;
                    }
                }

                int rand1 = RH_TET_SkavenMod.random.Next(0,5);
                int rand2 = RH_TET_SkavenMod.random.Next(0, 20);

                if (seerCount < 2 || (seerCount < 3 && rand1 == 0) || (seerCount > 3 && rand2 == 0))
                { 
                    // wizard joins
                    PawnKindDef pawnKind = SkavenDefOf.RH_TET_Skaven_WizardScenario;
                    this.GenerateAndSpawnPawn(pawnKind, thisMap);
                }
                else
                {
                    // 45/50 stormvermin or assassin.
                    if (rand2 < 10)
                    {
                        // assassin joins
                        PawnKindDef pawnKind = SkavenDefOf.RH_TET_Skaven_Assassin;
                        this.GenerateAndSpawnPawn(pawnKind, thisMap);
                    }
                    else
                    {
                        // stormvermin joins
                        PawnKindDef pawnKind = SkavenDefOf.RH_TET_Skaven_ClanratHigh;
                        this.GenerateAndSpawnPawn(pawnKind, thisMap);
                    }
                }
            }
        }

        private void GenerateAndSpawnPawn(PawnKindDef pawnKind, Map thisMap)
        {

            Faction ofPlayer = Faction.OfPlayer;
            PawnGenerationRequest request = new PawnGenerationRequest(pawnKind, ofPlayer, PawnGenerationContext.NonPlayer, -1,
                true, false,
                false, true, true,
                0.05f, true, true, true,
                false, false, false,
                false, false, false,
                0.0f, 0.0f, null, 0.0f,
                null, null, null,
                null, null, null,
                null, Gender.Male, null,
                null, null, null);

            Pawn pawn = PawnGenerator.GeneratePawn(request);

            try
            {
                pawn.ideo.SetIdeo(Faction.OfPlayer.ideos.PrimaryIdeo);
            }
            catch
            {
                // Ignore if no ideos present.
            }

            //  Spawn
            GenSpawn.Spawn(pawn, this.Position, thisMap, WipeMode.Vanish);

            TaggedString text = "RH_TET_Skaven_TunnelPawnJoin".Translate();
            TaggedString label = "RH_TET_Skaven_TunnelPawnJoinLabel".Translate();
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawn);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, pawn, null, null);
        }

        private void DoGasCloud(Map thisMap)
        {
            Thing thing = ThingMaker.MakeThing(RH_TET_MagicDefOf.RH_TET_PoisonGas, (ThingDef)null);
            GenPlace.TryPlaceThing(thing, this.Position, thisMap, ThingPlaceMode.Direct, (Action<Verse.Thing, int>)null, (Predicate<IntVec3>)null, new Rot4());
            (thing as GasCloud).ReceiveConcentration(80000);

            // Send Message.
            Find.LetterStack.ReceiveLetter("RH_TET_Skaven_TunnelGasAttackLabel".Translate(),
                    "RH_TET_Skaven_TunnelGasAttack".Translate(),
                    LetterDefOf.NegativeEvent,
                    (LookTargets)this,
                    null,
                    null,
                    null,
                    null);
        }

        private void DoRaid(Map thisMap)
        {
            Faction f = Find.FactionManager.FirstFactionOfDef(SkavenDefOf.RH_TET_Skaven_UnderEmpireFaction);

            if (!f.HostileTo(Faction.OfPlayer))
            {
                int currentGoodwill = f.GoodwillWith(Faction.OfPlayer);

                if (currentGoodwill > 0)
                {
                    currentGoodwill *= -1;
                    currentGoodwill += -100;
                }
                else if (currentGoodwill < 0)
                {
                    currentGoodwill *= -1;
                    currentGoodwill = -100 + currentGoodwill;
                }
                else
                    currentGoodwill = -100;

                f.TryAffectGoodwillWith(Faction.OfPlayer, currentGoodwill, true, true, null);
            }

            // Create pawn group.
            IncidentParms parms = new IncidentParms();
            parms.points = StorytellerUtility.DefaultThreatPointsNow(this.Map);

            parms.faction = f;
            PawnGroupKindDef combat = PawnGroupKindDefOf.Combat;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
            parms.target = this.Map;
            parms.spawnCenter = this.Position;
            float points = parms.points;
            parms.points = IncidentWorker_Raid.AdjustedRaidPoints(parms.points, parms.raidArrivalMode, parms.raidStrategy, parms.faction, combat);

            PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms();
            pawnGroupMakerParms.tile = thisMap.Tile;
            pawnGroupMakerParms.faction = f;
            pawnGroupMakerParms.points = points;
            pawnGroupMakerParms.groupKind = PawnGroupKindDefOf.Combat;

            IEnumerable<Pawn> pawns = PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms);

            if (!pawns.Any<Pawn>())
            {
                Log.Warning("No pawns created for skaven under empire tunnel raid.");
                return;
            }
            else
            {
                Map target = (Map)parms.target;
                foreach (Pawn p in pawns)
                {
                    GenSpawn.Spawn(p, parms.spawnCenter, target, parms.spawnRotation, WipeMode.Vanish, false);
                }
            }

            List<TargetInfo> targetInfoList = new List<TargetInfo>();
            if (pawns.Any<Pawn>())
            {
                foreach (Pawn pawn in pawns)
                    targetInfoList.Add((TargetInfo)(Thing)pawn);
            }

            // Send Message.
            Find.LetterStack.ReceiveLetter("RH_TET_Skaven_UnderEmpireTunnelRaidLabel".Translate(),
                    "RH_TET_Skaven_UnderEmpireTunnelRaid".Translate(),
                    LetterDefOf.NegativeEvent,
                    (LookTargets)targetInfoList,
                    f,
                    null,
                    null,
                    null);

            if (parms.controllerPawn == null || parms.controllerPawn.Faction != Faction.OfPlayer)
            {
                Map target = (Map)parms.target;
                List<List<Pawn>> pawnListList = IncidentParmsUtility.SplitIntoGroups(pawns.ToList(), parms.pawnGroups);
                int raidSeed = Rand.Int;
                for (int index = 0; index < pawnListList.Count; ++index)
                {
                    List<Pawn> pawns1 = pawnListList[index];
                    Lord lord = LordMaker.MakeNewLord(parms.faction, this.MakeLordJob(parms, target, pawns1, raidSeed), target, (IEnumerable<Pawn>)pawns1);
                    lord.inSignalLeave = parms.inSignalEnd;
                    QuestUtility.AddQuestTag((object)lord, parms.questTag);
                    if (DebugViewSettings.drawStealDebug && parms.faction.HostileTo(Faction.OfPlayer))
                        Log.Message("Market value threshold to start stealing (raiders=" + (object)lord.ownedPawns.Count + "): " + (object)StealAIUtility.StartStealingMarketValueThreshold(lord) + " (colony wealth=" + (object)target.wealthWatcher.WealthTotal + ")");
                }
            }

            FleckMaker.ThrowMetaPuff(this.Position.ToVector3(), thisMap);

            // Destroy tunnel.
            this.Destroy(DestroyMode.Vanish);
        }

        protected LordJob MakeLordJob(
          IncidentParms parms,
          Map map,
          List<Pawn> pawns,
          int raidSeed)
        {
            IntVec3 originCell = parms.spawnCenter.IsValid ? parms.spawnCenter : pawns[0].PositionHeld;
            if (parms.attackTargets != null && parms.attackTargets.Count > 0)
            {
                return (LordJob)new LordJob_AssaultThings(parms.faction, parms.attackTargets, 1f, false);
            }
            if (parms.faction.HostileTo(Faction.OfPlayer))
            {
                return (LordJob)new LordJob_AssaultColony(parms.faction, true, false, false, false, true, false, false);
            }
            IntVec3 result;
            RCellFinder.TryFindRandomSpotJustOutsideColony(originCell, map, out result);
            return (LordJob)new LordJob_AssistColony(parms.faction, result);
        }

        private void DoMadSkaven(Map thisMap)
        {
            Faction f = Find.FactionManager.FirstFactionOfDef(SkavenDefOf.RH_TET_Skaven_UnderEmpireFaction);

            // Create pawn group.
            float points = (this.Map.mapPawns.FreeColonistsSpawned.Count * 35f);
            if (points < 50)
                points = 50;

            PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms();
            pawnGroupMakerParms.tile = thisMap.Tile;
            pawnGroupMakerParms.faction = f;
            pawnGroupMakerParms.points = points;
            pawnGroupMakerParms.groupKind = PawnGroupKindDefOf.Combat;

            IEnumerable<Pawn> pawns = PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms);
            if (!pawns.Any<Pawn>())
            {
                Log.Warning("No pawns created for skaven under empire berserk tunnelers.");
                return;
            }

            // Spawn pawns.
            Pawn saveP = null;

            foreach (Pawn p in pawns)
            {
                saveP = p;
                p.Position = this.Position;
                p.SetFaction(null);
                p.SpawnSetup(this.Map, false);
                p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, (string)null, true, true, false, (Pawn)null, true, false, false);
            }

            // Send Message.
            Find.LetterStack.ReceiveLetter("RH_TET_Skaven_BerserkTunnelRaidLabel".Translate(),
                    "RH_TET_Skaven_BerserkTunnelRaid".Translate(),
                    LetterDefOf.NegativeEvent,
                    (LookTargets)saveP,
                    null,
                    null,
                    null,
                    null);

            FleckMaker.ThrowMetaPuff(this.Position.ToVector3(), thisMap);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = this.Map;
            base.DeSpawn(mode);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
        }

        public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
        {
            base.Kill(dinfo, exactCulprit);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            SkavenTunnelFriendly tunnel = this;
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;
            foreach (Gizmo questRelatedGizmo in QuestUtility.GetQuestRelatedGizmos((Thing)tunnel))
                yield return questRelatedGizmo;
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Do Random Event",
                    action = delegate
                    {
                        nextTickEvent = Find.TickManager.TicksGame + 13;
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Spawn Tunnel Raid",
                    action = delegate
                    {
                        DoRaid(this.Map);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Spawn Berserk Raid",
                    action = delegate
                    {
                        DoMadSkaven(this.Map);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Spawn Gas",
                    action = delegate
                    {
                        DoGasCloud(this.Map);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Joiner: Slave",
                    action = delegate
                    {
                        GenerateAndSpawnPawn(SkavenDefOf.RH_TET_Skaven_SlaveRatMelee, this.Map);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Joiner: Clan Rat",
                    action = delegate
                    {
                        GenerateAndSpawnPawn(SkavenDefOf.RH_TET_Skaven_ClanratLow, this.Map);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Joiner: Stormvermin",
                    action = delegate
                    {
                        GenerateAndSpawnPawn(SkavenDefOf.RH_TET_Skaven_ClanratHigh, this.Map);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Joiner: Assassin",
                    action = delegate
                    {
                        GenerateAndSpawnPawn(SkavenDefOf.RH_TET_Skaven_Assassin, this.Map);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Joiner: Magic User",
                    action = delegate
                    {
                        GenerateAndSpawnPawn(SkavenDefOf.RH_TET_Skaven_WizardScenario, this.Map);
                    }
                };
            }
            yield break;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.nextTickEvent, "nextTickEvent", 0, false);
            if (Scribe.mode == LoadSaveMode.Saving)
                return;
            bool flag = false;
            Scribe_Values.Look<bool>(ref flag, "active", false, false);
            if (!flag)
                return;
        }
    }
}
