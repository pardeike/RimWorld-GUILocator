using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;

namespace GUILocator
{
	public static class State
	{
		public static bool useFullClassname = false;
		public static int numberOfMethodsToDisplay = 3;
		public static Dictionary<string, Element> calls = new Dictionary<string, Element>();

		public static void PresentMenu()
		{
			var elements = calls.Values.ToList();
			if (elements.Count == 0) return;
			elements.Sort();

			var mouse = UI.MousePositionOnUI;
			FloatMenuUtility.MakeMenu(elements.AsEnumerable(), element => element.GetPath(numberOfMethodsToDisplay), element => delegate ()
			{
				var options = element.trace
					.GetFrames()
					.Skip(1)
					.Select(f => f.GetMethod())
					.Select(method =>
					{
						var label = $"{method.DeclaringType.FullName}.{Element.MethodString(method)}";
						return new FloatMenuOption(label, delegate ()
						{
							var token = method.MetadataToken;
							if (token != 0)
								_ = Process.Start(GUILocator.Settings.dnSpyPath, $"\"{GUILocator.Settings.dllPath}\" --select 0x{token:X8}");
						},
						MenuOptionPriority.Default, null, null, 0f, null, null)
						{
							Disabled = method.MetadataToken == 0
						};
					});
				if (options.Any())
					Find.WindowStack.Add(new FloatMenu(options.ToList()));
			});

			calls.Clear();
		}
	}
}