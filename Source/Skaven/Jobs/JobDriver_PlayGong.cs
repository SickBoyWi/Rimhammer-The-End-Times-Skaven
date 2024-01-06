using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace TheEndTimes_Skaven
{
    public class JobDriver_PlayGong : JobDriver_SitFacingBuilding
    {
        public Building_Gong Gong
        {
            get
            {
                return (Building_Gong)(Thing)this.job.GetTarget(TargetIndex.A);
            }
        }

        protected override void ModifyPlayToil(Toil toil)
        {
            if (!this.pawn.CanReserve(this.Gong))
                return;
            base.ModifyPlayToil(toil);
            toil.AddPreInitAction((Action)(() => this.Gong.StartPlaying(this.pawn)));
            toil.AddFinishAction((Action)(() => this.Gong.StopPlaying()));
        }
    }
}