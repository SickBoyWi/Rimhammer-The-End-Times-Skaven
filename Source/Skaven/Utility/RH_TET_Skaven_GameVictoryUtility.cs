using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheEndTimes_Skaven
{
    public static class RH_TET_Skaven_GameVictoryUtility
    {
        private static void ShowCredits(string victoryText)
        {
            Screen_Credits screen_Credits = new Screen_Credits(victoryText);
            screen_Credits.wonGame = true;
            Find.WindowStack.Add(screen_Credits);
            Find.MusicManagerPlay.ForceSilenceFor(999f);
            ScreenFader.StartFade(Color.clear, 3.0f);
        }

        private static void InitiateVictory(Map map)
        {
            SkavenDefOf.RH_TET_Skaven_MorskittarFire.PlayOneShotOnCamera((Map)null);
            DefDatabase<SoundDef>.GetNamed("PsychicPulseGlobal", true).PlayOneShot((SoundInfo)new TargetInfo(map.Center, map, false));
            ScreenFader.StartFade(Color.green, 5.0f);
        }

        public static void PlayerVictory(Map map)
        {
            InitiateVictory(map);
            string victoryText = "RH_TET_Skaven_GameOverCannonShot".Translate();
            ShowCredits(victoryText);
        }
    }
}
