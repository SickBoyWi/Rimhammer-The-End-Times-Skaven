using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheEndTimes_Skaven
{
    public class HediffComp_SpawnOnDeath : HediffComp
    {
        public HediffCompProperties_SpawnOnDeath Props
        {
            get
            {
                return (HediffCompProperties_SpawnOnDeath)this.props;
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            if (!this.Props.destroyBody)
                return;
            this.Pawn.Corpse.Destroy(DestroyMode.Vanish);
        }

        public override void Notify_PawnKilled()
        {
            this.Pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            this.Pawn.apparel.DestroyAll(DestroyMode.Vanish);
            if (!this.Pawn.Spawned)
                return;
            if (this.Props.fleck != null)
            {
                Vector3 drawPos = this.Pawn.DrawPos;
                for (int index = 0; index < this.Props.moteCount; ++index)
                {
                    Vector2 vector2 = Rand.InsideUnitCircle * this.Props.moteOffsetRange.RandomInRange * (float)Rand.Sign;
                    FleckMaker.Static(new Vector3(drawPos.x + vector2.x, drawPos.y, drawPos.z + vector2.y), this.Pawn.Map, this.Props.fleck, 1f);
                }
            }
            if (this.Props.filth != null)
                FilthMaker.TryMakeFilth(this.Pawn.Position, this.Pawn.Map, this.Props.filth, this.Props.filthCount, FilthSourceFlags.None);
            if (this.Props.sound != null)
                this.Props.sound.PlayOneShot(SoundInfo.InMap((TargetInfo)(Thing)this.Pawn, MaintenanceType.None));
            Pawn pawn = PawnGenerator.GeneratePawn(this.Props.pawnKind, (Faction)null);
            if (pawn == null)
                return;
            pawn.SetFaction(this.GetFaction(), (Pawn)null);
            GenPlace.TryPlaceThing((Thing)pawn, this.Pawn.Position, this.Pawn.Map, ThingPlaceMode.Near, (Action<Thing, int>)null, (Predicate<IntVec3>)null, new Rot4());
        }

        public Faction GetFaction()
        {
            return !this.Props.usePlayerFaction && this.Props.forcedFaction == null ? FactionUtility.DefaultFactionFrom(this.Props.forcedFaction) : Faction.OfPlayer;
        }
    }
}
