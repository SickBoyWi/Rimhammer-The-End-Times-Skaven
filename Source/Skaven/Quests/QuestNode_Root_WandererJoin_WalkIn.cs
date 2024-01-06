using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace TheEndTimes_Skaven
{
    public class QuestNode_Root_WandererJoin_WalkIn : QuestNode_Root_WandererJoin
    {
        private const int TimeoutTicks = 60000;
        public const float RelationWithColonistWeight = 1f;
        private string signalAccept;
        private string signalReject;

        protected override void RunInt()
        {
            Faction ofPlayer = Faction.OfPlayerSilentFail;

            if (ofPlayer == null || (!ofPlayer.def.defName.Equals("RH_TET_Skaven_PlayerFaction")
                && !ofPlayer.def.defName.Equals("RH_TET_Skaven_PlayerWizard")))
            {
                return;
            }

            base.RunInt();
            Quest quest = RimWorld.QuestGen.QuestGen.quest;
            quest.Delay(60000, (Action)(() => quest.End(QuestEndOutcome.Fail, 0, (Faction)null, (string)null, QuestPart.SignalListenMode.OngoingOnly, false)), (string)null, (string)null, (string)null, false, (IEnumerable<ISelectable>)null, (string)null, false, (string)null, (string)null, (string)null, false, QuestPart.SignalListenMode.OngoingOnly);
        }

        public override Pawn GeneratePawn()
        {
            Slate slate = RimWorld.QuestGen.QuestGen.slate;
            Gender? fixedGender = new Gender?();
            PawnGenerationRequest request;

            PawnKindDef pawnKindDef = this.DeterminePawnKind();

            request = new PawnGenerationRequest(pawnKindDef, (Faction)null, PawnGenerationContext.PlayerStarter, -1, 
                true, false, 
                false, true, false, 
                RelationWithColonistWeight, false, true, 
                true,
                true, true, false, 
                false, false, false, 
                0.0f, 0.0f, (Pawn)null, 
                1f, (Predicate<Pawn>)null, (Predicate<Pawn>)null, 
                (IEnumerable<TraitDef>)null, (IEnumerable<TraitDef>)null, new float?(), 
                new float?(), new float?(), fixedGender);

            Pawn pawn = PawnGenerator.GeneratePawn(request);

            try
            {
                pawn.ideo.SetIdeo(Faction.OfPlayer.ideos.PrimaryIdeo);
            }
            catch
            {
                // Ignore if no ideos present.
            }

            if (!pawn.IsWorldPawn())
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
            return pawn;
        }

        private PawnKindDef DeterminePawnKind()
        {
            Faction ofPlayer = Faction.OfPlayerSilentFail;

            if (!ofPlayer.def.defName.Equals("RH_TET_Skaven_PlayerFaction")
            && !ofPlayer.def.defName.Equals("RH_TET_Skaven_PlayerWizard"))
            {
                return null;
            }

            int rando = RH_TET_SkavenMod.random.Next(0, 100);

            // Give the player a shaman if they don't have one.
            bool hasMagicUser = false;
            List<Pawn> pawns = PawnsFinder.AllMaps;
            foreach (Pawn candidate in pawns)
            {
                if (SkavenUtil.IsMagicUser(candidate) && !candidate.Dead)
                {
                    hasMagicUser = true;
                    break;
                }
            }

            if (!hasMagicUser || rando < 3)
            {
                return PawnKindDef.Named("RH_TET_Skaven_WizardScenario");
            }
            else if (rando < 5)
            {
                return PawnKindDef.Named("RH_TET_Skaven_WizardGreat");
            }
            else if (rando < 25)
            {
                return PawnKindDef.Named("RH_TET_Skaven_HornedPlayer");
            }
            else if (rando < 45)
            {
                return PawnKindDef.Named("RH_TET_Skaven_StormPlayer");
            }
            else if (rando < 60)
            {
                return PawnKindDef.Named("RH_TET_Skaven_ClanPlayer");
            }
            else if (rando < 80)
            {
                return PawnKindDef.Named("RH_TET_Skaven_PlayerSlave");
            }
            else if (rando < 100)
            {
                return PawnKindDef.Named("RH_TET_Skaven_SlavePlayer");
            }

            return PawnKindDef.Named("RH_TET_Skaven_SlavePlayer");
        }

        protected override void AddSpawnPawnQuestParts(Quest quest, Map map, Pawn pawn)
        {
            this.signalAccept = QuestGenUtility.HardcodedSignalWithQuestID("Accept");
            this.signalReject = QuestGenUtility.HardcodedSignalWithQuestID("Reject");
            quest.Signal(this.signalAccept, (Action)(() =>
            {
                quest.SetFaction((IEnumerable<Thing>)Gen.YieldSingle<Pawn>(pawn), Faction.OfPlayer, (string)null);
                quest.PawnsArrive(Gen.YieldSingle<Pawn>(pawn), (string)null, map.Parent, (PawnsArrivalModeDef)null, false, new IntVec3?(), (string)null, (string)null, (RulePack)null, (RulePack)null, false, false, true);
                quest.End(QuestEndOutcome.Success, 0, (Faction)null, (string)null, QuestPart.SignalListenMode.OngoingOnly, false);
            }), (IEnumerable<string>)null, QuestPart.SignalListenMode.OngoingOnly);
            quest.Signal(this.signalReject, (Action)(() =>
            {
                quest.GiveDiedOrDownedThoughts(pawn, PawnDiedOrDownedThoughtsKind.DeniedJoining, (string)null);
                quest.End(QuestEndOutcome.Fail, 0, (Faction)null, (string)null, QuestPart.SignalListenMode.OngoingOnly, false);
            }), (IEnumerable<string>)null, QuestPart.SignalListenMode.OngoingOnly);
        }

        public override void SendLetter(Quest quest, Pawn pawn)
        {
            TaggedString title = "LetterLabelWandererJoins".Translate(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true);
            TaggedString text = "LetterWandererJoins".Translate(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true);
            QuestNode_Root_WandererJoin_WalkIn.AppendCharityInfoToLetter("JoinerCharityInfo".Translate((NamedArgument)(Thing)pawn), ref text);
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref title, pawn);
            ChoiceLetter_AcceptJoiner letterAcceptJoiner = (ChoiceLetter_AcceptJoiner)LetterMaker.MakeLetter(title, text, LetterDefOf.AcceptJoiner, (Faction)null, (Quest)null);
            letterAcceptJoiner.signalAccept = this.signalAccept;
            letterAcceptJoiner.signalReject = this.signalReject;
            letterAcceptJoiner.quest = quest;
            letterAcceptJoiner.StartTimeout(60000);
            Find.LetterStack.ReceiveLetter((Letter)letterAcceptJoiner, (string)null);
        }

        public static void AppendCharityInfoToLetter(
          TaggedString charityInfo,
          ref TaggedString letterText)
        {
            if (!ModsConfig.IdeologyActive)
                return;
            IEnumerable<Pawn> source1 = IdeoUtility.AllColonistsWithCharityPrecept();
            if (!source1.Any<Pawn>())
                return;
            letterText += "\n\n" + charityInfo + "\n\n" + "PawnsHaveCharitableBeliefs".Translate() + ":";
            foreach (IGrouping<Ideo, Pawn> source2 in source1.GroupBy<Pawn, Ideo>((Func<Pawn, Ideo>)(c => c.Ideo)))
                letterText += "\n  - " + "BelieversIn".Translate((NamedArgument)source2.Key.name.Colorize(source2.Key.Color), (NamedArgument)source2.Select<Pawn, string>((Func<Pawn, string>)(f => f.NameShortColored.Resolve())).ToCommaList(false, false));
        }
    }
}
