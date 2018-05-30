using Verse;

namespace ProblemHacker
{
	public class ContactHackedAllyBase : Communication 
	{
		ProblemWorker_Hacker pw;

		public ContactHackedAllyBase()
		{
		}

		public ContactHackedAllyBase(ProblemWorker_Hacker pw) : base(pw.rootMap.uniqueID)
		{
			this.pw = pw;
		}

		public override string GetCallLabel()
		{
			return "ProblemHackerContactHackedAllyBase".Translate(pw.alliedBase.Name);
		}

		public override void ExposeData()
		{
			Scribe_References.Look<ProblemWorker_Hacker> (ref this.pw, "ProblemWorkerHacker");
		}

		public override void OpenCommunications(Pawn negotiator)
		{
			Dialog_NodeTree dialog = new Dialog_NodeTree (ContactHackedAllyBase_Dialogs.MakeContactHackedAllyBaseRootNode (pw), 
				false, false, "ProblemHackerContactHackedAllyBaseTitle".Translate (pw.alliedBase.Name));
			Find.WindowStack.Add (dialog);
			FindExt.DynamicCommManagerFor (pw.rootMap).RemoveCommunication (this);
		}
	}
}

