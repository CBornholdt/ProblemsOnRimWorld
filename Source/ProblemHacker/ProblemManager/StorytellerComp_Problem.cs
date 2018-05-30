using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ProblemHacker
{
	public class StorytellerComp_Problem : StorytellerComp
	{
		public override IEnumerable<FiringIncident> MakeIntervalIncidents (IIncidentTarget target) => Enumerable.Empty<FiringIncident>();

		public StorytellerComp_Problem()
		{	

		}
	}
}

