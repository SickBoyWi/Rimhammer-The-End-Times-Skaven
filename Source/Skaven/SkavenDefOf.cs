using HugsLib.Utils;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace TheEndTimes_Skaven
{
    [DefOf]
    public static class SkavenDefOf
    {
        // Factions
        public static FactionDef RH_TET_Skaven_UnderEmpireFaction;

        // Pawn Kinds
        public static PawnKindDef RH_TET_Skaven_SlaveRatMelee;
        public static PawnKindDef RH_TET_Skaven_SlaveRatMissile;
        public static PawnKindDef RH_TET_Skaven_ClanratLow;
        public static PawnKindDef RH_TET_Skaven_ClanratHigh;
        public static PawnKindDef RH_TET_Skaven_WizardScenario;
        public static PawnKindDef RH_TET_Skaven_Assassin;

        // Sounds
        public static SoundDef RH_TET_Skaven_BellRing_Combat;
        public static SoundDef RH_TET_Skaven_BellRing_Noncombat;
        public static SoundDef RH_TET_Skaven_BellRing_Mighty;
        public static SoundDef RH_TET_Skaven_MorskittarFire;

        // Quests / Incidents
        public static IncidentDef RH_TET_Skaven_WandererJoin;

        // Things 
        public static ThingDef RH_TET_Skaven_Warpstone;
        public static ThingDef RH_TET_Skaven_MorskittarControl;
        public static ThingDef RH_TET_Skaven_MineableWarpstone;

        // Thought
        public static ThoughtDef RH_TET_Skaven_BlessedBe;
        public static ThoughtDef RH_TET_Skaven_ForsakenBe;

        // Mote
        public static ThingDef RH_TET_Skaven_MoteStar;

        // =============== Cauldron/Food ===============
        public static ThingDef RH_TET_Beastmen_SlopMeal;
        public static ThingDef RH_TET_Skaven_SlopBucket;
        public static ThingDef RH_TET_Skaven_FeedDispenser;

        // Morskittar
        public static ThingDef RH_TET_Skaven_Morskittar_Cannon;//x1
        public static ThingDef RH_TET_Skaven_Morskittar_Support;//x4
        public static ThingDef RH_TET_Skaven_Morskittar_Control;//x1
        public static ThingDef RH_TET_Skaven_Morskittar_Battery;//x7
    }
}