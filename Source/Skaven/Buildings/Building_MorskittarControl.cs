using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace TheEndTimes_Skaven
{
    public class Building_MorskittarControl : Building
    {
        public enum State { notinuse = 0, initiating, powering, powered };
        private State currentState = State.notinuse;
        private bool isDestroyed = false;
        Effecter empEffecter = null;

        // Events / end game variables.
        private int nextRaidStrongTick = -1;
        private int chargeTicksCompleted = 0;
        private int STRONG_RAID_TICKS_MIN = (int)(60000f * 1.5f);
        private int STRONG_RAID_TICKS_MAX = (int)(60000f * 2.5f);
        private int TICKS_TILL_FULLY_CHARGED = 60000 * 13;

        private bool IsPowering() => currentState == State.initiating || currentState == State.powering;
        private bool IsPowered() => currentState == State.initiating || currentState == State.powered;

        private bool CanShootNow
        {
            get
            {
                return !SkavenUtil.ShootFailReasons((Building)this).Any<string>();
            }
        }

        public void ChangeState(State type)
        {
            currentState = type;
            ReportState();
        }

        private void ReportState()
        {
            var s = new StringBuilder();
            s.Append("===================");
            s.AppendLine("Morskittar Cannon States Changed");
            s.AppendLine("===================");
            s.AppendLine("State: " + currentState);
            s.AppendLine("===================");
            SkavenUtil.DebugReport(s.ToString());
        }
        public override void SpawnSetup(Map map, bool bla)
        {
            base.SpawnSetup(map, bla);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<State>(ref currentState, "currentState");
            Scribe_Values.Look<int>(ref this.chargeTicksCompleted, "chargeTicksCompleted");
            Scribe_Values.Look<int>(ref this.nextRaidStrongTick, "nextRaidStrongTick");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // block further ticker work
            isDestroyed = true;
            base.Destroy(mode);
        }

        public override void Tick()
        {
            if (isDestroyed)
                return;

            if (!Spawned) return;

            base.Tick();

            if (Faction.OfPlayerSilentFail == null || this.Faction != Faction.OfPlayer)
                return;

            MorskittarTick();
        }

        private void MorskittarTick()
        {
            if (IsPowering() || IsPowered())
            {
                PoweringTick();
            }

            if (chargeTicksCompleted >= TICKS_TILL_FULLY_CHARGED)
            {
                currentState = State.powered;
            }
        }

        private void WinTheGame()
        {
            DropMeteors();
            DoVictory();
        }

        private void PoweringTick()
        {
            if (!this.IsPowered() && !this.CanPowerNow())
            {
                // Shut it down.
                this.CancelPowering();
            }

            if (nextRaidStrongTick <= 0)
            {
                float threatScale = Find.Storyteller.difficulty.threatScale;
                nextRaidStrongTick = Find.TickManager.TicksGame + new IntRange(STRONG_RAID_TICKS_MIN, STRONG_RAID_TICKS_MAX).RandomInRange;
            }

            if (chargeTicksCompleted < TICKS_TILL_FULLY_CHARGED)
                chargeTicksCompleted++;

            DoEffecterStuff();

            if (nextRaidStrongTick < Find.TickManager.TicksGame)
            {
                float threatScale = Find.Storyteller.difficulty.threatScale;
                nextRaidStrongTick = Find.TickManager.TicksGame + new IntRange(STRONG_RAID_TICKS_MIN, STRONG_RAID_TICKS_MAX).RandomInRange;

                SkavenUtil.SpawnRaidEvent(this, new FloatRange(1.1f, 1.3f));
            }
        }

        private void DoEffecterStuff()
        {
            List<Building> buildingsOnCannon = SkavenUtil.CannonBuildingsAttachedTo(this, false);

            if (this.empEffecter == null)
            {
                this.empEffecter = EffecterDefOf.DisabledByEMP.Spawn(this, this.Map);
            }

            foreach(Building b in buildingsOnCannon)
                this.empEffecter.EffectTick((TargetInfo)b, (TargetInfo)b);
        }

        private void DoVictory()
        {
            if (currentState != State.powered)
                return;

            RH_TET_Skaven_GameVictoryUtility.PlayerVictory(this.Map);

            currentState = State.notinuse;
            chargeTicksCompleted = 0;
            nextRaidStrongTick = -1;
        }
        private void DropMeteors()
        {
            if (currentState != State.powered)
                return;

            FleckMaker.Static(this.Position, this.Map, FleckDefOf.LightningGlow, 10f);

            Map map = this.Map;
            IntVec3 cell;
            bool noSpot = false;
            int meteorCount = RH_TET_SkavenMod.random.Next(80, 100);

            RH_TET_SkavenMod.EndGameProcessing = true;

            for(int i = 0; i < meteorCount; i++)
            { 
                noSpot = false;
                if (!this.TryFindCell(out cell, map))
                { 
                    Log.Warning("Couldn't find a spot to drop the warpstone meteor.");
                    noSpot = true;
                }
                if (!noSpot)
                { 
                    List<Thing> thingList = ThingSetMakerDefOf.Meteorite.root.Generate();
                    SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, (IEnumerable<Thing>)thingList, cell, map);
                }
            }

            RH_TET_SkavenMod.EndGameProcessing = false;

            if (this.empEffecter != null)
            {
                this.empEffecter.Cleanup();
                this.empEffecter = null;
            }
        }
        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();

            // Add the inspections string from the base
            stringBuilder.Append(base.GetInspectString());

            if (currentState == State.notinuse)
                stringBuilder.Append("RH_TET_Skaven_NotInUse".Translate());
            else if (currentState == State.initiating)
                stringBuilder.Append("RH_TET_Skaven_Initiating".Translate());
            else if (currentState == State.powering)
                stringBuilder.Append(string.Format("{0}: {1}", (object)"RH_TET_Skaven_PoweringUp".Translate(), (object)(this.TICKS_TILL_FULLY_CHARGED - this.chargeTicksCompleted).ToStringTicksToPeriod(true, false, true, true)));
            else if (currentState == State.powered)
                stringBuilder.Append("RH_TET_Skaven_ReadyToFire".Translate());

            return stringBuilder.ToString();
        }

        private bool TryFindCell(out IntVec3 cell, Map map)
        {
            int maxMineables = ThingSetMaker_Meteorite.MineablesCountRange.max;
            return CellFinderLoose.TryFindSkyfallerCell(ThingDefOf.MeteoriteIncoming, map, out cell, 6, new IntVec3(), -1, true, true, true, false, false, false, (Predicate<IntVec3>)(x =>
            {
                int num1 = Mathf.CeilToInt(Mathf.Sqrt((float)maxMineables)) + 2;
                CellRect cellRect = CellRect.CenteredOn(x, num1, num1);
                int num2 = 0;
                foreach (IntVec3 c in cellRect)
                {
                    if (c.InBounds(map) && c.Standable(map))
                        ++num2;
                }
                return num2 >= maxMineables;
            }));
        }

        private bool CanPowerNow()
        {
            if (IsPowered())
                return false;

            if (this.Faction != Faction.OfPlayer)
                return false;

            if (!SkavenUtil.ShootFailReasons((Building)this).Any<string>())
                return true;

            return false;
        }

        private void TryToBeginPowering()
        {
            if (IsPowering())
            {
                Messages.Message("RH_TET_Skaven_PoweringAlready".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            switch (currentState)
            {
                case State.notinuse:
                    StartPowering();
                    return;

                case State.initiating:
                case State.powering:
                case State.powered:
                    Messages.Message("RH_TET_Skaven_PoweringAlready".Translate(), TargetInfo.Invalid, MessageTypeDefOf.RejectInput);
                    return;
            }
        }

        private bool PreStartPoweringReady()
        {
            if (Destroyed || !Spawned)
            {
                AbortPowering("The Morskittar Engine is unavailable.");
                return false;
            }
            else if (!this.CanShootNow)
            {
                AbortPowering("The Morskittar Engine is missing components.");
                return false;
            }

            return true;
        }

        private void AbortPowering(String reason)
        {
            currentState = State.notinuse;
            Messages.Message(reason + " " + "RH_TET_Skaven_MorskittarAbortPowering".Translate(), MessageTypeDefOf.NegativeEvent);
        }

        private void StartPowering()
        {
            //Check for missing things.
            if (!PreStartPoweringReady()) return;

            //Send a message about the gathering
            if (Map?.info?.parent is Settlement factionBase)
            {
                Messages.Message("RH_TET_Skaven_MorskittarStarted".Translate(), TargetInfo.Invalid, MessageTypeDefOf.NeutralEvent);
            }

            currentState = State.powering;

            SkavenUtil.DebugReport("Morskittar powering started.");
        }

        private void CancelPowering()
        {
            currentState = State.notinuse;
            chargeTicksCompleted = 0;
            if (empEffecter != null)
            {
                empEffecter.Cleanup();
                empEffecter = null;
            }

            Messages.Message("RH_TET_Skaven_PoweringCancelled".Translate(), MessageTypeDefOf.NegativeEvent);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;

            if (!IsPowering())
            {
                if (CanPowerNow())
                {
                    var command_Action = new Command_Action
                    {
                        action = TryToBeginPowering,
                        defaultLabel = "RH_TET_Skaven_CommandPower".Translate(),
                        defaultDesc = "RH_TET_Skaven_CommandPowerDesc".Translate(),
                        hotKey = KeyBindingDefOf.Misc3,
                        icon = ContentFinder<Texture2D>.Get("UI/RH_TET_Skaven_Power")
                    };
                    command_Action.Disabled = false;
                    yield return command_Action;
                }
                else
                {
                    string descr = "RH_TET_Skaven_CommandPowerDescUnavailable".Translate();
                    if (IsPowered())
                        descr = "RH_TET_Skaven_CommandPowerDescUnavailablePowered".Translate();

                    var command_Action = new Command_Action
                    {
                        action = null,
                        defaultLabel = "RH_TET_Skaven_CommandPower".Translate(),
                        defaultDesc = descr,
                        hotKey = KeyBindingDefOf.Misc3,
                        icon = ContentFinder<Texture2D>.Get("UI/RH_TET_Skaven_PowerDisabled")
                    };
                    command_Action.Disabled = true;
                    yield return command_Action;
                }
            }
            else
            {
                var command_Cancel = new Command_Action
                {
                    action = CancelPowering,
                    defaultLabel = "RH_TET_Skaven_Cancel".Translate(),
                    defaultDesc = "RH_TET_Skaven_CommandCancelPowering".Translate(),
                    hotKey = KeyBindingDefOf.Designator_Cancel,
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel")
                };
                yield return command_Cancel;
            }

            if (IsPowered())
            {
                var command_Action = new Command_Action
                {
                    action = WinTheGame,
                    defaultLabel = "RH_TET_Skaven_CommandFireCannon".Translate(),
                    defaultDesc = "RH_TET_Skaven_CommandFireCannonDesc".Translate(),
                    hotKey = KeyBindingDefOf.Misc3,
                    icon = ContentFinder<Texture2D>.Get("UI/RH_TET_Skaven_FireCannon")
                };
                command_Action.Disabled = false;
                yield return command_Action;
            }
            else
            {
                var command_Action = new Command_Action
                {
                    action = null,
                    defaultLabel = "RH_TET_Skaven_CommandFireCannon".Translate(),
                    defaultDesc = "RH_TET_Skaven_CommandFireDescUnavailable".Translate(),
                    hotKey = KeyBindingDefOf.Misc3,
                    icon = ContentFinder<Texture2D>.Get("UI/RH_TET_Skaven_FireCannonDisabled")
                };
                command_Action.Disabled = true;
                yield return command_Action;
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Start Powering Up Now",
                    action = delegate
                    {
                        this.currentState = State.powering;
                    }
                };
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Nearly Finish Powering Up",
                    action = delegate
                    {
                        this.chargeTicksCompleted = 60000 * 13 - 1000;
                    }
                };
            }

            yield break;
        }
    }
}
