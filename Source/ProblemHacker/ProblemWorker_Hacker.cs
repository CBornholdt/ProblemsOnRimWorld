using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;
using RimWorld.Planet;

//Styled after IncidentWorker_JourneyOffer ... kinda, at least the transversal stuff

namespace ProblemHacker
{
	public class ProblemWorker_Hacker : ProblemWorker
	{
		internal readonly int MaxHackingWorldDistance = 10;
		internal readonly int MinHackingWorldDistance = 2;
		internal readonly int MinCoolDownTicks = 10000;
		internal readonly int MaxCoolDownTicks = 20000;
		internal readonly int MinPreparingTicks = 10000;
		internal readonly int MaxPreparingTicks = 20000;
		internal readonly int MaxTimeHacking = 15000;
		internal readonly int TicksUntilHackerNoticesIssue = 5000;
		internal readonly int TicksUntilHackerFlees = 10000;
		internal readonly int TicksBetweenWeakContactAttempts = 10000;
		internal readonly int WeakContactAttemptDuration = 3000;

		public enum HackingState { CoolDown, Preparing, Ongoing, Stopped }; 
		public enum HackedAllyCommunicationState { NoAlly, WeakContactAttempt, SpokenToFaction, SpokenToBase, GotWarning, PlanningAssault };

		internal Map rootMap;
		internal int hackingSourceTile = -1;
		internal int ambushHackerTile = -1;
		internal FactionBase alliedBase = null;
		internal HackingState hackingState = HackingState.Stopped;
		internal HackedAllyCommunicationState alliedCommState = HackedAllyCommunicationState.NoAlly;
		internal int numWeakContactResponses = 0;
		internal int commStateLastTick; //GameTick during last communication
		internal int hackingStateLastTick;	//GameTick during last state change
		internal int tickWaitedFor;
		internal bool hasHackerNoticedIssue = false;
		internal bool foundHackingSite = false;
		internal bool foundAmbushSite = false;
		internal bool didAllySetupAmbush = false;
		internal bool areCountermeasuresActive = false;
		internal Pawn hacker;	
		internal bool hackerCurrentlyHeld;	//Will temporarily hold hacker until site is revealed
		internal Site hackingSite;
		internal int timesHacked = 0;
		internal int currentExtortionAmount = 0;

		internal List<Building_TurretGun> hackedTurrets;
		internal Building hackingEntry;

		private int id;

		internal bool wasHackingRushed = false;

		private static readonly SimpleCurve hackingRushPenaltyCurve = new SimpleCurve {
			{ new CurvePoint(0f, 1f), true },
			{ new CurvePoint(5f, 0f), true }
		};
		private static readonly SimpleCurve hackingChanceCurve = new SimpleCurve {
			{ new CurvePoint(0f, 0.3f), true },
			{ new CurvePoint(2f, 0.5f), true },
			{ new CurvePoint(5f, 0.8f), true }
		};

		public Faction HackedAlliedFaction {
			get {
				return this.alliedBase?.Faction;
			}
		}

		public bool HackerFound {
			get {
				return this.foundHackingSite;
			}
		}

		public int ID {
			get { return this.id; }
		}

		internal void ApplyHacking()
		{
			foreach (Building_TurretGun turret in this.hackedTurrets ?? Enumerable.Empty<Building_TurretGun>()) 
				turret.AddDynamicComp<CompHacked>(new CompHacked(this.hacker.Faction));

			this.hackingEntry?.AddDynamicComp<CompHacked>();
		}

		internal bool AreAllHackedTurretsDestroyedOrDisabled()
		{
			return !hackedTurrets.Any (x => x.Destroyed == false && x.GetComp<CompPowerTrader> ().PowerOn);
		}

		internal void BeginHacking()
		{
			if(this.areCountermeasuresActive) {
				CountermeasuresWorked();
				return;
			}

			if(this.timesHacked == 0)
				FindExt.ResearchTaskManager.ResetResearch (DefDatabase<ResearchProjectDef>.GetNamed ("HackingCountermeasures"), unlockAsWell:true);

			FindExt.ResearchTaskManager.ResetResearch (DefDatabase<ResearchProjectDef>.GetNamed ("DisruptHacking"), unlockAsWell:true);

			IEnumerable<Building> potentialEntries = rootMap.listerBuildings.allBuildingsColonist.Where(CanHackThroughBuilding);

			IEnumerable<Building_TurretGun> potentialTurrets = rootMap.listerBuildings.AllBuildingsColonistOfClass<Building_TurretGun> ()
				.Where (x => x.GetComp<CompPowerTrader> ()?.PowerOn ?? false);  
			if (!potentialEntries.Any() || !potentialTurrets.Any()) {
				HackingFailed ();
				return;
			}

			float hackingChance = hackingChanceCurve.Evaluate((float) this.timesHacked);
			float hackingRushPenaltyChance = hackingRushPenaltyCurve.Evaluate((float) this.timesHacked);

			for(int i = 0; i < 3; i++) {	//Make 3 attempts
				foreach (Building_TurretGun turret in potentialTurrets) 
					if (Rand.Chance (hackingChance)) {
						hackedTurrets.Add(turret);
						if(wasHackingRushed && Rand.Chance(hackingRushPenaltyChance))
							break;	
					}
				
				if (hackedTurrets.Any ()) {
					this.hackingEntry = potentialEntries.RandomElement();
					ApplyHacking();
					this.timesHacked++;
					SendHackingNotice ();
					return;
				}
			}

			HackingFailed();
		}

		internal void CalculateExtortionAmount()
		{
			this.currentExtortionAmount = (int)((float)this.HackableTurretList().Count () * Rand.Range (100f, 200f) * ((float)this.timesHacked + 3f) / 8f);
		}

		public bool CanHackThroughBuilding(Building b)
		{
			return (b.GetComp<CompPowerTrader> ()?.PowerOn ?? false) && (b is Building_CommsConsole || b is Building_OrbitalTradeBeacon);
		}

		internal void CreateAndRevealHackerSite()
		{
			this.foundHackingSite = true;
			this.hackingSite = SiteMaker.MakeSite (DefDatabase<SiteCoreDef>.GetNamed ("ProblemHacker_HackingSite"), (SitePartDef)null, this.hacker.Faction);
			this.hackingSite.GetComponent<RemoteHackerComp> ().pawn.TryAdd (this.hacker, true);
			this.hackerCurrentlyHeld = false;	//No longer hold the hacker ...
			this.hackingSite.GetComponent<TimeoutComp> ().StartTimeout (120000);
			this.hackingSite.Tile = this.hackingSourceTile;

			Find.WorldObjects.Add (this.hackingSite);

			//IntVec3 spawnCell = CellFinder.RandomSpawnCellForPawnNear (this.hackingSite.Map.Center, this.hackingSite.Map, 4);
			//GenSpawn.Spawn (this.hacker, spawnCell, this.hackingSite.Map, Rot4.Random, false);
		}

		internal void CountermeasuresWorked()
		{
			if (Rand.Chance (0.25f))
				HackerNoticesIssue ();
		}

		//TODO set success rate relative to the Intellectual skill of hacker vs researcher
		internal void DisruptHackingAttempt() {
			if (this.hackingState != HackingState.Ongoing)
				return;

			if (Rand.Chance (0.5f)) {	//Failed
				FindExt.ResearchTaskManager.ResetResearch (DefDatabase<ResearchProjectDef>.GetNamed ("DisruptHacking"));
				Find.LetterStack.ReceiveLetter ("Disruption Failed", "Your attempts at disruption have failed", LetterDefOf.NegativeEvent, null);
			} else {	//Succeeded
				SetHackingState (HackingState.CoolDown);
				Find.LetterStack.ReceiveLetter ("Disruption successful", "Your attempts at disruption have succeeded", LetterDefOf.NegativeEvent, null);
			}
		}

		internal void ExtortionAccepted()
		{
			TradeUtility.LaunchSilver (this.rootMap, this.currentExtortionAmount);
			Finish();
		}

		internal void ExtortionRejected()
		{

		}

		internal void ExtortionRejectedInsulted()
		{
			this.wasHackingRushed = true;
			SetHackingState (HackingState.Ongoing);
		}

		internal void FindClosestHackableAlliedBase()
		{
			this.alliedBase = null;
			float minTileDistance = 10000;
			foreach (FactionBase factionBase in Find.WorldObjects.FactionBases.Where(x => !x.Faction.HostileTo(Faction.OfPlayer) &&
				x.Faction.def.techLevel >= TechLevel.Industrial && x.Faction != Faction.OfPlayer)) {
				float distance = Find.WorldGrid.TraversalDistanceBetween (this.rootMap.Tile, factionBase.Tile);
				if (distance < minTileDistance) {
					this.alliedBase = factionBase;
					minTileDistance = distance;
				}
			}
		}

		internal void FindOrCreateHacker(Faction hackingFaction)
		{
			if(PawnsFinder.AllMapsWorldAndTemporary_Alive.
				Where(x => x.Faction == hackingFaction 
					&& x.skills.GetSkill(SkillDefOf.Intellectual).Level >= 5
					&& !(x.story?.WorkTagIsDisabled(WorkTags.Violent) ?? false)
					&& !(x.Map?.IsPlayerHome ?? false)
					&& !(x.IsCaravanMember())
					&& x.Faction.leader != x).	//Don't want to deal with removing the last member of a caravan ... ugh
				TryRandomElementByWeight(x => x.skills.GetSkill(SkillDefOf.Intellectual).Level, out this.hacker)) {
				if(this.hacker.Spawned)	
					this.hacker.DeSpawn();	
				if(Find.WorldPawns.Contains(this.hacker))
					Find.WorldPawns.RemovePawn(this.hacker);
				return;
			} 

			PawnGenerationRequest request = new PawnGenerationRequest(hackingFaction.RandomPawnKind(), hackingFaction, 
				mustBeCapableOfViolence:true, forceAddFreeWarmLayerIfNeeded:true);
			this.hacker = PawnGenerator.GeneratePawn(request);
			//Find.WorldPawns.PassToWorld(this.hacker, PawnDiscardDecideMode.KeepForever);
		}

		internal IEnumerable<Building_TurretGun> HackableTurretList(IIncidentTarget target = null)
		{
			if (target == null)
				target = this.target;
			Map map = target as Map;
			return map.listerBuildings.AllBuildingsColonistOfClass<Building_TurretGun> ().Where(t => t.TryGetComp<CompHacked>() == null);
		}

		public void HackingCountermeasuresImplemented()
		{
			if(this.areCountermeasuresActive)
				return;
			this.areCountermeasuresActive = true;
			Find.LetterStack.ReceiveLetter ("ProblemHackerCountermeasuresActiveLetterLabel".Translate (), 
				"ProblemHackerCountermeasuresActiveLetterText".Translate (), LetterDefOf.PositiveEvent);
		}

		internal void HackerNoticesIssue()
		{
			SetHackingState (HackingState.Stopped);
			this.hasHackerNoticedIssue = true;
			this.tickWaitedFor = Find.TickManager.TicksGame + TicksUntilHackerFlees;
		}

		internal void HackerFlees()
		{
			Finish();
		}

		internal void HackingFailed()
		{
			SetHackingState (HackingState.CoolDown);
		}

		public bool HasCompletedOrFailed()
		{
			if(this.hacker.Dead)
				return true;
			return false;
		}

		public bool HasHackingStopped()
		{
			return this.hackingState == HackingState.Stopped;
		}

		protected override void Initialize()
		{
			FindExt.ResearchTaskManager.RegisterResearchCompletionAction(DefDatabase<ResearchProjectDef>.GetNamed("HackingCountermeasures"), 
				this.HackingCountermeasuresImplemented);
			FindExt.ResearchTaskManager.RegisterResearchCompletionAction(DefDatabase<ResearchProjectDef>.GetNamed ("DisruptHacking"),
				this.DisruptHackingAttempt);
		}
			
		public bool IsHackingCurrentlyOngoing()
		{
			return this.hackingState == HackingState.Ongoing;
		}

		internal void PayExtortion()
		{

		}

		public bool PlayerHasExtortionAmount()
		{
			return TradeUtility.AllLaunchableThings (this.rootMap).Where (t => t.def == ThingDefOf.Silver).Sum (s => s.stackCount) >= this.currentExtortionAmount;
		}

		internal void RemoveHacking()
		{
			FindExt.ResearchTaskManager.LockResearch (DefDatabase<ResearchProjectDef>.GetNamed ("DisruptHacking"));

			if (this.hackedTurrets.Any (x => !x.Destroyed))
				SendHackingEndedNotice ();
			foreach (Building_TurretGun turret in this.hackedTurrets.Where(x => !x.Destroyed && !x.Discarded))
				turret.RemoveDynamicComp<CompHacked> ();
			this.hackedTurrets.Clear ();
			this.hackingEntry?.RemoveDynamicComp<CompHacked>();
			this.hackingEntry = null;
			this.wasHackingRushed = false;
		}

		internal void SendExtortionThreat()
		{
			CalculateExtortionAmount ();
			//	Find.LetterStack.ReceiveLetter ("ProblemHackerExtortionLetterLabel".Translate (), text, LetterDefOf.ThreatSmall);
			FindExt.DynamicCommManagerFor (this.rootMap).AddCommunicationWithStandardNotification (new ExtortionCommunication (this), MaxPreparingTicks, true);
		}

		internal void SendHackingNotice()
		{
			var letter = DynamicCommManager.MakeNotificationLetter ("ProblemHackerHackedLetterTitle".Translate(), 
				"ProblemHackerHackedLetterText".Translate(), LetterDefOf.ThreatBig, 3000);
			Find.LetterStack.ReceiveLetter (letter);
		}

		internal void SendHackingEndedNotice()
		{
			Find.LetterStack.ReceiveLetter ("ProblemHackerHackingEndedLetterLabel".Translate (), "ProblemHackerHackingEndedLetterText".Translate (), LetterDefOf.PositiveEvent);
		}

		internal void SendWeakContactAttempt()
		{	//Will look for keys "PH_WCA_LetterText<0-3>" incrementing per weak contact attempt
			FindExt.DynamicCommManagerFor (this.rootMap).AddCommunicationWithLetter (new WeakContactAttempt (this), "PH_WCA_LetterTitle".Translate (),
				"PH_WCA_LetterText".Translate (), LetterDefOf.NeutralEvent, true, WeakContactAttemptDuration);
		}
			
		internal void SetHackedAllyCommStatus(HackedAllyCommunicationState newState)
		{
			if (this.alliedCommState == newState)
				return;

			this.commStateLastTick = Find.TickManager.TicksGame;

			switch (newState) {
			case HackedAllyCommunicationState.WeakContactAttempt:
				FindExt.DynamicCommManagerFor (this.rootMap).AddFactionCommunication(new HackedAllyFactionCommunication(this));
				SendWeakContactAttempt ();
				break;
			}

			this.alliedCommState = newState;
		}

		internal void SetHackingState(HackingState newState)
		{
			if (this.hackingState != newState) {
				if (this.hackingState == HackingState.Ongoing || newState == HackingState.Stopped)
					RemoveHacking ();

				hackingStateLastTick = Find.TickManager.TicksGame;
				switch (newState) {
				case HackingState.CoolDown:
					this.tickWaitedFor = Rand.Range (MinCoolDownTicks, MaxCoolDownTicks) + hackingStateLastTick;
					break;
				case HackingState.Preparing:
					this.tickWaitedFor = Rand.Range (MinPreparingTicks, MaxPreparingTicks) + hackingStateLastTick;
					break;
				case HackingState.Ongoing:
					this.tickWaitedFor = hackingStateLastTick + MaxTimeHacking;
					BeginHacking ();
					break;
				case HackingState.Stopped:
					this.tickWaitedFor = (int)(Rand.Range(1f, 2f) * (float)TicksUntilHackerNoticesIssue) + hackingStateLastTick;
					break;
				}
				this.hackingState = newState;
			}
		}

		internal bool TimeForStateChange()
		{
			return Find.TickManager.TicksGame >= this.tickWaitedFor;
		}

		public void TracedHackingSignal(Pawn Pawn)
		{
			if(!this.foundHackingSite){
				CreateAndRevealHackerSite();
				String text = string.Format ("ProblemHackerTracedLetterText".Translate (), Pawn.NameStringShort);
				Find.LetterStack.ReceiveLetter ("ProblemHackerTracedLetterLabel".Translate(), text, LetterDefOf.PositiveEvent, null);
			}
		}

		internal bool TryFindHackingSourceTile()
		{
			if(this.alliedBase == null)	
				return TileFinder.TryFindPassableTileWithTraversalDistance(this.rootMap.Tile, MinHackingWorldDistance, MaxHackingWorldDistance,
					out this.hackingSourceTile, preferCloserTiles:false);
			else //Find tile close to Allied base as well ...
				return TileFinder.TryFindPassableTileWithTraversalDistance(this.rootMap.Tile, MinHackingWorldDistance, MaxHackingWorldDistance, 
					out this.hackingSourceTile, (x) => Find.WorldGrid.TraversalDistanceBetween (x, this.alliedBase.Tile) <= MaxHackingWorldDistance, 
					preferCloserTiles:false);
		}

		public void UpdateNotifications()
		{
			switch (this.alliedCommState) {
			case HackedAllyCommunicationState.NoAlly:
				return;
			case HackedAllyCommunicationState.WeakContactAttempt:
				if (this.commStateLastTick >= Find.TickManager.TicksGame + TicksBetweenWeakContactAttempts) {
					SendWeakContactAttempt ();
					this.commStateLastTick = Find.TickManager.TicksGame;
				}
				return;
			case HackedAllyCommunicationState.SpokenToFaction:
				goto case HackedAllyCommunicationState.WeakContactAttempt;
			}
		}

		protected override bool CanStartNowFor(IIncidentTarget target)
		{
			return target is Map && (target as Map).ParentFaction == Faction.OfPlayer && !IsRunningOn(target) && this.HackableTurretList (target).Count() >= 4;
		}

		public override void ExposeData()
		{
			base.ExposeData ();
			Scribe_References.Look<Map> (ref this.rootMap, "RootMap");
			Scribe_Values.Look<int> (ref this.id, "ID", 0);
			Scribe_Values.Look<int> (ref this.hackingSourceTile, "HackerSourceTile");
			Scribe_Values.Look<int> (ref this.ambushHackerTile, "AmbushSourceTile");
			Scribe_References.Look<FactionBase> (ref this.alliedBase, "AlliedBase");
			Scribe_Values.Look<int> (ref this.hackingStateLastTick, "HackingStateLastTick");
			Scribe_Values.Look<HackingState> (ref this.hackingState, "HackingState");
			Scribe_Values.Look<HackedAllyCommunicationState> (ref this.alliedCommState, "AlliedCommState");
			Scribe_Values.Look<int> (ref this.numWeakContactResponses, "NumWeakContactResponses");
			Scribe_Values.Look<int> (ref this.commStateLastTick, "CommStateLastTick");
			Scribe_Values.Look<int> (ref this.tickWaitedFor, "TickWaitedFor");
			Scribe_Values.Look<bool> (ref this.hasHackerNoticedIssue, "HasHackedNoticedIssue");
			Scribe_Values.Look<bool> (ref this.foundHackingSite, "FoundHackingSite");
			Scribe_Values.Look<bool> (ref this.foundAmbushSite, "FoundAmbushSite");
			Scribe_Values.Look<bool> (ref this.didAllySetupAmbush, "DidAllySetupAmbush");
			Scribe_Values.Look<bool>(ref this.hackerCurrentlyHeld, "HackerCurrentlyHeld");
			if (this.hackerCurrentlyHeld)
				Scribe_Deep.Look<Pawn> (ref this.hacker, "THEHACKERheld");
			else
				Scribe_References.Look<Pawn> (ref this.hacker, "THEHACKER");

			Scribe_Collections.Look<Building_TurretGun> (ref this.hackedTurrets, "HackedTurrets", LookMode.Reference);
			Scribe_References.Look<Building> (ref this.hackingEntry, "HackingEntry");
			Scribe_References.Look<Site> (ref this.hackingSite, "HackingSite");
			Scribe_Values.Look<int> (ref this.timesHacked, "TimesHacked");
			Scribe_Values.Look<int> (ref this.currentExtortionAmount, "CurrentExtortionAmount");
			Scribe_Values.Look<bool> (ref this.wasHackingRushed, "WasHackingRushed");
		}

		protected override void Finish()
		{
			RemoveHacking();
			FindExt.ResearchTaskManager.UnregisterResearchCompletionAction (DefDatabase<ResearchProjectDef>.GetNamed ("HackingCountermeasures"),
				this.HackingCountermeasuresImplemented);
			FindExt.ResearchTaskManager.UnregisterResearchCompletionAction (DefDatabase<ResearchProjectDef>.GetNamed ("DisruptHacking"),
				this.DisruptHackingAttempt);
			FindExt.DynamicCommManagerFor (this.rootMap).RemoveAllCommunicationsOfType<WeakContactAttempt> ();
			FindExt.DynamicCommManagerFor (this.rootMap).RemoveAllCommunicationsOfType<ContactHackedAllyBase> ();
			FindExt.DynamicCommManagerFor (this.rootMap).RemoveAllCommunicationsOfType<ExtortionCommunication> ();
			FindExt.DynamicCommManagerFor (this.rootMap).RemoveAllFactionCommunicationsOfType<HackedAllyFactionCommunication> ();
			base.Finish ();
		}

		public override string GetUniqueLoadID()
		{
			return "ProblemHacker_" + this.id.ToString();
		}

		protected override bool TryStartProblem ()
		{
			this.rootMap = this.target as Map;
			this.id = this.rootMap.uniqueID;

			Faction hackingFaction = Find.FactionManager.RandomEnemyFaction (allowHidden:false, allowDefeated:false, allowNonHumanlike:false, minTechLevel:TechLevel.Industrial);
			if (hackingFaction == null || !TryFindHackingSourceTile ())
				return false;
				
			FindOrCreateHacker(hackingFaction);
			this.hackerCurrentlyHeld = true;
			this.hackedTurrets = new List<Building_TurretGun> ();

			FindClosestHackableAlliedBase();
			if (this.alliedBase != null) 
				SetHackedAllyCommStatus (HackedAllyCommunicationState.WeakContactAttempt);

			this.hasHackerNoticedIssue = false;
			this.foundHackingSite = false;
			SetHackingState (HackingState.CoolDown);

			return true;
		}

		public override void UpdateTick()
		{
			if (HasCompletedOrFailed ()) {
				Finish ();
				return;
			}

			UpdateNotifications ();
				
			switch(this.hackingState){
			case HackingState.CoolDown:
				if (TimeForStateChange ()) {
					SendExtortionThreat ();
					SetHackingState (HackingState.Preparing);
				}
				break;
			case HackingState.Preparing:
				if (TimeForStateChange ()) {
					SetHackingState (HackingState.Ongoing);
				}
				break;
			case HackingState.Ongoing:	//Countermeasures prevent hacking shutoff due to turret disabled
				if ((AreAllHackedTurretsDestroyedOrDisabled () && !this.areCountermeasuresActive) || TimeForStateChange ()) {
					RemoveHacking ();
					SetHackingState (HackingState.CoolDown);
				}
				break;
			case HackingState.Stopped:
				if (this.hasHackerNoticedIssue) {
					if (TimeForStateChange())
						HackerFlees ();
				}
				else {
					if (TimeForStateChange()) 
						HackerNoticesIssue();
				}
				break;
			}
		}

		public DiaOption MakeResetToRootOption()
		{
			MethodInfo okToRoot = typeof(FactionDialogMaker).GetMethod ("OKToRoot", BindingFlags.NonPublic | BindingFlags.Static);
			return (DiaOption) okToRoot.Invoke (null, null);
		}
	}
}

