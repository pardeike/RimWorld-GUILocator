using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using Verse;

namespace GUILocator
{
	public static class State
	{
		static readonly Dictionary<MethodBase, Element> calls = new Dictionary<MethodBase, Element>();

		static string GetAssemblyPath(Assembly assembly)
		{
			if (assembly.IsDynamic) return null;
			var path = assembly.Location;
			if (File.Exists(path)) return path;

			var contentPack = LoadedModManager.RunningMods.FirstOrDefault(m => m.assemblies.loadedAssemblies.Contains(assembly));
			if (contentPack == null) return null;

			List<Tuple<string, FileInfo>> files;
			try
			{
				files = ModContentPack.GetAllFilesForModPreserveOrder(contentPack, "Assemblies/", p => p.Equals(".dll", StringComparison.OrdinalIgnoreCase), null);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException)
			{
				return null;
			}

			foreach (var file in files)
			{
				path = file.Item2.FullName;
				try
				{
					// Read identity without loading candidate assemblies or resolving their dependencies.
					if (AssemblyName.GetAssemblyName(path).FullName == assembly.FullName) return path;
				}
				catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is BadImageFormatException || ex is ArgumentException)
				{
					// A broken or unavailable candidate must not hide the remaining matches.
				}
			}
			return null;
		}

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
						var path = GetAssemblyPath(member.DeclaringType.Assembly);

						return new FloatMenuOption(member.MethodString(), delegate ()
						{
							var token = member.MetadataToken;
							if (token != 0 && File.Exists(path)) _ = Process.Start(GUILocator.Settings.dnSpyPath, $"\"{path}\" --select 0x{token:X8}");
						})
						{ Disabled = member.MetadataToken == 0 || path == null };
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
