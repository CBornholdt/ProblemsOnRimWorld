using System;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	public class CompHacked : DynamicThingComp
	{
		public Faction hackingFaction;
		public Pawn hacker;
		protected Faction originalFaction;
		public bool investigatable;

		public CompHacked(Faction hackingFaction, Pawn hacker = null, bool investigatable = true)
		{
			this.hackingFaction = hackingFaction;
			this.hacker = hacker;
			this.investigatable = investigatable;
		}

		public CompHacked()
		{
			hackingFaction = Faction.OfPlayer;
			this.originalFaction = Faction.OfPlayer;
			hacker = null;
			this.investigatable = true;
		}

		public override void ExposeData()
		{
			Scribe_References.Look<Faction> (ref hackingFaction, "HackingFaction");
			Scribe_References.Look<Faction> (ref this.originalFaction, "OriginalFaction");
			Scribe_References.Look<Pawn> (ref hacker, "Hacker");
			Scribe_Values.Look<bool> (ref this.investigatable, "Investigatable");
		}

		public override void Added()
		{
			originalFaction = parent.Faction;
			parent.SetFaction (hackingFaction);
		}

		public override void Initialize()
		{
			parent.SetFactionDirect(hackingFaction);
		}

		public override void PostDraw ()
		{
			parent.Map.overlayDrawer.DrawOverlay (this.parent, OverlayTypes.QuestionMark);
		}

		public override void Removed()
		{
			parent.SetFaction (originalFaction);
		}
	}
}

