using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace ProblemHacker
{
	public class DynamicComponentManager : WorldComponent
	{
		//This manager will hold the DynamicThingComps and serialize them, but keep the ThingWithComps as reference only
		private Dictionary<ThingWithComps, ThingCompList> managedThingComps = new Dictionary<ThingWithComps, ThingCompList>();

		private List<ThingWithComps> managedThingComps_keyList = null;	//Needed to expose managedThingComps with Keys as a reference ...
		private List<ThingCompList> managedThingComps_valueList = null;	//Same as above, although ThingCompLists are deep serialized

		public DynamicComponentManager(World world) : base(world)
		{
		}

		public T AddDynamicThingComp<T>(ThingWithComps thing, T existingComp = null) where T : DynamicThingComp, new()
		{
			T comp = thing.TryGetComp<T> ();
			if (comp != null)
				return comp;

			if (existingComp != null)
				comp = existingComp;
			else
				comp = new T (); 
			comp.parent = thing;
			if (!this.managedThingComps.ContainsKey (thing))
				this.managedThingComps.Add (thing, new ThingCompList());

			this.managedThingComps [thing].comps.Add (comp);
			thing.AllComps.Add (comp);
			comp.Initialize();
			comp.Added ();
			return comp;
		}

		public bool RemoveDynamicThingComp<T>(ThingWithComps thing) where T : DynamicThingComp
		{
			T comp = thing.TryGetComp<T> ();
			if (comp == null)
				return false;
				
			thing.AllComps.Remove (comp);
			if(this.managedThingComps.TryGetValue(thing, out ThingCompList compList))
				compList.comps.Remove(comp);
			comp.Removed();
			return true;
		}

		public void CleanupDestroyedOrDiscardedThings()
		{
			var things = this.managedThingComps.Keys.ToList ();
			for (int i = things.Count (); i-- > 0;)
				if (things [i].Discarded)
					this.managedThingComps.Remove (things [i]);
		}

		public void LongTick()
		{	//Remove destroyed Things
			CleanupDestroyedOrDiscardedThings ();
		}

		private void PostLoadInit()
		{
			Log.Message ("Comps :" + this.managedThingComps.Count);
			foreach(ThingWithComps thing in this.managedThingComps.Keys.Where(t => !t.Destroyed && !t.Discarded))
				foreach(DynamicThingComp comp in this.managedThingComps[thing].comps) {
					thing.AllComps.Add(comp);
					comp.parent = thing;
					comp.Initialize ();
				}
		}

		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
				CleanupDestroyedOrDiscardedThings ();

			Log.Message ("Comps :" + this.managedThingComps.Count + " Mode :" + Scribe.mode.ToString());

			Scribe_Collections.Look<ThingWithComps, ThingCompList> (ref this.managedThingComps, "ManagedThingComps", LookMode.Reference,
				LookMode.Deep, ref this.managedThingComps_keyList, ref this.managedThingComps_valueList);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
				this.PostLoadInit ();
		}

		public override void WorldComponentTick()
		{
			if ((Find.TickManager.TicksGame % 1000) != 100)
				return;
			LongTick ();
		}

		private class ThingCompList : IExposable
		{
			public List<DynamicThingComp> comps = new List<DynamicThingComp>();

			public void ExposeData()
			{
				Scribe_Collections.Look<DynamicThingComp> (ref comps, "Comps", LookMode.Deep);
			}
		}
	}

	public static class DynamicComponentManagerExt
	{
		public static T AddDynamicComp<T>(this ThingWithComps thing, T existingComp = null) where T : DynamicThingComp, new()
		{
			return FindExt.DynamicComponentManager.AddDynamicThingComp<T> (thing, existingComp);
		}

		public static bool RemoveDynamicComp<T>(this ThingWithComps thing) where T : DynamicThingComp, new()
		{
			return FindExt.DynamicComponentManager.RemoveDynamicThingComp<T> (thing);
		}
	}
}

