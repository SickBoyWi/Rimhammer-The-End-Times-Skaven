using RimWorld;
using UnityEngine;
using Verse;

namespace TheEndTimes_Skaven
{
    public class Building_Heater : Building_TempControl
    {
        private const float EfficiencyFalloffSpan = 100f;

        public override void TickRare()
        {
            float ambientTemperature = this.AmbientTemperature;
            float a = GenTemperature.ControlTemperatureTempChange(this.Position, this.Map, (float)((double)this.compTempControl.Props.energyPerSecond * ((double)ambientTemperature >= 20.0 ? ((double)ambientTemperature <= 120.0 ? (double)Mathf.InverseLerp(120f, 20f, ambientTemperature) : 0.0) : 1.0) * 4.16666650772095), this.compTempControl.targetTemperature);
            bool flag = !Mathf.Approximately(a, 0.0f);
            if (flag)
            {
                this.GetRoom(RegionType.Set_All).Temperature += a;
            }
            this.compTempControl.operatingAtHighPower = false;
        }
    }
}
