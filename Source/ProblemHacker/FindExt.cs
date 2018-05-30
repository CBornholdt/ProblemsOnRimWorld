using System;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	public static class FindExt
	{
		public static ResearchTaskManager ResearchTaskManager {
			get {
				return Current.Game.GetComponent<ResearchTaskManager> ();
			}
		}

		public static DynamicComponentManager DynamicComponentManager {
			get {
				return Find.World.GetComponent<DynamicComponentManager> ();
			}
		}

		public static ProblemManager ProblemManager {
			get {
				return Find.World.GetComponent<ProblemManager>();
			}
		}

		public static DynamicCommManager DynamicCommManagerFor(Map map)
		{
			return map.GetComponent<DynamicCommManager>();
		}
	}
}

