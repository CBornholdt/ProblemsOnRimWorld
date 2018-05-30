using System;
using Verse;
using System.Linq;

namespace ProblemHacker
{
	public class WeakContactAttempt : Communication 
	{
		ProblemWorker_Hacker pw;

		private static readonly int TotalNumResponseTexts = 3;

		public WeakContactAttempt()
		{
		}

		public WeakContactAttempt(ProblemWorker_Hacker pw) : base(pw.rootMap.uniqueID)
		{
			this.pw = pw;
		}

		public override string GetCallLabel()
		{
			return "PH_WCA_CallLabel".Translate(pw.alliedBase.Name);
		}

		public override void ExposeData()
		{
			Scribe_References.Look<ProblemWorker_Hacker> (ref this.pw, "ProblemWorkerHacker");
		}

		public override void OpenCommunications(Pawn negotiator)
		{
			Dialog_NodeTree dialog = new Dialog_NodeTree (MakeAnswerWeakContactAttemptRootNode (negotiator), 
				false, false, "PH_WCA_RootTitle".Translate ());
			Find.WindowStack.Add (dialog);
			FindExt.DynamicCommManagerFor (pw.rootMap).RemoveCommunication (this);
		}

		public static string GarbleText(string text)
		{
			return new string(text.Select(c => Rand.Chance (0.8f) ? '*' : c).ToArray());
		}

		public string GetResponseNumString()
		{
			return ((int)Math.Min (TotalNumResponseTexts, pw.numWeakContactResponses)).ToString ();
		}

		public DiaNode MakeAnswerWeakContactAttemptRootNode(Pawn negotiator)
		{
			DiaNode node = new DiaNode ("PH_WCA_RootTitle".Translate ()) {
				text = GarbleText((("PH_WCA_RootText" + GetResponseNumString()).Translate (this.pw.alliedBase.Name, this.pw.hacker.Name)))
			};
			node.options.Add (new DiaOption ("PH_WCA_Root_RespondOption".Translate (negotiator.Name)) { 
				link = new DiaNode ("PH_WCA_Respond_Title") {
					text = GarbleText("PH_WCA_Respond_Text".Translate(negotiator.Name)),
					options = {
						new DiaOption("PH_WCA_Respond_ConcernedOption".Translate()) {
							resolveTree = true
						}
					}
				}
			});
			node.options.Add(new DiaOption("PH_WCA_Root_ConcernedOption".Translate()) {
				resolveTree = true
			});
			node.options.Add(new DiaOption("PH_WCA_Root_IgnoreOption".Translate()) {
				resolveTree = true
			});
			return node;
		}
	}
}

