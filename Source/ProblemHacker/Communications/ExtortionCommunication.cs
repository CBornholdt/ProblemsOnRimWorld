using Verse;

namespace ProblemHacker
{
	public class ExtortionCommunication : Communication 
	{
		ProblemWorker_Hacker pw;

		public ExtortionCommunication ()
		{
		}

		public ExtortionCommunication(ProblemWorker_Hacker pw) : base(pw.rootMap.uniqueID)
		{
			this.pw = pw;
		}

		public override string GetCallLabel()
		{
			return "AnswerCommunication".Translate();
		}

		public override void ExposeData()
		{
			Scribe_References.Look<ProblemWorker_Hacker> (ref this.pw, "ProblemWorkerHacker");
		}

		public override void OpenCommunications(Pawn negotiator)
		{
			Dialog_NodeTree dialog = new Dialog_NodeTree (ExtortionCommunication_Dialogs.MakeExtortionRootNode (pw), 
				false, false, "ProblemHackerExtortionDialogTitle".Translate ());
			Find.WindowStack.Add (dialog);
			FindExt.DynamicCommManagerFor (pw.rootMap).RemoveCommunication (this);
		}
	}
}

