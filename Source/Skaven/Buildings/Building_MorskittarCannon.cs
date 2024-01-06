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
    public class Building_MorskittarCannon : Building
    {
        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();

            // Add the inspections string from the base
            stringBuilder.Append(base.GetInspectString());

            List<Building> buildingsOnCannon = SkavenUtil.CannonBuildingsAttachedTo(this, false);
            Building control = null;
            foreach (Building b in buildingsOnCannon)
            {
                if (b.def.defName != null && b.def.defName.Equals("RH_TET_Skaven_Morskittar_Control"))
                {
                    control = b;
                    break;
                }
            }

            if (control != null)
            {
                stringBuilder.Append(control.GetInspectString());
            }

            // return the complete string
            return stringBuilder.ToString();
        }
    }
}
