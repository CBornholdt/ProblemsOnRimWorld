using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Harmony;
using Verse;
using System.Reflection;
using System.Reflection.Emit;

namespace ProblemHacker
{
	[HarmonyPatch(typeof(RimWorld.MainTabWindow_Research))]
	[HarmonyPatch("DrawResearchPrereqs")]
	public static class ResearchManager_DrawResearchPrereqs_Patch
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo labelCap = AccessTools.Method (typeof(Verse.Def), "get_LabelCap");
			MethodInfo replacePrereqWithLocked = AccessTools.Method (typeof(ResearchManager_DrawResearchPrereqs_Patch), "ReplacePrereqWithLocked");

			List<CodeInstruction> codes = instructions.ToList ();
			for(int i = 0; i < codes.Count; i++) {
				if (codes [i].opcode == OpCodes.Callvirt && codes [i].operand == labelCap) {
					yield return codes [i];								//Leave string on the stack
					yield return new CodeInstruction (OpCodes.Ldarg_1);	//Leave ResearchProjectDef, string on the stack
					yield return new CodeInstruction (OpCodes.Call, replacePrereqWithLocked);	//Consume 2, leave string
					continue;
				}
				yield return codes [i];
			}	
		}

		static string ReplacePrereqWithLocked(string labelCap, ResearchProjectDef def)
		{
			if (def.LabelCap == labelCap)
				return "Locked".Translate ();
			return labelCap;
		}
	}
}

