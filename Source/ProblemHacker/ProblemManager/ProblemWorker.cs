using System;
using RimWorld;
using Verse;
using System.Linq;

namespace ProblemHacker
{
	public abstract class ProblemWorker : IncidentWorker, IExposable, ILoadReferenceable
	{
		protected IIncidentTarget target;
		protected IncidentParms parms;

		public bool IsRealInstance {
			get {
				return ProblemUtility.AllProblems.Contains(this);
			}
		}

		public StorytellerComp_Problem StoryComp { 
			get {
				return Find.Storyteller.storytellerComps.OfType<StorytellerComp_Problem>().First();
			}
		}

		public bool IsRunning() => ProblemUtility.AllProblems.Where(p => p.def == this.def).Any();

		public bool IsRunningOn(IIncidentTarget target)
		{
			return ProblemUtility.AllProblems.Where(p => p.def == this.def && p.target == target).Any();
		}

		public virtual void ExposeData()
		{
			Scribe_References.Look<IIncidentTarget>(ref this.target, "ProblemWorkerTarget");
			Scribe_Deep.Look<IncidentParms> (ref this.parms, "ProblemWorkerParms");
			if(Scribe.mode == LoadSaveMode.PostLoadInit)
				this.Initialize();
		}

		public virtual void UpdateTick() {}

		public abstract string GetUniqueLoadID();

		protected abstract bool TryStartProblem ();

		protected abstract bool CanStartNowFor(IIncidentTarget target);

		protected virtual void Initialize() {}

		//Overridden
		protected override bool CanFireNowSub(IIncidentTarget target) => StoryComp != null && CanStartNowFor(target);

		protected override bool TryExecuteWorker (IncidentParms parms)
		{
			ProblemWorker realWorker = CreateStoredInstance(parms);
			realWorker.Initialize();
			bool result = realWorker.TryStartProblem();
			if (!result)
				realWorker.Finish();

			return result;
		}

		protected virtual void Finish() => ProblemUtility.AllProblems.Remove(this);

		private ProblemWorker CreateStoredInstance(IncidentParms parms)
		{
			ProblemWorker p = (ProblemWorker)Activator.CreateInstance(this.def.workerClass);
			p.target = parms.target;
			p.def = def;
			p.parms = parms;
			ProblemUtility.AllProblems.Add(p);
			return p;
		}
	}
}

