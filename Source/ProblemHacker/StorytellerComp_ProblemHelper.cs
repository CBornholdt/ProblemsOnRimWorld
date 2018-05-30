using System;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProblemHacker
{
	//This helper is because I cannot implement ExposeData in the StorytellerComp_Problem class itself, so I expose the data here

	public class StorytellerComp_ProblemHelper : IExposable
	{
		public List<ProblemWorker> problems = new List<ProblemWorker>();

		public StorytellerComp_ProblemHelper()
		{
			Log.Message ("Creating " + GetHashCode ());
		}

		public void ExposeData() 
		{	//Funky as Scribe_Collections does not support polymorphic loading
			int pCount = 0;
			if(Scribe.mode == LoadSaveMode.Saving) 
				pCount = problems?.Count ?? 0;
			Scribe_Values.Look<int> (ref pCount, "ProblemCount", 0);
			for (int c = 0; c < pCount; c++) {
				IncidentDef d = default(IncidentDef);
				if(Scribe.mode == LoadSaveMode.Saving)
					d = problems [c].def;
				Scribe_Defs.Look<IncidentDef> (ref d, "ProblemIncidentDef");
				ProblemWorker pw;
				if(Scribe.mode == LoadSaveMode.LoadingVars) 
					pw = (ProblemWorker)Activator.CreateInstance(d.workerClass);
				else 
					pw = problems[c];
				Scribe_Deep.Look<ProblemWorker> (ref pw, "ProblemSpecificData");
				if(Scribe.mode == LoadSaveMode.LoadingVars) {
					pw.def = d;
					problems.Add(pw);
				}
			}
		}
	}

	public static class ProblemHelper	//Singleton helper as I can't add to Storyteller directly
	{
		public static StorytellerComp_ProblemHelper compHelperInstance = new StorytellerComp_ProblemHelper();
	}
}

