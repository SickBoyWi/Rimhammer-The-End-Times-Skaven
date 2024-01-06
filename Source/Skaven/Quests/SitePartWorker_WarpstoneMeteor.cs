using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace TheEndTimes_Skaven
{
    public class SitePartWorker_WarpstoneMeteor : SitePartWorker
    {
        public override void Notify_GeneratedByQuestGen(
          SitePart part,
          Slate slate,
          List<Rule> outExtraDescriptionRules,
          Dictionary<string, string> outExtraDescriptionConstants)
        {
            base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
            IEnumerable<ThingDef> thingDefs = slate.Get<IEnumerable<ThingDef>>("itemStashThings", (IEnumerable<ThingDef>)null, false);
            part.things = (ThingOwner)new ThingOwner<Thing>((IThingHolder)part, false, LookMode.Deep);
            outExtraDescriptionRules.Add((Rule)new Rule_String("itemStashContentsValue", "$2,500"));
        }

        public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
        {
            string str = base.GetPostProcessedThreatLabel(site, sitePart);
            if (site.HasWorldObjectTimeout)
                str = (string)(str + (" (" + "DurationLeft".Translate((NamedArgument)site.WorldObjectTimeoutTicksLeft.ToStringTicksToPeriod(true, false, true, true)) + ")"));
            return str;
        }
    }
}
