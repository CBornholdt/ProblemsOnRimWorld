using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	public static class ProblemUtility
	{
		public static List<ProblemWorker> AllProblems {
			get {
				return FindExt.ProblemManager.problems;
			}
		}

		public static StorytellerComp_Problem StoryProblemComp {
			get {
				return Find.Storyteller.storytellerComps.OfType<StorytellerComp_Problem> ().First ();
			}
		}

		public static IEnumerable<ProblemWorker> AllProblemsFor(IIncidentTarget target)
		{
			return AllProblems.Where (x => x.IsRunningOn(target));
		}

		public static IEnumerable<T> AllProblemsOfType<T>() where T : ProblemWorker
		{
			return AllProblems.OfType<T> ();
		}

		public static ProblemWorker GetProblemOn(IncidentDef problemDef, IIncidentTarget target)
		{
			return AllProblems.Where (x => x.IsRunningOn(target) && x.def == problemDef).FirstOrDefault();
		}

		public static T GetProblemOfTypeOn<T>(IIncidentTarget target) where T : ProblemWorker
		{
			return GetProblemOn (DefDatabase<IncidentDef>.AllDefsListForReading.Where(x => x.workerClass ==
				typeof(T)).First(), target) as T;
		}

		public static T GetProblemOfTypeOn<T>(IncidentDef problemDef, IIncidentTarget target) where T : ProblemWorker
		{
			return GetProblemOn (problemDef, target) as T;
		}
	}
}
