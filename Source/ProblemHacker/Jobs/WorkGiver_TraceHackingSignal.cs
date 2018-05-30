using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace ProblemHacker
{
	public class WorkGiver_TraceHackingSignal : WorkGiver_Scanner
	{
		private bool CanTraceHackingFrom(Building building)
		{
			return (building.GetComp<CompHacked>() != null) && (building.GetComp<CompPowerTrader> ()?.PowerOn ?? false) && !(building is Building_TurretGun);
		}

		public override Verse.ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForGroup (ThingRequestGroup.BuildingArtificial);
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			Building building = thing as Building;
			return (building != null) && forced && CanTraceHackingFrom (building);
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			return new Job (DefDatabase<JobDef>.GetNamed ("TraceHackingSignal"), thing);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.listerBuildings.allBuildingsColonist.Where (CanTraceHackingFrom).Cast<Thing>();
		}
	}
}

