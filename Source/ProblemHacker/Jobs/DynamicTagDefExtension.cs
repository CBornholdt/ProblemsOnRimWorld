using System;
using Verse;

namespace ProblemHacker
{
	//Will label this work as dynamic and not worthy of priorization
	public class DynamicTag : DefModExtension
    {
		public DynamicTag() { }
        
		public string Tag {
			get { return "Dynamic"; }
		}
    }
}
