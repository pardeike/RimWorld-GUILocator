using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;

namespace GUILocator
{
	public static class State
	{
		static readonly Dictionary<string, MethodBase> originals = new Dictionary<string, MethodBase>();
		static readonly Dictionary<MethodBase, Element> calls = new Dictionary<MethodBase, Element>();

		public static MethodBase GetRealMethod(this StackFrame frame)
		{
			var member = frame.GetMethod();
			if (originals.TryGetValue(member.Name, out var original))
				return original;
			if (member.Name.Contains("DMD<DMD"))
				Log.Warning($"Found untranslated member [{member.Name}]");
			return member;
		}

		public static void PresentMenu()
		{
			var elements = calls.Values.ToList();
			calls.Clear();

			if (elements.Count == 0) return;
			elements.Sort(new Element.Comparer());

			var mouse = UI.MousePositionOnUI;
			FloatMenuUtility.MakeMenu(elements.AsEnumerable(), element => element.GetPath(GUILocator.Settings.numberOfMethodsToDisplay), element => delegate ()
			{
				var options = element.trace
					.GetFrames()
					.Skip(1)
					.Select(f => f.GetRealMethod())
					.Select(member =>
					{
						var path = member.DeclaringType.Assembly.Location;
						if (path == null || path.Length == 0)
						{
							var contentPack = LoadedModManager.RunningMods.FirstOrDefault(m => m.assemblies.loadedAssemblies.Contains(member.DeclaringType.Assembly));
							if (contentPack != null)
							{
								path = ModContentPack.GetAllFilesForModPreserveOrder(contentPack, "Assemblies/", p => p.ToLower() == ".dll", null)
									.Select(fileInfo => fileInfo.Item2.FullName)
									.First(dll =>
									{
										var assembly = Assembly.ReflectionOnlyLoadFrom(dll);
										return assembly.GetType(member.DeclaringType.FullName) != null;
									});
							}
						}

						var option = Misc.FloatMenuOption(Element.MethodString(member), delegate ()
						{
							var token = member.MetadataToken;
							if (token != 0)
								_ = Process.Start(GUILocator.Settings.dnSpyPath, $"\"{path}\" --select 0x{token:X8}");
						});
						option.Disabled = member.MetadataToken == 0;
						return option;
					});
				if (options.Any())
					Find.WindowStack.Add(new FloatMenu(options.ToList()));
			});
		}

		static void PatchPostfix(MethodBase __result, MethodBase original)
		{
			originals[__result.Name] = original;
		}

		public static void RegisterOriginalsPatch(Harmony harmony)
		{
			var t_PatchFunctions = AccessTools.TypeByName("HarmonyLib.PatchFunctions");
			var m_UpdateWrapper = AccessTools.Method(t_PatchFunctions, "UpdateWrapper");
			var m_PatchPostfix = SymbolExtensions.GetMethodInfo(() => PatchPostfix(null, null));
			_ = harmony.Patch(m_UpdateWrapper, postfix: new HarmonyMethod(m_PatchPostfix));
		}

		public static void Add(StackTrace trace)
		{
			var key = trace.GetFrame(1).GetRealMethod();
			calls[key] = new Element(trace);
		}
	}
}
