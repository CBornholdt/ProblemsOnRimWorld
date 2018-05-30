using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Harmony;
using RimWorld;
using RimWorld.Planet;

namespace ProblemHacker
{
	public class DynamicCommManager : MapComponent
	{
		private List<CommStruct> comms;
		private readonly string standardNotificationTitleKey = "DynamicCommManagerStandardNotificationTitle";
		private readonly string standardNotificationTextKey = "DynamicCommManagerStandardNotificationText";

		private List<FactionCommStruct> factionComms;

		public DynamicCommManager (Map map) : base(map)
		{
			comms = new List<CommStruct> ();
			factionComms = new List<FactionCommStruct>();
		}

		public IEnumerable<ICommunicable> GetCommsFor(Pawn pawn)
		{
			return comms.Where (c => c.communication.PotentiallyFor (pawn)).Select(c => c.communication).Cast<ICommunicable>();
		}

		public IEnumerable<DiaOption> GetAdditionalFactionOptionsFor(Faction faction, Pawn negotiator)
		{
			return factionComms.Where (c => c.factionComm.ForFaction (faction) && c.factionComm.ForPawn (negotiator))
				.Select (c => c.factionComm.GetOption (faction, negotiator));
		}	

		public void AddCommunication(Communication comm, int ticksUntilDisabled = -1, bool removeWhenDisabled = true)
		{
			comms.Add (new CommStruct (comm, ticksUntilDisabled, removeWhenDisabled));
		}

		public void AddCommunicationWithLetter(Communication comm, string letterTitle, string letterText = null, 
			LetterDef letterDef = null, bool lookAtComms = false, int ticksUntilDisabled = -1, bool removeWhenDisabled = true)
		{
			comms.Add (new CommStruct (comm, ticksUntilDisabled, removeWhenDisabled));
			GlobalTargetInfo lookTarget = default(GlobalTargetInfo);
			if(lookAtComms && this.map.listerBuildings.AllBuildingsColonistOfClass<Building_CommsConsole>().Any())
				this.map.listerBuildings.AllBuildingsColonistOfClass<Building_CommsConsole>().
					Select<Building_CommsConsole, GlobalTargetInfo>(cc => new GlobalTargetInfo(cc)).TryRandomElement(out lookTarget);
			letterDef = ConvertLetterDefToNotification (letterDef ?? LetterDefOf.NeutralEvent);
			NotificationLetter letter = (NotificationLetter) LetterMaker.MakeLetter(letterTitle, letterText ?? letterTitle, letterDef, lookTarget);
			letter.communication = comm;
			comm.SetNotificationLetter (letter);
			if(ticksUntilDisabled != -1)
				letter.StartTimeout(ticksUntilDisabled);
			Find.LetterStack.ReceiveLetter(letter);
		}

		public void AddCommunicationWithStandardNotification(Communication comm, int ticksUntilDisabled = -1, bool removeWhenDisabled = true)
		{
			AddCommunicationWithLetter (comm, this.standardNotificationTitleKey.Translate (), this.standardNotificationTextKey.Translate(), 
				DefDatabase<LetterDef>.GetNamed ("NotificationNeutralEvent"), true, ticksUntilDisabled, removeWhenDisabled);
		}

		public void AddFactionCommunication(FactionCommunication factionComm, int ticksUntilDisabled = -1, bool removeWhenDisabled = true)
		{
			factionComms.Add (new FactionCommStruct (factionComm, ticksUntilDisabled, removeWhenDisabled));
		}

		public bool Contains(Communication comm)
		{
			return this.comms.Any(c => c.communication == comm);
		}

		public bool RemoveCommunication(Communication comm)
		{
			return comms.RemoveAll (c => c.communication == comm) > 0;
		}

		public bool RemoveAllCommunicationsOfType<T>() where T : Communication
		{
			return comms.RemoveAll (c => c.communication is T) > 0;
		}

		public bool RemoveFactionCommunication(FactionCommunication factionComm)
		{
			return factionComms.RemoveAll (c => c.factionComm == factionComm) > 0;
		}

		public bool RemoveAllFactionCommunicationsOfType<T>() where T : FactionCommunication
		{
			return factionComms.RemoveAll (c => c.factionComm is T) > 0;
		}

		public override void ExposeData()
		{
			Scribe_Collections.Look<CommStruct> (ref this.comms, "Comms");
			Scribe_Collections.Look<FactionCommStruct> (ref this.factionComms, "FactionComms");

		/*	//When mod is added to existing game, comms will become null ...
			if (Scribe.mode == LoadSaveMode.LoadingVars && this.comms == null) {
				this.comms = new List<CommStruct>();
				this.factionComms = new List<FactionCommStruct>();
				Log.Message("Hitting");
			}   */
		}

		public override void MapComponentTick()
		{
			for (int i = this.comms.Count; i-- > 0;) {
				var comm = this.comms [i];
				if (comm.enabled && comm.ticksUntilDisabled > 0) {
					if (--(comm.ticksUntilDisabled) == 0) {
						comm.enabled = false;
						if (comm.removeWhenDisabled)
							this.comms.RemoveAt (i);
					}
				}
			}
		}

		private class CommStruct : IExposable
		{
			public bool enabled;
			public int ticksUntilDisabled;
			public bool removeWhenDisabled;
			public Communication communication;
			public Letter letter;

			public CommStruct() : this(null)
			{
			}

			public CommStruct(Communication communication, int ticksUntilDisabled = -1, bool removeWhenDisabled = true, Letter letter = null)
			{
				this.communication = communication;
				this.enabled = true;
				this.ticksUntilDisabled = ticksUntilDisabled;
				this.removeWhenDisabled = removeWhenDisabled;
				this.letter = letter;
			}

			public void ExposeData()
			{
				Scribe_Values.Look<bool> (ref enabled, "Enabled");
				Scribe_Values.Look<int> (ref ticksUntilDisabled, "TicksUntilDisabled");
				Scribe_Deep.Look<Communication> (ref communication, "Communication");
			}
		}

		private class FactionCommStruct : IExposable
		{
			public bool enabled;
			public int ticksUntilDisabled;
			public bool removeWhenDisabled;
			public FactionCommunication factionComm;

			public FactionCommStruct() : this(null) { }

			public FactionCommStruct(FactionCommunication factionComm, int ticksUntilDisabled = -1, bool removeWhenDisabled = true)
			{
				this.factionComm = factionComm;
				this.ticksUntilDisabled = ticksUntilDisabled;
				this.removeWhenDisabled = removeWhenDisabled;
				this.enabled = true;

			}

			public void ExposeData()
			{
				Scribe_Values.Look<bool> (ref enabled, "Enabled");
				Scribe_Values.Look<int> (ref ticksUntilDisabled, "TicksUntilDisabled");
				Scribe_Deep.Look<FactionCommunication> (ref factionComm, "FactionComm");
			}
		}

	/*	private class OptionStruct : IExposable
		{
			public bool enabled	*/

		public static Letter MakeNotificationLetter(string title, string text = null, LetterDef def = null, int duration = -1)
		{
			if(def == null)
				def = LetterDefOf.NeutralEvent;
			def = ConvertLetterDefToNotification(def);
			NotificationLetter letter = new NotificationLetter ();
			letter.label = letter.title = title;
			letter.text = text ?? title;
			letter.def = def;
			if(duration != -1)
				letter.StartTimeout(duration);
			return letter;
		}	

		public static LetterDef ConvertLetterDefToNotification(LetterDef def)
		{
			if (def.defName.Contains ("Notification"))
				return def;
				
			return DefDatabase<LetterDef>.GetNamed ("Notification" + def.defName, false) ?? def;
		}
	}
}

