using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TheEndTimes_Skaven
{
    [StaticConstructorOnStartup]
    public class RH_TET_SkavenMod : Mod
    {
        public static System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        public static bool EndGameProcessing = false;

        public RH_TET_SkavenMod(ModContentPack content) : base(content)
        {

        }
    }
}