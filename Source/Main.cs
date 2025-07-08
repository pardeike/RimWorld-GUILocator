using Brrainz;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace GUILocator
{
	class GUILocator : Mod
	{
		public static GUILocatorSettings Settings;

		public GUILocator(ModContentPack content) : base(content)
		{
			Settings = GetSettings<GUILocatorSettings>();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			GUILocatorSettings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "GUILocator";
		}
	}

	[StaticConstructorOnStartup]
	public static class Patcher
	{
		static Patcher()
		{
			var harmony = new Harmony("net.pardeike.rimworld.mods.guilocator");
			harmony.PatchAll();
			Log.Warning("GUILocator enabled - middle mouse click on GUI to activate");

			CrossPromotion.Install(76561197973010050);
		}

		public static MethodBase GetOriginalFromStackframe(this StackFrame frame)
		{
			if (frame == null) return null;
			var member = Harmony.GetMethodFromStackframe(frame);
			if (member is MethodInfo methodInfo)
			{
				var original = Harmony.GetOriginalMethod(methodInfo);
				member = original ?? member;
			}
			return member;
		}

		public static string MethodString(this MethodBase member)
		{
			var t = member?.DeclaringType;
			if (t == null) return null;
			return $"{(GUILocator.Settings.useFullClassname ? t.FullName : t.Name)}.{member.Name}";
		}
	}

	[HarmonyPatch(typeof(Root))]
	[HarmonyPatch(nameof(Root.OnGUI))]
	class UIRoot_Play_UIRootOnGUI_Patch
	{
		static bool lastMouseState = false;

		static void Postfix()
		{
			var mouseState = Input.GetMouseButton(2);
			if (mouseState == lastMouseState) return;
			lastMouseState = mouseState;
			if (mouseState) return;
			State.PresentMenu();
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
					if (m.IsGenericMethod == false) return [m];
					if (m.Name == "Dropdown") return
					[
						m.MakeGenericMethod(typeof(Bill_Production), typeof(Pawn)),
						m.MakeGenericMethod(typeof(Pawn), typeof(DrugPolicy)),
						m.MakeGenericMethod(typeof(ITab_Pawn_Feeding.BabyFeederPair), typeof(AutofeedMode)),
						m.MakeGenericMethod(typeof(PenFoodCalculator), typeof(ThingDef)),
						m.MakeGenericMethod(typeof(Pawn), typeof(MedicalCareCategory)),
						m.MakeGenericMethod(typeof(Pawn), typeof(ThingDef)),
						m.MakeGenericMethod(typeof(Pawn), typeof(MechanitorControlGroup)),
						m.MakeGenericMethod(typeof(Pawn), typeof(FoodPolicy)),
						m.MakeGenericMethod(typeof(Pawn), typeof(ApparelPolicy)),
						m.MakeGenericMethod(typeof(Pawn), typeof(ReadingPolicy)),
						m.MakeGenericMethod(typeof(Pawn), typeof(Pawn)),
						m.MakeGenericMethod(typeof(Pawn), typeof(HostilityResponseMode)),
					];
					return new List<MethodInfo>()
					{
						m.MakeGenericMethod(typeof(int)),
						m.MakeGenericMethod(typeof(float))
					};
				});
		}

		public static void TestRect(Rect rect)
		{
			if (Find.WindowStack == null) return;
			if ((Mouse.IsOver(rect) == false)) return;
			if (Input.GetMouseButton(2) == false) return;
			if (Event.current.type == EventType.Repaint) return;
			if (Event.current.type == EventType.Layout) return;
			Event.current.Use();
			var trace = new StackTrace(false);
			State.Add(trace);
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
