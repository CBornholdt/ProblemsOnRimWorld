using System;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	public abstract class FactionCommunication : IExposable, ILoadReferenceable
	{
		private int id;

		public FactionCommunication() => this.id = 0; 

		protected FactionCommunication(int id) => this.id = id;

		public abstract bool ForFaction(Faction faction);

		public virtual bool ForPawn(Pawn negotiator) { return true; }

		public virtual string TransformParentText(string text) { return text; }

		public abstract DiaOption GetOption(Faction faction, Pawn negotiator);

		public virtual void ExposeData()
		{
			Scribe_Values.Look<int> (ref this.id, "ID", 0);
		}

		public string GetUniqueLoadID()
		{
			return this.GetType ().ToString() + this.id.ToString();
		}
	}
}
