using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace TheEndTimes_Skaven
{
    public class CompCannonPart : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            List<Gizmo> gizmos = new List<Gizmo>();

            CompCannonPart compShipPart = this;
            Command_Action commandAction = new Command_Action();
            commandAction.action = new Action(compShipPart.ShowReport);
            commandAction.defaultLabel = (string)"RH_TET_Skaven_CommandMorskittarReport".Translate();
            commandAction.defaultDesc = (string)"RH_TET_Skaven_CommandMorskittarReportDesc".Translate();
            commandAction.hotKey = KeyBindingDefOf.Misc4;
            commandAction.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true);

            gizmos.Add(commandAction);

            return gizmos;
        }

        public void ShowReport()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!SkavenUtil.ShootFailReasons((Building)this.parent).Any<string>())
            {
                stringBuilder.AppendLine((string)"RH_TET_Skaven_CannonpReportCanShoot".Translate());
            }
            else
            {
                stringBuilder.AppendLine((string)"RH_TET_Skaven_CannonReportCannotShoot".Translate());
                foreach (string launchFailReason in SkavenUtil.ShootFailReasons((Building)this.parent))
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(launchFailReason);
                }
            }
            Find.WindowStack.Add((Window)new Dialog_MessageBox((TaggedString)stringBuilder.ToString(), (string)null, (Action)null, (string)null, (Action)null, (string)null, false, (Action)null, (Action)null, WindowLayer.Dialog));
        }
    }
}
