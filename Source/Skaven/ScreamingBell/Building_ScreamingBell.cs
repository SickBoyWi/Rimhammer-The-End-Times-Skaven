using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;
using RimWorld;
using TheEndTimes_Magic;

namespace TheEndTimes_Skaven
{
    public partial class Building_ScreamingBell : Building //, IBillGiver, IThingHolder
    {

        //Universal Variables
        public enum State { ready = 0, ringing, rung };
        private State currentState = State.ready;
        private bool destroyedFlag = false;
        public string ScreamingBellName = "Screaming Bell";
        public int resultBonus = 0;
        public Pawn seer = null;

        // Cooldowns
        private readonly int CombatRingCooldown = 30000 * 7; // 3.5 days
        private readonly int NoncombatRingCooldown = 60000 * 5; // 5 days
        private readonly int MightyRingCooldown = 60000 * 15; // 15 days
        public IEnumerable<IntVec3> CellsAround => GenRadial.RadialCellsAround(Position, 5, true);
        private bool IsReady() =>
            currentState == State.ready;
        private int CombatRingReadyTick = -1;
        private int NonCombatRingReadyTick = -1;
        private int MightyRingReadyTick = -1;

        public Building_ScreamingBell()
        {
        }

        public void ChangeState(State type)
        {
            if (type == State.ready)
            {
                currentState = type;
            }
            else Log.Error("Changed default state of Screaming Bell this should never happen.");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (ScreamingBellName == null) ScreamingBellName = "Screaming Bell";

        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.CombatRingReadyTick, "CombatRingReadyTick", -1);
            Scribe_Values.Look<int>(ref this.NonCombatRingReadyTick, "NonCombatRingReadyTick", -1);
            Scribe_Values.Look<int>(ref this.MightyRingReadyTick, "MightyRingReadyTick", -1);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // block further ticker work
            destroyedFlag = true;
            base.Destroy(mode);
        }

        public override void Tick()
        {
            if (destroyedFlag)
                return;

            if (!Spawned) return;

            // Don't forget the base work
            base.Tick();
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();

            // Add the inspections string from the base
            stringBuilder.Append(base.GetInspectString());

            stringBuilder.AppendLine();

            bool newLineRequired = false;

            // TODO - Make all these tagged strings in languages file.
            bool onCooldown = false;
            if (CombatRingReadyTick > Find.TickManager.TicksGame)
            {
                onCooldown = true;
                stringBuilder.Append("Combat ring cooldown time remaining:" + ((CombatRingReadyTick - Find.TickManager.TicksGame).ToStringTicksToPeriod(true,false,true,false)));

                newLineRequired = true;
            }
            if (NonCombatRingReadyTick > Find.TickManager.TicksGame)
            {
                if (newLineRequired)
                {
                    stringBuilder.AppendLine();
                    newLineRequired = false;
                }

                onCooldown = true;
                stringBuilder.Append("Non-combat ring cooldown time remaining:" + ((NonCombatRingReadyTick - Find.TickManager.TicksGame).ToStringTicksToPeriod(true, false, true, false)));
                
                newLineRequired = true;
            }
            if (MightyRingReadyTick > Find.TickManager.TicksGame)
            {
                if (newLineRequired)
                {
                    stringBuilder.AppendLine();
                    newLineRequired = false;
                }

                onCooldown = true;
                stringBuilder.Append("Mighty ring cooldown time remaining:" + ((MightyRingReadyTick - Find.TickManager.TicksGame).ToStringTicksToPeriod(true, false, true, false)));

                newLineRequired = true;
            }
            if (!onCooldown)
            {
                if (newLineRequired)
                {
                    stringBuilder.AppendLine();
                    newLineRequired = false;
                }

                stringBuilder.Append("Ready for use.");

                newLineRequired = true;
            }

            // return the complete string
            return stringBuilder.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;

            int currTicks = Find.TickManager.TicksGame;
            bool canNonCombatRing = this.Faction ==  Faction.OfPlayerSilentFail && NonCombatRingReadyTick <= currTicks;
            bool canCombatRing = this.Faction == Faction.OfPlayerSilentFail && CombatRingReadyTick <= currTicks;
            bool canMightyRing = this.Faction == Faction.OfPlayerSilentFail && MightyRingReadyTick <= currTicks;

            var command_Action = new Command_Action
            {
                action = NoncombatRing,
                defaultLabel = "RH_TET_Skaven_CommandNoncombatRing".Translate(),
                defaultDesc = "RH_TET_Skaven_CommandNoncombatRingDesc".Translate(),
                disabled = !canCombatRing || !canNonCombatRing,
                disabledReason = "RH_TET_Skaven_CommandBellDisabled".Translate(),
                hotKey = KeyBindingDefOf.Misc1,
                icon = ContentFinder<Texture2D>.Get("UI/RH_TET_SkavenBellRing_Noncombat")
            };
            yield return command_Action;

            var command_Action2 = new Command_Action
            {
                action = CombatRing,
                defaultLabel = "RH_TET_Skaven_CommandCombatRing".Translate(),
                defaultDesc = "RH_TET_Skaven_CommandCombatRingDesc".Translate(),
                disabled = !canCombatRing || !canNonCombatRing,
                disabledReason = "RH_TET_Skaven_CommandBellDisabled".Translate(),
                hotKey = KeyBindingDefOf.Misc2,
                icon = ContentFinder<Texture2D>.Get("UI/RH_TET_SkavenBellRing_Combat")
            };
            yield return command_Action2;

            var command_Action3 = new Command_Action
            {
                action = MightyRing,
                defaultLabel = "RH_TET_Skaven_CommandMightyRing".Translate(),
                defaultDesc = "RH_TET_Skaven_CommandMightyRingDesc".Translate(),
                disabled = !canMightyRing || !canCombatRing || !canNonCombatRing,
                disabledReason = "RH_TET_Skaven_CommandBellDisabled".Translate(),
                hotKey = KeyBindingDefOf.Misc3,
                icon = ContentFinder<Texture2D>.Get("UI/RH_TET_SkavenBellRing_Mighty")
            };
            yield return command_Action3;

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Reset Bell Cooldowns",
                    action = delegate
                    {
                        CombatRingReadyTick = Find.TickManager.TicksGame;
                        NonCombatRingReadyTick = CombatRingReadyTick;
                        MightyRingReadyTick = CombatRingReadyTick;
                    }
                };
            }

            yield break;
        }

        private void MightyRing()
        {
            int random = RH_TET_SkavenMod.random.Next(0, 10);

           SkavenDefOf.RH_TET_Skaven_BellRing_Mighty. PlayOneShotOnCamera(this.Map);

            // 10% chance of something bad happening.
            if (random < 1)
            {
                ResolveMightyRingBad();
            }
            else
            {
                // Good
                ResolveMightyRingGood();
            }

            MightyRingReadyTick = MightyRingCooldown + Find.TickManager.TicksGame + RH_TET_SkavenMod.random.Next(1000,5000);
        }

        private void ResolveMightyRingGood()
        {
            int rando = RH_TET_SkavenMod.random.Next(0, 100);

            if (rando < 34)
            {
                // Summon joiner
                Messages.Message("RH_TET_Skaven_BellRingPositiveJoiner".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                SkavenUtil.SpawnWandererJoin(this);
            }
            else if (rando < 66)
            {
                // Spawn magic item
                // TODO ADD MORE HERE WHEN SKAVEN GET UNIQUE ITEMS.
                List<ThingDef> listOfPotentials = new List<ThingDef>();
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_NurgleCharm);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_MeleeWeapon_NurgleStar);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_Magic_Apparel_NurgleBand);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_Potion_Healing);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_Wand_Resurrection);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_Magic_Jewelry_RubyRingOfRuin);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_BootsOfSpeed);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_BootsWinged);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_Magic_BeltArdor);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_Rod_FlamingDeath);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_Magic_HelmetRegeneration);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_Magic_ArmorVitality);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_MeleeWeapon_GoldSigilSword);
                listOfPotentials.Add(RH_TET_MagicDefOf.RH_TET_MeleeWeapon_ObsidianBlade);

                ThingDef thingToMake = listOfPotentials.RandomElement<ThingDef>();
                Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingPositiveLabel".Translate(), "RH_TET_Skaven_BellRingPositiveDesc".Translate(thingToMake.label), LetterDefOf.PositiveEvent, (LookTargets)(Thing)this), (string)null);
                SkavenUtil.SpawnItemNearBuilding(this, thingToMake);
            }
            else
            {
                // Spawn warpstone
                Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingPositiveLabel".Translate(), "RH_TET_Skaven_BellRingPositiveDesc".Translate(SkavenDefOf.RH_TET_Skaven_Warpstone.label), LetterDefOf.PositiveEvent, (LookTargets)(Thing)this), (string)null);
                SkavenUtil.SpawnItemNearBuilding(this, SkavenDefOf.RH_TET_Skaven_Warpstone, 13);
            }
        }

        private void ResolveMightyRingBad()
        {
            int rando = RH_TET_SkavenMod.random.Next(0, 100);

            if (rando < 34)
            {
                //Summon raid
                Messages.Message("RH_TET_Skaven_BellRingNegative".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.ThreatSmall, true);
                SkavenUtil.SpawnRaidEvent(this, new FloatRange(1.1f, 1.4f));
            }
            else if (rando < 66)
            {
                //Food poison all colonists? Kill a colonist? Other hediff all colonists?
                Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingNegativeIllnessLabel".Translate(), "RH_TET_Skaven_BellRingNegativeIllnessDesc".Translate(), LetterDefOf.NegativeEvent), (string)null);
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                pawns.RemoveLast();// Prevent man in black from showing.
                SkavenUtil.GivePawnsHediff(pawns, HediffDefOf.FoodPoisoning);
                SkavenUtil.GivePawnsHediff(pawns, RH_TET_MagicDefOf.RH_TET_Magic_Knockdown);
            }
            else
            {

                IncidentParms incidentParams = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, (IIncidentTarget)this.Map);

                if (rando < 72)
                {
                    Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingNegativeLabel".Translate(), "RH_TET_Skaven_BellRingNegativeToxicFalloutDesc".Translate(), LetterDefOf.NegativeEvent), (string)null);
                    int duration = RH_TET_SkavenMod.random.Next(5, 12);
                    GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.ToxicFallout, Mathf.RoundToInt(duration * 60000));
                    this.Map.gameConditionManager.RegisterCondition(gameCondition);
                }
                else if (rando < 78)
                {
                    Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingNegativeLabel".Translate(), "RH_TET_Skaven_BellRingNegativeEclipseDesc".Translate(), LetterDefOf.NegativeEvent), (string)null);
                    int duration = RH_TET_SkavenMod.random.Next(1, 3);
                    GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.Eclipse, Mathf.RoundToInt(duration * 60000));
                    this.Map.gameConditionManager.RegisterCondition(gameCondition);
                }
                else if (rando < 84)
                {
                    Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingNegativeLabel".Translate(), "RH_TET_Skaven_BellRingNegativeDroneDesc".Translate(), LetterDefOf.NegativeEvent), (string)null);
                    int duration = RH_TET_SkavenMod.random.Next(2, 4);
                    GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.PsychicDrone, Mathf.RoundToInt(duration * 60000));
                    this.Map.gameConditionManager.RegisterCondition(gameCondition);
                }
                else if (rando < 90)
                {
                    Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingNegativeLabel".Translate(), "RH_TET_Skaven_BellRingNegativeSwarmDesc".Translate(), LetterDefOf.NegativeEvent, (LookTargets)(Thing)this), (string)null);

                    incidentParams.forced = true;
                    incidentParams.points = 1.2f;
                    IntVec3 spawnSpot;
                    SkavenUtil.TryFindSpawnSpot(this.Map, out spawnSpot);
                    incidentParams.spawnCenter = spawnSpot;
                    incidentParams.pawnCount = RH_TET_SkavenMod.random.Next(10, 65);
                    PawnKindDef animalKindUse = RH_TET_MagicDefOf.RH_TET_Rat;
                    int num = incidentParams.pawnCount > 0 ? incidentParams.pawnCount : ManhunterPackIncidentUtility.GetAnimalsCount(animalKindUse, 1.2f);
                    Find.Storyteller.incidentQueue.Add(new QueuedIncident(new FiringIncident(IncidentDefOf.ManhunterPack, null, incidentParams), Find.TickManager.TicksGame + 500, 0));
                }
                else
                {
                    try
                    {
                        GameConditionDef def = DefDatabase<GameConditionDef>.GetNamedSilentFail("RH_TET_ChaosStorm");
                        Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingNegativeLabel".Translate(), "RH_TET_Skaven_BellRingNegativeChaosStormDesc".Translate(), LetterDefOf.NegativeEvent), (string)null);
                        int duration = RH_TET_SkavenMod.random.Next(3, 10);
                        GameCondition gameCondition = GameConditionMaker.MakeCondition(def, Mathf.RoundToInt(duration * 60000));
                        this.Map.gameConditionManager.RegisterCondition(gameCondition);

                    }
                    catch
                    {
                        Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingNegativeLabel".Translate(), "RH_TET_Skaven_BellRingNegativeToxicFalloutDesc".Translate(), LetterDefOf.NegativeEvent), (string)null);
                        int duration = RH_TET_SkavenMod.random.Next(5, 12);
                        GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.ToxicFallout, Mathf.RoundToInt(duration * 60000));
                        this.Map.gameConditionManager.RegisterCondition(gameCondition);
                    }
                }
            }

        }

        private void CombatRing()
        {
            int random = RH_TET_SkavenMod.random.Next(0, 100);

            SkavenDefOf.RH_TET_Skaven_BellRing_Combat.PlayOneShotOnCamera(this.Map);
            
            // 15% chance of something bad happening.
            if (random < 15)
            {
                ResolveCombatRingBad();
            }
            else
            {
                // Good
                ResolveCombatRingGood();
            }

            CombatRingReadyTick = CombatRingCooldown + Find.TickManager.TicksGame + RH_TET_SkavenMod.random.Next(1000, 5000);
        }

        private void ResolveCombatRingGood()
        {
            int rando = RH_TET_SkavenMod.random.Next(0, 100);
            
            if (rando < 18)
            {
                //Light Debuff Enemies
                HashSet<IAttackTarget> targets = this.Map.attackTargetsCache.TargetsHostileToFaction(this.Faction);
                List<Pawn> hostilePawns = new List<Pawn>();
                foreach (IAttackTarget target in targets)
                {
                    Pawn p = target as Pawn;
                    if (p != null && !p.NonHumanlikeOrWildMan())
                        hostilePawns.Add(p);
                }

                if (hostilePawns.Count > 0)
                {
                    Messages.Message("RH_TET_Skaven_BellRingPositiveDebuffFoesMinorDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                    SkavenUtil.GivePawnsHediff(hostilePawns, RH_TET_MagicDefOf.RH_TET_NurgleAfflictionsI);
                }
                else
                {
                    Messages.Message("RH_TET_Skaven_BellRingPositiveNoEffectDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            else if (rando < 32)
            {
                //Major Debuff Enemies
                HashSet<IAttackTarget> targets = this.Map.attackTargetsCache.TargetsHostileToFaction(this.Faction);
                List<Pawn> hostilePawns = new List<Pawn>();
                foreach (IAttackTarget target in targets)
                {
                    Pawn p = target as Pawn;
                    if (p != null && !p.NonHumanlikeOrWildMan())
                        hostilePawns.Add(p);
                }

                if (hostilePawns.Count > 0)
                {
                    Messages.Message("RH_TET_Skaven_BellRingPositiveDebuffFoesMajorDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                    SkavenUtil.GivePawnsHediff(hostilePawns, RH_TET_MagicDefOf.RH_TET_NurgleAfflictionsIV);
                }
                else
                {
                    Messages.Message("RH_TET_Skaven_BellRingPositiveNoEffectDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            else if (rando < 50)
            {
                //Light Combat Boost friendlies
                Messages.Message("RH_TET_Skaven_BellRingPositiveBoostMinorDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                SkavenUtil.GivePawnsHediff(pawns, RH_TET_MagicDefOf.RH_TET_ChaosBoonI);
            }
            else if (rando < 67)
            {
                //Combat Boost Friendlies
                Messages.Message("RH_TET_Skaven_BellRingPositiveBoostMajorDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                SkavenUtil.GivePawnsHediff(pawns, RH_TET_MagicDefOf.RH_TET_ChaosBoonIV);
            }
            else //if (rando < 85)
            {
                //Summons Rat Swarms near enemies
                if (GenHostility.AnyHostileActiveThreatToPlayer(this.Map, false))
                {
                    HashSet<IAttackTarget> targets = this.Map.attackTargetsCache.TargetsHostileToFaction(this.Faction);

                    if (targets.Count > 0)
                    {
                        int pawnSpawnCount = 13;
                        IntVec3 position = targets.First().Thing.Position;

                        Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingPositiveLabel".Translate(), "RH_TET_Skaven_BellRingPositiveRatSwarmDesc".Translate(), LetterDefOf.PositiveEvent, (LookTargets)targets.First().Thing), (string)null);

                        for (int i = 0; i < pawnSpawnCount; i++)
                        {
                            float biologicalAge = (float)RH_TET_MagicMod.random.Next(2, 5);

                            PawnGenerationRequest pawnRequest = new PawnGenerationRequest(RH_TET_MagicDefOf.RH_TET_Rat, this.Faction, PawnGenerationContext.PlayerStarter, -1,
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
                            newPawn.SetFaction(null);

                            try
                            {
                                GenSpawn.Spawn((Thing)newPawn, position, this.Map, WipeMode.Vanish);
                                newPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, (string)null, true, false, (Pawn)null, true);
                            }
                            catch
                            {
                                Log.Warning("Screaming Bell could not spawn angry rat.");
                                //this.Destroy(DestroyMode.Vanish);
                            }
                        }
                    }
                }
                else
                {
                    Messages.Message("RH_TET_Skaven_BellRingPositiveNoEffectDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            //else
            //{
                // TODO JEH - DO THIS WHEN YOU HAVE A MONSTER TO SUMMON.
                //Summon monster helper(verminlord/ abom)

                // TODO JEH - FOR NOW, COPY PREVIOUS ITEM.
            //}
        }

        private void ResolveCombatRingBad()
        {
            int rando = RH_TET_SkavenMod.random.Next(0, 100);

            if (rando < 30)
            {
                //Debuff Friends
                Messages.Message("RH_TET_Skaven_BellRingNegativeDefuffDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.NegativeEvent, true);
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                SkavenUtil.GivePawnsHediff(pawns, RH_TET_MagicDefOf.RH_TET_NurgleAfflictionsI);
            }
            else if (rando < 65)
            {
                //Lightly Harms friends
                Messages.Message("RH_TET_Skaven_BellRingNegativeExplodeDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.NegativeEvent, true);

                Map map1 = this.Map;
                IntVec3 position = this.Position;
                Map map2 = map1;
                double explosionRadius = 8.0;
                DamageDef bomb = DamageDefOf.Bomb;
                int damageAmount = 100;
                double armorPenetration = .25;
                ThingDef def = this.def;
                ThingDef filth = ThingDefOf.Filth_CorpseBile;
                Thing thing = this;
                ThingDef postExplosionSpawnThingDef = filth;
                float? direction = new float?();
                // Note: Launcher is null, may cause issue. So is equipment def.
                GenExplosion.DoExplosion(position, map2, (float)explosionRadius, bomb, null, damageAmount, (float)armorPenetration, (SoundDef)null, null, def, thing, postExplosionSpawnThingDef, 0.2f, 1, new GasType?(), false, (ThingDef)null, 0.0f, 1, 0.4f, false, direction, (List<Thing>)null);
                CellRect cellRect = CellRect.CenteredOn(this.Position, 5);
                cellRect.ClipInsideMap(map1);
                for (int index = 0; index < 3; ++index)
                    GenExplosion.DoExplosion(position, map2, 3.0f, DamageDefOf.Flame, null, 20, 0f, (SoundDef)null, null, this.def, this, (ThingDef)null, 0.0f, 1, new GasType?(), false, (ThingDef)null, 0.0f, 1, 0.0f, false, new float?(), (List<Thing>)null);
            }
            else
            {
                //Summon second raid
                Messages.Message("RH_TET_Skaven_BellRingNegative".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.ThreatSmall, true);
                SkavenUtil.SpawnRaidEvent(this, new FloatRange(.6f, .8f));
            }
        }

        private void NoncombatRing()
        {
            int random = RH_TET_SkavenMod.random.Next(0, 10);

            SkavenDefOf.RH_TET_Skaven_BellRing_Noncombat.PlayOneShotOnCamera(this.Map);

            // 20% chance of something bad happening.
            if (random < 2)
            {
                ResolveNoncombatRingBad();
            }
            else
            {
                // Good
                ResolveNoncombatRingGood();
            }

            NonCombatRingReadyTick = NoncombatRingCooldown + Find.TickManager.TicksGame + RH_TET_SkavenMod.random.Next(1000, 5000);
        }

        private void ResolveNoncombatRingGood()
        {
            int rando = RH_TET_SkavenMod.random.Next(0, 100);

            if (rando < 25)
            {
                //Work buff: Inspiration: Frenzy_Work
                Messages.Message("RH_TET_Skaven_BellRingPositiveInspiredDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                InspirationDef inspDef = DefDatabase<InspirationDef>.GetNamedSilentFail("Frenzy_Work");
                SkavenUtil.GivePawnsInspiration(pawns, inspDef);
            }
            else if(rando < 45)
            {
                //Spawn  item / food 
                List<ThingDef> listOfPotentials = new List<ThingDef>();
                int stackCount = 1;

                int randy = RH_TET_SkavenMod.random.Next(0, 7);
                ThingDef thingToMake = null;

                if (randy == 0)
                {
                    thingToMake = ThingDefOf.Jade;
                    stackCount = 100;
                }
                else if (randy == 1)
                {
                    thingToMake = ThingDefOf.Pemmican;
                    stackCount = 1000;
                }
                else if (randy == 2)
                {
                    thingToMake = ThingDefOf.Meat_Human;
                    stackCount = 500;
                }
                else if (randy == 3)
                {
                    thingToMake = ThingDefOf.MedicineHerbal;
                    stackCount = 50;
                }
                else if (randy == 4)
                {
                    thingToMake = ThingDefOf.SmokeleafJoint;
                    stackCount = 75;
                }
                else if (randy == 5)
                {
                    thingToMake = ThingDefOf.Steel;
                    stackCount = 250;
                }
                else
                {
                    thingToMake = ThingDefOf.Beer;
                    stackCount = 75;
                }

                Find.LetterStack.ReceiveLetter((Letter)LetterMaker.MakeLetter("RH_TET_Skaven_BellRingPositiveLabel".Translate(), "RH_TET_Skaven_BellRingPositiveDesc".Translate(thingToMake.label), LetterDefOf.PositiveEvent, (LookTargets)(Thing)this), (string)null);
                while (stackCount > 0)
                {
                    if (stackCount > thingToMake.stackLimit)
                    {
                        SkavenUtil.SpawnItemNearBuilding(this, thingToMake, thingToMake.stackLimit);
                        stackCount = stackCount - thingToMake.stackLimit;
                    }
                    else
                    {
                        SkavenUtil.SpawnItemNearBuilding(this, thingToMake, stackCount);
                        stackCount = 0;
                    }
                }
            }
            else if (rando < 65)
            {
                //Mood buff
                Messages.Message("RH_TET_Skaven_BellRingPositiveMoodDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                SkavenUtil.GivePawnsThought(pawns, SkavenDefOf.RH_TET_Skaven_BlessedBe);
            }
            else if (rando < 90)
            {
                //Cure all mental breaks → default to mood buff if no mental breaks on.
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                bool mentalStateFound = false;
                foreach(Pawn pawn in pawns)
                    if (pawn.InMentalState)
                    {
                        mentalStateFound = true;
                        pawn.mindState.mentalStateHandler.Reset();
                        Messages.Message("RH_TET_Skaven_BellRingMentalStateFixedDesc".Translate(pawn.Name), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                    }

                if (!mentalStateFound)
                {
                    //Mood buff
                    Messages.Message("RH_TET_Skaven_BellRingPositiveMoodDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);
                    SkavenUtil.GivePawnsThought(pawns, SkavenDefOf.RH_TET_Skaven_BlessedBe);
                }
            }
            else
            {
                //Summon rats that join
                Messages.Message("RH_TET_Skaven_BellRingPositiveRatJoinersdDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.PositiveEvent, true);

                int pawnSpawnCount = 13;
                Map map = this.Map;
                IntVec3 position = this.Position;

                SkavenUtil.SpawnRatSwarm(this.Faction, position, map, pawnSpawnCount);
            }
        }

        private void ResolveNoncombatRingBad()
        {
            int rando = RH_TET_SkavenMod.random.Next(0, 100);

            if (rando < 25)
            {
                //Work debuff
                Messages.Message("RH_TET_Skaven_BellRingNegativeWorkSlowDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.NegativeEvent, true);
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                SkavenUtil.GivePawnsHediff(pawns, RH_TET_MagicDefOf.RH_TET_Lethargic);
            }
            else if (rando < 50)
            {
                //Mood debuff 
                Messages.Message("RH_TET_Skaven_BellRingNegativeMoodDesc".Translate(), (LookTargets)(Thing)this, MessageTypeDefOf.NegativeEvent, true);
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;
                SkavenUtil.GivePawnsThought(pawns, SkavenDefOf.RH_TET_Skaven_ForsakenBe);
            }
            else if (rando < 75)
            {
                //Cause Disease(random, 1 - 3 pawns)
                HediffDef use1 = HediffDefOf.Malaria;
                HediffDef use2 = HediffDefOf.Plague;
                HediffDef use3 = HediffDefOf.Flu;

                List<HediffDef> diseases = new List<HediffDef>();
                diseases.Add(use1);
                diseases.Add(use2);
                diseases.Add(use3);

                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;

                int randy = RH_TET_SkavenMod.random.Next(1, 3);
                for (int i = 0; i < randy; i++)
                {
                    Pawn p = pawns.RandomElement();
                    p.health.AddHediff(diseases[i]);
                    Messages.Message("RH_TET_Skaven_BellRingNegativeDiseaseDesc".Translate(p.Name, diseases[i].label), (LookTargets)(Thing)this, MessageTypeDefOf.NegativeEvent, true);
                }
            }
            else
            {
                //Cause Mental Break
                MentalStateDef use1 = MentalStateDefOf.Berserk;
                MentalStateDef use2 = MentalStateDefOf.Wander_OwnRoom;
                MentalStateDef use3 = MentalStateDefOf.Wander_Psychotic;
                MentalStateDef use4 = MentalStateDefOf.Wander_Sad;

                List<MentalStateDef> states = new List<MentalStateDef>();
                states.Add(use1);
                states.Add(use2);
                states.Add(use3);
                states.Add(use4);

                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsSpawned;

                int randy = RH_TET_SkavenMod.random.Next(1, 3);
                for (int i = 0; i < randy; i++)
                {
                    Pawn p = pawns.RandomElement();
                    MentalStateDef state = states.RandomElement();
                    p.mindState.mentalStateHandler.TryStartMentalState(state, "bell ring", true, false, (Pawn)null, true);
                    Messages.Message("RH_TET_Skaven_BellRingNegativeMentalDesc".Translate(p.Name, state.label), (LookTargets)(Thing)this, MessageTypeDefOf.NegativeEvent, true);
                }
            }
        }
    }
}