using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using TheEndTimes_Magic;
using Verse;
using Verse.AI.Group;

namespace TheEndTimes_Skaven
{
    [StaticConstructorOnStartup]
    public partial class HarmonyPatches
    {
        // This will cause all patches in the mod to run.
        static HarmonyPatches()
        {
            var harmony = new Harmony("rimworld.theendtimes.skaven");

            harmony.Patch(original: AccessTools.Method(
                    type: typeof(Pawn),
                    name: "PreApplyDamage"),
                    prefix: null,
                    postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_HealthTracker_PostApplyDamage_Postfix)));

            harmony.Patch(original: AccessTools.Method(
                    type: typeof(Faction),
                    name: "TryMakeInitialRelationsWith"),
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Faction_TryMakeInitialRelationsWith_PreFix)),
                    postfix: null);

            harmony.Patch(original: AccessTools.Method(
                    type: typeof(ThingSetMaker_Meteorite),
                    name: "FindRandomMineableDef"),
                    prefix: null,
                    postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ThingSetMaker_Meteorite_FindRandomMineableDef_Postfix)));

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void ThingSetMaker_Meteorite_FindRandomMineableDef_Postfix(ref ThingDef __result)
        {
            if (RH_TET_SkavenMod.EndGameProcessing)
                __result = SkavenDefOf.RH_TET_Skaven_MineableWarpstone;
        }

        public static void EdgeFromList(List<IntVec3> cellExport, out int height, out int width)
        {
            height = 0;
            width = 0;
            IntVec3 intVec3_1 = cellExport.First<IntVec3>();
            foreach (IntVec3 intVec3_2 in cellExport)
            {
                if (intVec3_1.z == intVec3_2.z)
                    ++width;
            }
            foreach (IntVec3 intVec3_2 in cellExport)
            {
                if (intVec3_1.x == intVec3_2.x)
                    ++height;
            }
        }

        private static void Faction_TryMakeInitialRelationsWith_PreFix(Faction __instance, Faction other)
        {
            Faction playerFaction = __instance;

            // Makes all factions hostile to beastmen.
            if (playerFaction.IsPlayer && !other.IsPlayer && (playerFaction.def.defName.Equals("RH_TET_Skaven_PlayerFaction")
                || playerFaction.def.defName.Equals("RH_TET_Skaven_PlayerWizard")))
            {
                if (other.def.defName.Equals("RH_TET_Beastmen_GorFaction") 
                    || other.def.defName.Equals("RH_TET_Skaven_UnderEmpireFaction")) // Could be friendly with beastmen or skaven.
                    other.def.permanentEnemy = false;
                if (other.def.defName.Equals("RH_TET_Dwarf_KarakMountain") // Empire and Dwarfs always hate them.
                     || other.def.defName.Equals("RH_TET_EmpireOfMan"))
                    other.def.permanentEnemy = true;
            }
        }

        private static void Pawn_HealthTracker_PostApplyDamage_Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (dinfo.Weapon != null && dinfo.Weapon.defName != null && dinfo.Weapon.defName.Equals("RH_TET_Skaven_CenserHigh"))//dinfo != null && 
            {
                    IntVec3 position = __instance.Position;
                    Map map = __instance.Map;

                    Thing thing = ThingMaker.MakeThing(RH_TET_MagicDefOf.RH_TET_PoisonGas, (ThingDef)null);
                    GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Direct, (Action<Verse.Thing, int>)null, (Predicate<IntVec3>)null, new Rot4());
                    (thing as GasCloud).ReceiveConcentration(3000);
            }
        }
    }
}