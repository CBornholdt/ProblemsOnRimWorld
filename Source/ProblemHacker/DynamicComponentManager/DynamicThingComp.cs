using System;
using Verse;

namespace ProblemHacker
{
	public abstract class DynamicThingComp : ThingComp, IExposable
	{
		public virtual void ExposeData()
		{
		}

		public virtual void Added() { }

		public virtual void Initialize(){}

		public virtual void Removed(){}
	}
}

