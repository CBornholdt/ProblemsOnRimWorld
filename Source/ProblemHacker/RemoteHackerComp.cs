using System;
using Verse;
using RimWorld.Planet;

namespace ProblemHacker
{
	public class RemoteHackerComp : ImportantPawnComp, IThingHolder
	{
		protected override string PawnSaveKey
		{
			get
			{
				return "hacker";
			}
		}

		protected override void RemovePawnOnWorldObjectRemoved()
		{
			for (int i = this.pawn.Count - 1; i >= 0; i--)
			{
				if (!this.pawn[i].Dead)
				{
					this.pawn[i].Kill(null, null);
				}
			}
			this.pawn.ClearAndDestroyContents(DestroyMode.Vanish);
		}

		public override string CompInspectStringExtra()
		{
			if (this.pawn.Any)
			{
				return "Hacker".Translate() + ": " + this.pawn[0].LabelShort;
			}
			return null;
		}
	}
}

