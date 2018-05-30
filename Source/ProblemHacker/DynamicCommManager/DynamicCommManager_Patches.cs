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
	[HarmonyPatch(typeof(Building_CommsConsole))]
	[HarmonyPatch("GetFloatMenuOptions")]
	public static class Building_CommsConsole_GetFloatMenuOptions_Patch
	{
		static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo listConcat = AccessTools.Method (typeof(Enumerable), "Concat", null, new []{typeof(ICommunicable)});
			MethodInfo concatHelper = AccessTools.Method (typeof(Building_CommsConsole_GetFloatMenuOptions_Patch), "ConcatHelper");
			//MethodInfo getCallLabel = AccessTools.Method (typeof(RimWorld.ICommunicable), "GetCallLabel");
			MethodInfo callLabelReplace = AccessTools.Method(typeof(Building_CommsConsole_GetFloatMenuOptions_Patch), "CallLabelReplace");
					
			List<CodeInstruction> codes = instructions.ToList ();
			for(int i = 0; i < codes.Count; i++) {
				//Was having issues getting myPawn from patched method, it was copied to a local anonymous class
				// Instead, I will grab the whole block starting after it loads that myPawn value and dup it 
				if ((i + 7) < codes.Count && codes[i + 7].opcode == OpCodes.Call && codes[i + 7].operand == listConcat) {
					yield return new CodeInstruction (OpCodes.Dup);	//leave Pawn on stack
					yield return codes[i];		
					yield return codes[i + 1];
					yield return codes[i + 2];
					yield return codes[i + 3];
					yield return codes[i + 4];
					yield return codes[i + 5];
					yield return codes[i + 6];
					yield return codes[i + 7];		//leaves IEnumerable<ICommunicable>, Pawn on stack
					yield return new CodeInstruction (OpCodes.Ldloc_S, 8);	//leaves List<FloatMenuOption>, IEnumerable<ICommunication>, Pawn
					yield return new CodeInstruction (OpCodes.Call, concatHelper);	//consume 3 leave IEnumerable<ICommunicable>
					i += 7;		//Increment for additional consumed operations
					continue;
				}
				if (codes [i].opcode == OpCodes.Isinst && codes [i].operand == typeof(RimWorld.Faction)) {
					yield return new CodeInstruction (OpCodes.Dup);		//Leave ICommunicable on stack
					yield return new CodeInstruction (OpCodes.Ldloc_S, 13);	//Leave ICommunicable, string on stack
					yield return new CodeInstruction (OpCodes.Call, callLabelReplace);	//Consume 2, leave string
					yield return new CodeInstruction (OpCodes.Stloc_S, 13);	//Consume string
				}
				yield return codes [i];
			}	
		}

		static string CallLabelReplace(ICommunicable comm, string callLabel)
		{
			if(comm is Communication)
				return comm.GetCallLabel();
			return callLabel;
		}

		static IEnumerable<ICommunicable> ConcatHelper(Pawn pawn, IEnumerable<ICommunicable> options, List<FloatMenuOption> list)
		{
			foreach(Communication comm in FindExt.DynamicCommManagerFor(pawn.Map).GetCommsFor(pawn))
				if(!comm.IsAllowedFor(pawn, out string disabledReason))
					list.Add(new FloatMenuOption(comm.GetCallLabel() + " (" + disabledReason.Translate() + ")",
						null, MenuOptionPriority.Default, null, null, 0, null, null));
				else 
					yield return comm;

			foreach(var opt in options)
				yield return opt;
		}
	}

	[HarmonyPatch(typeof(JobDriver_UseCommsConsole))]
	[HarmonyPatch("MakeNewToils")]
	public static class JobDriver_UseCommsConsole_MakeNewToils_Prefix
	{
		private static void Prefix(JobDriver_UseCommsConsole __instance)
		{
			Communication comm = __instance.job.commTarget as Communication;
			if (comm == null)
				return;
			__instance.AddFailCondition (() => !FindExt.DynamicCommManagerFor (__instance.GetActor ().Map).Contains (comm) || !comm.IsAllowedFor (__instance.GetActor ()));
		}
	}
}
