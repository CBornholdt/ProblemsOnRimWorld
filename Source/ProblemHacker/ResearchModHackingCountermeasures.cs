using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	public class ResearchModHackingCountermeasures : ResearchMod
	{
		public override void Apply()
		{
			foreach (ProblemWorker_Hacker pwHacker in ProblemUtility.AllProblemsOfType<ProblemWorker_Hacker>())
				pwHacker.HackingCountermeasuresImplemented ();

			/*List<ProblemWorker> workers = Find.Storyteller.storytellerComps.OfType<StorytellerComp_Problem>().FirstOrDefault()?.problems;
			if(workers == null)
				return;
			foreach(ProblemWorker_Hacker pw in workers.OfType<ProblemWorker_Hacker>()) 
				if(!pw.HasHackingStopped())
					pw.HackingCountermeasuresImplemented();*/
		}
	}
}

