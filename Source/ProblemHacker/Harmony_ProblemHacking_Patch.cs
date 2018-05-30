using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Verse;
using RimWorld;
using Harmony;

// Analysis disable once CheckNamespace
namespace ProblemHacker
{
	[StaticConstructorOnStartup]
	static class HarmonyPatches
	{
		static HarmonyPatches() {
			HarmonyInstance.DEBUG = true;

			HarmonyInstance harmony = HarmonyInstance.Create ("rimworld.problemhacker");

			harmony.Patch (AccessTools.Method (typeof(RimWorld.Storyteller), "StorytellerTick"), null,
				new HarmonyMethod (typeof(ProblemHacker.ProblemManager).GetMethod ("Storyteller_StorytellerTick_Postfix")));

			harmony.Patch (AccessTools.Method (typeof(RimWorld.Storyteller), "StorytellerTick"), null,
				new HarmonyMethod (typeof(ProblemHacker.HarmonyPatches).GetMethod ("StorytellerTick_Postfix")));

			harmony.PatchAll (Assembly.GetExecutingAssembly ());
		}
	}
}

