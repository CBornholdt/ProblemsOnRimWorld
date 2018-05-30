using System;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ProblemHacker
{
	//Needs to be a world component because problems will occasionally hold things
	public class ProblemManager : WorldComponent
	{
		public List<ProblemWorker> problems = new List<ProblemWorker>();

		public ProblemManager(World world) : base(world)
		{
		}

		public override void ExposeData() 
		{	
			Scribe_Collections.Look<ProblemWorker> (ref problems, "Problems", LookMode.Deep);
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			foreach (var problem in problems)
				problem.UpdateTick();
		}
	}
}
