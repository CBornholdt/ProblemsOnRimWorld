using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	//This class manipulates ResearchProjectDefs and thus needs to be viewed with suspicion, deep deep-cloned suspicion
	public class ResearchTaskManager : GameComponent
	{
		private List<ResearchProjectDef> unlockedTasks = new List <ResearchProjectDef> ();
		private List<ResearchProjectDef> finishedTasks = new List <ResearchProjectDef> ();
		private Dictionary<ResearchProjectDef, List<Action>> completionActions = new Dictionary<ResearchProjectDef, List<Action>> ();

		public IEnumerable<ResearchProjectDef> AllResearchTasks {
			get {	//Need to access private researchMods field using reflection
				var researchMods = typeof(ResearchProjectDef).GetField ("researchMods", 
					                   BindingFlags.Instance | BindingFlags.NonPublic);
				return DefDatabase<ResearchProjectDef>.AllDefsListForReading
					.Where (p => ((List<ResearchMod>)researchMods.GetValue (p))?.OfType<ResearchTaskMod>().Any() ?? false);
			}
		}

		public ResearchTaskManager(Game game)
		{
		}

		public void LockResearch(ResearchProjectDef project)
		{
			if (project.prerequisites == null)
				project.prerequisites = new List<ResearchProjectDef> ();

			if (!project.prerequisites.Contains (project)) 
				project.prerequisites.Add (project);

			if (Find.ResearchManager.currentProj == project)
				Find.ResearchManager.currentProj = null;

			Log.Message ("Locking " + project.defName);
		}

		public void UnlockResearch(ResearchProjectDef project)
		{
			project.prerequisites?.Remove (project);

			if(!unlockedTasks.Contains(project))
				unlockedTasks.Add(project);
		}

		public void ResetResearch(ResearchProjectDef project, bool unlockAsWell = false)
		{
			ResearchProjectDef currentProject = Find.ResearchManager.currentProj;
			Find.ResearchManager.currentProj = project;

			//Constant is inverse from ResearchManager.ResearchPerformed
			Find.ResearchManager.ResearchPerformed (project.ProgressReal / -0.007f, null);
			finishedTasks.Remove (project);
			Find.ResearchManager.currentProj = currentProject;

			if (unlockAsWell)
				UnlockResearch (project);
		}

		public void RegisterResearchCompletionAction(ResearchProjectDef task, Action completionAction)
		{
			List<Action> actions;
			if (completionActions.TryGetValue (task, out actions))
				actions.Add (completionAction);
			else
				completionActions.Add (task, new List<Action>(){ completionAction });

		//	Log.Message ("Registered " + completionActions [task].Count + " for " + task.defName);
		//	Log.Message ("Total registered " + completionActions.Values.Where (l => l != null).Select (l => l.Count).Sum ());
		}

		public void UnregisterResearchCompletionAction(ResearchProjectDef task, Action completionAction)
		{
			List<Action> actions;
			if (completionActions.TryGetValue (task, out actions))
				actions.RemoveAll (a => a == completionAction ||
					(a.Method == completionAction.Method && a.Target == completionAction.Target));
			else
				Log.Message("Attempted to unregister a completation Action for project " + task.defName + " without any completion actions registered");
		}

		internal void ResearchTaskCompleted()
		{
			//Log.Message ("Total registered " + completionActions.Values.Select (l => l?.Count ?? 0).Sum ());
			foreach(ResearchProjectDef justFinishedTask in AllResearchTasks.Where(p => p.IsFinished && !finishedTasks.Contains(p))) {
				finishedTasks.Add(justFinishedTask);
				List<Action> actions;
				if (this.completionActions.TryGetValue (justFinishedTask, out actions)) 
					actions.ForEach (a => a.Invoke ());
			}
		}
			
		public override void ExposeData()
		{
			Scribe_Collections.Look<ResearchProjectDef>(ref this.unlockedTasks, "UnlockedTasks", LookMode.Def);
		//	if (this.unlockedTasks == null)		//Scribe_Collections LookMode.Def if main node is not found will set value to null
		//		this.unlockedTasks = new List<ResearchProjectDef> ();
		}

		public override void FinalizeInit()
		{
			foreach(ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
				project.prerequisites?.Remove(project);

			foreach(ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(
				p => (p.tags?.Contains("Locked") ?? false) && !(this.unlockedTasks.Contains(p))))
				LockResearch(project);

			this.finishedTasks = AllResearchTasks.Where(p => p.IsFinished).ToList();
		}
	}

	public class ResearchTaskMod : ResearchMod
	{
		public override void Apply()
		{
			FindExt.ResearchTaskManager.ResearchTaskCompleted ();
		}
	}
}

