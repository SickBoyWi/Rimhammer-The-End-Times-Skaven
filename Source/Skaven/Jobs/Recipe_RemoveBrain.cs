using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace TheEndTimes_Skaven
{
    public class Recipe_RemoveBrain : Recipe_Surgery
    {
        protected virtual bool SpawnPartsWhenRemoved
        {
            get
            {
                return false;
            }
        }

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(
          Pawn pawn,
          RecipeDef recipe)
        {
            yield return pawn.health.hediffSet.GetBrain();
        }

        public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
        {
            return (pawn.Faction != billDoerFaction && pawn.Faction != null || pawn.IsQuestLodger());
        }

        public override void ApplyOnPawn(
          Pawn pawn,
          BodyPartRecord part,
          Pawn billDoer,
          List<Thing> ingredients,
          Bill bill)
        {
            bool flag1 = MedicalRecipesUtility.IsClean(pawn, part);
            bool flag2 = this.IsViolationOnPawn(pawn, part, Faction.OfPlayer);
            if (billDoer != null)
            {
                if (this.CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                    return;
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, (object)billDoer, (object)pawn);
                
                GenSpawn.Spawn(SkavenDefOf.RH_TET_Skaven_MorskittarControl, billDoer.Position, billDoer.Map, WipeMode.Vanish);
            }
            this.DamagePart(pawn, part);
            if (flag1)
                this.ApplyThoughts(pawn, billDoer);
            if (!flag2)
                return;
            this.ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
        }

        public virtual void DamagePart(Pawn pawn, BodyPartRecord part)
        {
            pawn.TakeDamage(new DamageInfo(DamageDefOf.SurgicalCut, 99999f, 999f, -1f, (Thing)null, part, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null, true, true));
        }

        public virtual void ApplyThoughts(Pawn pawn, Pawn billDoer)
        {
            if (pawn.Dead)
                ThoughtUtility.GiveThoughtsForPawnExecuted(pawn, billDoer, PawnExecutionKind.OrganHarvesting);
            else
                ThoughtUtility.GiveThoughtsForPawnOrganHarvested(pawn, billDoer);
        }

        public override string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part)
        {
            if (!part.def.removeRecipeLabelOverride.NullOrEmpty())
                return part.def.removeRecipeLabelOverride;
            if (pawn.RaceProps.IsMechanoid || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))
                return RecipeDefOf.RemoveBodyPart.label;
            return (string)"RH_TET_Skaven_RemoveBrain".Translate();
        }
    }
}