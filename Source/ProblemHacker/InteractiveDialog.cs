using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ProblemHacker
{
	public class InteractiveHelper
	{
		public Func<DiaNode> CreateOutcomeChooser(IEnumerable<InteractiveOutcome> outcomes, Pawn doer, Pawn reciever)
		{
			List<InteractiveOutcome> list = outcomes.ToList ();
			Func<InteractiveOutcome, float> weightFunc = delegate(InteractiveOutcome outcome) {
				float result = outcome.baseWeight;
				foreach (InteractiveModifier modifier in outcome.weightModifiers)
					if (modifier.AppliesWith (doer, reciever))
						result *= modifier.modifier;
				return result;
			};
			return delegate {
				InteractiveOutcome chosen;
				list.TryRandomElementByWeight (weightFunc, out chosen);
				return chosen.getResponse();
			};
		}
	}

	public class InteractiveOutcome
	{
		public float baseWeight;
		public Func<DiaNode> getResponse;
		public List<InteractiveModifier> weightModifiers;
	
		public InteractiveOutcome(float baseWeight, Func<DiaNode> getResponse, List<InteractiveModifier> weightModifiers)
		{
			this.baseWeight = baseWeight;
			this.getResponse = getResponse;
			this.weightModifiers = weightModifiers;
		}
	}

	public class InteractiveModifier
	{
		public float modifier;
		public Func<Pawn, Pawn, bool> appliesFunc = null;

		public bool AppliesWith (Pawn doer, Pawn receiver){
			return appliesFunc?.Invoke(doer, receiver) ?? false;
		}
	
		public InteractiveModifier(float modifier, Func<Pawn, Pawn, bool> appliesFunc = null) {
			this.modifier = modifier;
			this.appliesFunc = appliesFunc;
		}
	}
}

