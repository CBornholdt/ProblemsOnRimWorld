using Verse;

namespace ProblemHacker
{
	public static class ContactHackedAllyBase_Dialogs
	{
		public static DiaNode MakeContactHackedAllyBaseRootNode(ProblemWorker_Hacker problemWorker)
		{
			DiaNode node = new DiaNode ("ProblemHackerHackedAllyBaseRootNodeTitle".Translate (problemWorker.alliedBase.Name)) {
				text = "ProblemHackerHackedAllyBaseRootNodeText".Translate (),
			};
			if (problemWorker.foundHackingSite)
				node.options.Add (new DiaOption ("ProblemHackerHackedAllyBaseOption_OfferInformation".Translate ()) {
					link = new DiaNode ("ProblemHackerHackedAllyBase_OfferedInformationNodeTitle".Translate ()) {
						text = "ProblemHackerHackedAllyBase_OfferedInformationNodeText".Translate (),
						options = {
							new DiaOption ("(" + "disconnect".Translate () + ")") {
								resolveTree = true
							}
						}
					}
				});
			else
				node.options.Add (new DiaOption ("ProblemHackerHackedAllyBaseOption_AskForInformation".Translate ()) {
					link = new DiaNode ("ProblemHackerHackedAllyBase_AskedForInformationNodeTitle".Translate ()) {
						text = "ProblemHackerHackedAllyBase_AskedForInformationNodeText".Translate (),
						options = {
							new DiaOption ("(" + "disconnect".Translate () + ")") {
								resolveTree = true
							}
						}
					}
				});
			return node;
		}

/*		public static DiaOption MakeContactHackedAllyBaseOption()
		{
			if(this.foundHackingSite)
				return new DiaOption ("ProblemHackerDialogOptionText_ContactAlly_HackerFound".Translate ()) {
					link = new DiaNode ("ProblemHackerDialogText_ResponseFromContactedAlly_HackerFound".Translate ()) {
						options = {
							MakeResetToRootOption ()
						}
					}
				};
			else
				return new DiaOption ("ProblemHackerDialogOptionText_ContactAlly_HackerNotFound".Translate ()) { 
					link = new DiaNode ("ProblemHackerDialogText_ResponseFromContactedAlly_HackerNotFound".Translate ()) {
						options = {
							MakeResetToRootOption ()
						}
					}
				};
		}	*/
	}
}

