using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace GUILocator
{
	public static class State
	{
		public static bool useFullClassname = false;
		public static List<string> calls = new List<string>();
	}

	[StaticConstructorOnStartup]
	class Main
	{
		static Main()
		{
			var harmony = new Harmony("net.pardeike.guilocator");
			harmony.PatchAll();
		}
	}

	[HarmonyPatch(typeof(UIRoot_Play))]
	[HarmonyPatch(nameof(UIRoot_Play.UIRootOnGUI))]
	class UIRoot_Play_UIRootOnGUI_Patch
	{
		static bool lastMouseState = false;

		static void Postfix()
		{
			var mouseState = Input.GetMouseButton(2);
			if (mouseState == lastMouseState) return;
			lastMouseState = mouseState;
			if (mouseState == false)
			{
				State.calls.Sort((a, b) => a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.Compare(a, b));
				var result = State.calls.ToArray().Join(null, "\n");
				Log.Warning($"{result}\n");
				State.calls.Clear();
			}
		}
	}

	[HarmonyPatch]
	class Patches
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			return AccessTools.GetDeclaredMethods(typeof(Widgets))
				.Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(Rect)))
				.SelectMany(m =>
				{
					if (m.IsGenericMethod == false) return new List<MethodInfo> { m };
					if (m.Name == "Dropdown") return new List<MethodInfo>()
					{
						m.MakeGenericMethod(typeof(Bill_Production), typeof(Zone_Stockpile)),
						m.MakeGenericMethod(typeof(Pawn), typeof(DrugPolicy)),
						m.MakeGenericMethod(typeof(Pawn), typeof(HostilityResponseMode)),
						m.MakeGenericMethod(typeof(Pawn), typeof(MedicalCareCategory)),
						m.MakeGenericMethod(typeof(Pawn), typeof(FoodRestriction)),
						m.MakeGenericMethod(typeof(Pawn), typeof(Outfit)),
						m.MakeGenericMethod(typeof(Pawn), typeof(Pawn))
					};
					return new List<MethodInfo>()
					{
						m.MakeGenericMethod(typeof(int)),
						m.MakeGenericMethod(typeof(float))
					};
				});
		}

		static string MethodString(MethodBase method)
		{
			var name = method.Name; // DMD<DMD<DrawWindowBackground_Patch0>?-1840475648::DrawWindowBackground_Patch0>
			var match = Regex.Match(method.Name, @"DMD<DMD<.+_Patch\d+>\?-?\d+::(.+)_Patch\d+>");
			if (match.Success)
				name = match.Groups[1].Value;
			var t = method.DeclaringType;
			return $"{(State.useFullClassname ? t.FullName : t.Name)}.{name}";
		}

		public static void TestRect(Rect rect)
		{
			if ((Mouse.IsOver(rect) == false)) return;
			if (Input.GetMouseButton(2) == false) return;
			var trace = new StackTrace(false);
			var call = trace.GetFrames()
				.Skip(1)
				.Reverse()
				.ToArray()
				.Join(l => MethodString(l.GetMethod()), " > ");
			if (State.calls.Contains(call) == false)
				State.calls.Add(call);
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
		{
			var list = instructions.ToList();
			var offset = original.IsStatic ? 0 : 1;
			var parameters = original.GetParameters();
			for (var i = 0; i < parameters.Length; i++)
				if (parameters[i].ParameterType == typeof(Rect))
				{
					list.InsertRange(0, new[]
					{
						new CodeInstruction(OpCodes.Ldarg, i + offset),
						new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => TestRect(default)))
					});
					break;
				}

			return list.AsEnumerable();
		}
	}
}