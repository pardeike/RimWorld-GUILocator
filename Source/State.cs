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
		static readonly Dictionary<MethodBase, Element> calls = new Dictionary<MethodBase, Element>();

		public static void PresentMenu()
		{
			var elements = calls.Values.ToList();
			calls.Clear();

			if (elements.Count == 0) return;
			elements.Sort(new Element.Comparer());

			var mouse = UI.MousePositionOnUI;
			FloatMenuUtility.MakeMenu(elements.ToList(), element => element.GetPath(GUILocator.Settings.numberOfMethodsToDisplay), element => delegate ()
			{
				var options = element.trace
					.GetFrames()
					.Skip(1)
					.Select(f => f.GetOriginalFromStackframe())
					.Where(member => member?.DeclaringType != null)
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

						return new FloatMenuOption(member.MethodString(), delegate ()
						{
							var token = member.MetadataToken;
							if (token != 0) _ = Process.Start(GUILocator.Settings.dnSpyPath, $"\"{path}\" --select 0x{token:X8}");
						})
						{ Disabled = member.MetadataToken == 0 };
					});

				if (options.Any())
					Find.WindowStack.Add(new FloatMenu(options.ToList()));
			});
		}

		public static void Add(StackTrace trace)
		{
			var key = Harmony.GetMethodFromStackframe(trace.GetFrame(1));
			calls[key] = new Element(trace);
		}
	}
}
