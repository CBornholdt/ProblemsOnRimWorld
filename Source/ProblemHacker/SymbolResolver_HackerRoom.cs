using System;
using Verse;
using RimWorld.BaseGen;

namespace ProblemHacker
{
	public class SymbolResolver_HackerRoomInterior : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
		//	Map map = BaseGen.globalSettings.map;
			ResolveParams rp2 = rp;
			rp2.singleThingDef = DefDatabase<ThingDef>.GetNamed ("OrbitalTradeBeacon");
			BaseGen.symbolStack.Push ("edgeThing", rp2);

			ResolveParams rp3 = rp;
			rp3.singleThingDef = DefDatabase<ThingDef>.GetNamed ("CommsConsole");
			rp3.edgeThingAvoidOtherEdgeThings = true;
			BaseGen.symbolStack.Push ("edgeThing", rp3);
		}
	}
}

