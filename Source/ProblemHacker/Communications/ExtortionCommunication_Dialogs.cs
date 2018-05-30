using Verse;
using RimWorld;

namespace ProblemHacker
{
	public static class ExtortionCommunication_Dialogs
	{
		public static DiaNode MakeExtortionRootNode(ProblemWorker_Hacker problemWorker)
		{
			return new DiaNode ("ProblemHackerExtortionDemands".Translate ()) {
				text = (problemWorker.alliedBase != null) ? "ProblemHackerExtortionDemandsText".Translate(problemWorker.alliedBase.Name, problemWorker.hacker.Name, problemWorker.currentExtortionAmount) :
					"ProblemHackerExtortionDemandsTextNoAllies".Translate(problemWorker.hacker.Name, problemWorker.currentExtortionAmount),
				options = {
					new DiaOption ("ProblemHackerDialogOptionText_ExtortionRootNode_AcceptExtortion".Translate ()) {
						linkLateBind = delegate {
							problemWorker.ExtortionAccepted ();
							return MakeExtortionAcceptedNode (problemWorker);
						},
						disabled = !problemWorker.PlayerHasExtortionAmount(),
						disabledReason = "NeedSilverLaunchable".Translate(problemWorker.currentExtortionAmount.ToString())
					},
					new DiaOption ("ProblemHackerDialogOptionText_ExtortionRootNode_RejectExtortion".Translate ()) {
						linkLateBind = delegate {
							problemWorker.ExtortionRejected ();
							return MakeExtortionRejectedNode (problemWorker);
						}
					},
					new DiaOption ("ProblemHackerDialogOptionText_ExtortionRootNode_Threaten".Translate (problemWorker.hacker.NameStringShort)) {
						linkLateBind = delegate {
							problemWorker.ExtortionRejectedInsulted ();
							return MakeExtortionRejectedInsultedNode (problemWorker);
						}
					},
					new DiaOption ("ProblemHackerDialogOptionText_ExtortionRootNode_Ignore".Translate ()) {
						resolveTree = true
					}
				}
			};
		}

		public static DiaNode MakeExtortionAcceptedNode(ProblemWorker_Hacker problemWorker)
		{
			return new DiaNode ("ProblemHackerExtortionAccepted".Translate ()) {
				text = "ProblemHackerExtortionAcceptedText".Translate(problemWorker.hacker.Name),
				options = {
					new DiaOption ("(" + "Disconnect".Translate () + ")") {
						resolveTree = true,
						action = delegate {
							TradeUtility.LaunchSilver(problemWorker.rootMap, problemWorker.currentExtortionAmount);
						}
					}
				}
			};
		}

		public static DiaNode MakeExtortionRejectedNode(ProblemWorker_Hacker problemWorker)
		{
			return new DiaNode ("ProblemHackerExtortionRejected".Translate ()) {
				text = "ProblemHackerExtortionRejectedText".Translate(),
				options = {
					new DiaOption ("(" + "Disconnect".Translate () + ")") {
						resolveTree = true
					}
				}
			};
		}

		public static DiaNode MakeExtortionRejectedInsultedNode(ProblemWorker_Hacker problemWorker)
		{
			return new DiaNode ("PH_Extortion_Insult_Title".Translate ()) {
				text = "PH_Extortion_Insult_Text".Translate (),
				options = {
					new DiaOption ("(" + "Disconnect".Translate () + ")") {
						resolveTree = true
					}
				}
			};
		}
	}
}

