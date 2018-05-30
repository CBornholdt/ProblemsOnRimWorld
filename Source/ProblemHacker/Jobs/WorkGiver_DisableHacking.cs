using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace ProblemHacker
{
	public class WorkGiver_DisableHacking : WorkGiver_Scanner
	{
		public override Verse.ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForGroup (ThingRequestGroup.BuildingArtificial);
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			Building building = thing as Building;
			return (building != null) && forced && building.TryGetComp<CompHacked>() != null;
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			return new Job (DefDatabase<JobDef>.GetNamed ("DisableHacking"), thing);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.listerBuildings.allBuildingsColonist.Where (b => b.TryGetComp<CompHacked>() != null).Cast<Thing>();
		}
	}
}

