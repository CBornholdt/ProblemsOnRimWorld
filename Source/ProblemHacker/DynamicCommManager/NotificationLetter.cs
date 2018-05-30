using System;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	public class NotificationLetter : StandardLetter
	{
		public Communication communication;

		protected override string PostProcessedLabel ()
		{
			return this.label;
		}

		public override void ExposeData()
		{
			base.ExposeData ();
			Scribe_References.Look<Communication> (ref this.communication, "Communication");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
				communication.SetNotificationLetter(this);
		}

		public override void OpenLetter()
		{
			// the +1 is due to LetterStack checking early ...
			if (this.disappearAtTick <= Find.TickManager.TicksGame + 1)
				return;	//Do nothing if opened due to timeout
			base.OpenLetter ();
		}
	}
}

