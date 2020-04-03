using HarmonyLib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GUILocator
{
	public class Element : IComparable<Element>
	{
		public StackTrace trace;
		public string path;
		public int cropCount = 2;

		public Element(StackTrace trace)
		{
			this.trace = trace;
		}

		public string GetPath(int cropCount)
		{
			return trace.GetFrames()
				.Skip(1)
				.Take(cropCount)
				.Reverse()
				.ToArray()
				.Join(l => MethodString(l.GetMethod()), " > ");
		}

		public static string MethodString(MethodBase method)
		{
			var name = method.Name; // DMD<DMD<DrawWindowBackground_Patch0>?-1840475648::DrawWindowBackground_Patch0>
			var match = Regex.Match(method.Name, @"DMD<DMD<.+_Patch\d+>\?-?\d+::(.+)_Patch\d+>");
			if (match.Success)
				name = match.Groups[1].Value;
			var t = method.DeclaringType;
			return $"{(State.useFullClassname ? t.FullName : t.Name)}.{name}";
		}

		public int CompareTo(Element other)
		{
			return string.Compare(GetPath(0), other.GetPath(0));
		}
	}
}
