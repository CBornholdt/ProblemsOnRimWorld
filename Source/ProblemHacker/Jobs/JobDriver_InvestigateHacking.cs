﻿using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProblemHacker
{
	public class JobDriver_InvestigateHacking : JobDriver
	{
		private float workLeft;

		private float totalWorkNeeded;

		public override bool TryMakePreToilReservations(){
			return this.pawn.Reserve (this.job.targetA, this.job, 1, -1, null);
		}

		private ProblemWorker_Hacker GetProblemWorker()
		{
			return ProblemUtility.GetProblemOfTypeOn<ProblemWorker_Hacker> (TargetThingA.Map);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float> (ref this.workLeft, "WorkLeft");
			Scribe_Values.Look<float> (ref this.totalWorkNeeded, "totalWorkNeeded");
		}

		//[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils(){
			ProblemWorker_Hacker pw = GetProblemWorker();
			if (pw == null) 
				yield break;
			this.AddEndCondition (() => pw.IsRunning () && !pw.HasHackingStopped() && (TargetThingA.TryGetComp<CompHacked>()?.investigatable ?? false)
				? JobCondition.Ongoing : JobCondition.Incompletable);
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Reserve.Reserve (TargetIndex.A, 1);
			yield return Toils_Goto.GotoThing (TargetIndex.A, PathEndMode.Touch);
			Toil doWork = new Toil().FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			doWork.initAction = () => { 
				this.totalWorkNeeded = 300f; 
				this.workLeft = this.totalWorkNeeded;
			};
			doWork.tickAction = () => {
				this.workLeft -= this.pawn.GetStatValue(StatDefOf.ResearchSpeed, true);
				this.pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(5f, false);
				if(this.workLeft <= 0f)
					doWork.actor.jobs.curDriver.ReadyForNextToil();
			};
			doWork.defaultCompleteMode = ToilCompleteMode.Never;
			doWork.WithProgressBar(TargetIndex.A, () => 1f - this.workLeft/this.totalWorkNeeded, false, -0.5f);
			yield return doWork;
			Toil foundEm = new Toil ();
			foundEm.defaultCompleteMode = ToilCompleteMode.Instant;
			foundEm.initAction = delegate {
				ResearchProjectDef current = Find.ResearchManager.currentProj;
				Find.ResearchManager.currentProj = DefDatabase<ResearchProjectDef>.GetNamed("HackingCountermeasures");
				Find.ResearchManager.ResearchPerformed(500 / 0.007f, null);	//Constant from inside ResearchManager
				Find.ResearchManager.currentProj = current;
				this.pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(500f, false);
				TargetThingA.TryGetComp<CompHacked>().investigatable = false;
				Find.LetterStack.ReceiveLetter ("ProblemHackerInvestigateHackingLetterLabel".Translate (), 
					"ProblemHackerInvestigateHackingLetterText".Translate (this.pawn.NameStringShort), LetterDefOf.PositiveEvent);
			};
			yield return foundEm;
		}
	}
}

