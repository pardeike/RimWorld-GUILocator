using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GUILocator
{
	public class Element
	{
		public class Comparer : IComparer<Element>
		{
			public int Compare(Element x, Element y)
			{
				if (GUILocator.Settings.sortByAddedOrder)
					return x.added.CompareTo(y.added);
				return x.GetPath(0).CompareTo(y.GetPath(0));
			}
		}

		public DateTime added;
		public StackTrace trace;
		public string path;
		public int cropCount = 2;

		public Element(StackTrace trace)
		{
			added = new DateTime();
			this.trace = trace;
		}

		public string GetPath(int cropCount)
		{
			return trace.GetFrames()
				.Skip(1)
				.Take(cropCount)
				.Reverse()
				.ToArray()
				.Join(l => MethodString(l.GetRealMethod()), " > ");
		}

		public static string MethodString(MethodBase member)
		{
			var t = member.DeclaringType;
			return $"{(GUILocator.Settings.useFullClassname ? t.FullName : t.Name)}.{member.Name}";
		}
	}
}