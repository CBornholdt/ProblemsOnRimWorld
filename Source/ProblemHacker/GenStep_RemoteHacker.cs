using System;
using Verse;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse.AI.Group;

namespace ProblemHacker
{
	public class GenStep_RemoteHacker : GenStep_Scatterer
	{
		private const int Size = 9;

		protected override bool CanScatterAt (IntVec3 c, Map map)
		{
			if (!base.CanScatterAt (c, map))
				return false;
			if (!map.reachability.CanReachMapEdge (c, TraverseParms.For (TraverseMode.PassDoors, Danger.Deadly, false)))
				return false;
			CellRect rect = CellRect.CenteredOn (c, Size, Size);
			if (!rect.FullyContainedWithin (new CellRect (0, 0, map.Size.x, map.Size.z)))
				return false;
			foreach (IntVec3 cell in rect.Cells) {
				TerrainDef terrain = map.terrainGrid.TerrainAt (cell);
				if (!terrain.affordances.Contains (TerrainAffordance.Heavy) && terrain.driesTo != null)
					return false;
			}

			return true;
		}

		protected override void ScatterAt(IntVec3 loc, Map map, int count = 1)
		{
			Faction faction;
			if (map.ParentFaction == null || map.ParentFaction == Faction.OfPlayer) {
				faction = Find.FactionManager.RandomEnemyFaction (false, false, true, TechLevel.Undefined);
			} else {
				faction = map.ParentFaction;
			}

			CellRect rect = CellRect.CenteredOn (loc, Size, Size);
			RemoteHackerComp hackerComp = map.info.parent.GetComponent<RemoteHackerComp> ();
			Pawn hackerPawn = hackerComp.pawn.Take (hackerComp.pawn [0]);
			ResolveParams rp = default(ResolveParams);
			rp.rect = rect;
			rp.faction = faction;
			rp.wallStuff = ThingDefOf.Steel;
			BaseGen.globalSettings.map = map;
			BaseGen.symbolStack.Push ("problemHacker_hackerRoom", rp);
			BaseGen.Generate ();

			ResolveParams rp2 = default(ResolveParams);
			rp2.rect = rect;
			rp2.faction = faction;
			rp2.singlePawnToSpawn = hackerPawn;
			rp2.singlePawnLord = LordMaker.MakeNewLord (faction, new LordJob_DefendBase (faction, loc), map);
			BaseGen.globalSettings.map = map;
			BaseGen.symbolStack.Push ("pawn", rp2);
			BaseGen.Generate ();

			ResolveParams rp3 = default(ResolveParams);
			rp3.rect = rp.rect.ExpandedBy (2);
			rp3.edgeDefenseWidth = 2;
			rp3.edgeDefenseTurretsCount = 8;
			rp3.edgeDefenseGuardsCount = 2;
			rp3.faction = faction;
			rp3.singlePawnLord = rp2.singlePawnLord;
			BaseGen.globalSettings.map = map;
			BaseGen.symbolStack.Push ("edgeDefense", rp3);
			BaseGen.Generate ();
		}
	}
}

