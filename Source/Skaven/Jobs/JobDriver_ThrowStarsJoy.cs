using RimWorld;
using UnityEngine;
using Verse;

namespace TheEndTimes_Skaven
{
    public class JobDriver_ThrowStarsJoy : JobDriver_WatchBuilding
    {
        private const int StarThrowInterval = 400;

        protected override void WatchTickAction(int delta)
        {
            if (this.pawn.IsHashIntervalTick(400))
                JobDriver_ThrowStarsJoy.ThrowObjectAt(this.pawn, this.TargetA.Cell, SkavenDefOf.RH_TET_Skaven_MoteStar);
            base.WatchTickAction(delta);
        }

        private static void ThrowObjectAt(Pawn thrower, IntVec3 targetCell, ThingDef mote)
        {
            if (!thrower.Position.ShouldSpawnMotesAt(thrower.Map) || thrower.Map.moteCounter.Saturated)
                return;
            float speed = Rand.Range(3.8f, 5.6f);
            Vector3 vector3 = targetCell.ToVector3Shifted() + Vector3Utility.RandomHorizontalOffset((float)((1.0 - (double)thrower.skills.GetSkill(SkillDefOf.Shooting).Level / 20.0) * 1.79999995231628));
            vector3.y = thrower.DrawPos.y;
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(mote, (ThingDef)null);
            moteThrown.Scale = 1f;
            moteThrown.rotationRate = (float)Rand.Range(-300, 300);
            moteThrown.exactPosition = thrower.DrawPos;
            moteThrown.SetVelocity((vector3 - moteThrown.exactPosition).AngleFlat(), speed);
            moteThrown.airTimeLeft = (float)Mathf.RoundToInt((moteThrown.exactPosition - vector3).MagnitudeHorizontal() / speed);
            GenSpawn.Spawn((Thing)moteThrown, thrower.Position, thrower.Map, WipeMode.Vanish);
        }
    }
}