using System;
using System.Text;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	public class HackedAllyFactionCommunication : FactionCommunication
	{
		ProblemWorker_Hacker pw;

		public HackedAllyFactionCommunication () : base(0) {}

		public HackedAllyFactionCommunication( ProblemWorker_Hacker pw) : base(pw.ID)
		{
			this.pw = pw;
		}

		public override void ExposeData()
		{
			base.ExposeData ();
			Scribe_References.Look<ProblemWorker_Hacker> (ref this.pw, "PW");
		}

		public override bool ForFaction(Faction faction)
		{
			return faction == pw.HackedAlliedFaction;
		}

		public override string TransformParentText(string text)
		{
			StringBuilder sb = new StringBuilder (text);
			sb.AppendLine ();
			sb.Append ("PH_HackedAllyFC_ParentText".Translate (pw.alliedBase.Name));
			return sb.ToString();
		}

		public override DiaOption GetOption (Faction faction, Pawn negotiator)
		{
			return new DiaOption ("PH_HackedAllyFC_ParentOption".Translate (faction.Name, pw.alliedBase.Name)) {
				linkLateBind = delegate {
					FindExt.DynamicCommManagerFor(pw.rootMap).RemoveFactionCommunication(this);
					return MakeHackedAllyResponse (faction, negotiator);
				}
			};
		}

		public DiaNode MakeHackedAllyResponse(Faction faction, Pawn negotiator)
		{
			return new DiaNode("PH_HackedAllyFC_RootTitle".Translate()) {
				options = {
					new DiaOption("(" + "Disconnect".Translate() + ")") {
						resolveTree = true
					}
				}
			};
		}
	}
}

