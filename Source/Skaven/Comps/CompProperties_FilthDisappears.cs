using Verse;

namespace TheEndTimes_Skaven
{

	internal class CompProperties_FilthDisappears : CompProperties
	{

		public int disappearsAfterRareTicks = 30;
		public ThingDef filthLeaving;
		public bool disappearsInRain = true;
		public float rainMultiplier = 2f;


		public CompProperties_FilthDisappears()
		{
			compClass = typeof(Comp_FilthDisappears);
		}
	}
}
