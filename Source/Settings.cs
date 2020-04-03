using System.Diagnostics;
using UnityEngine;
using Verse;

namespace GUILocator
{
	public class GUILocatorSettings : ModSettings
	{
		public string dnSpyPath = $"C:\\Program Files (x86)\\dnSpy\\dnSpy.exe";
		public int numberOfMethodsToDisplay = 3;
		public bool useFullClassname = false;
		public bool sortByAddedOrder = false;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref dnSpyPath, "dnSpyPath", dnSpyPath);
			Scribe_Values.Look(ref numberOfMethodsToDisplay, "numberOfMethodsToDisplay", numberOfMethodsToDisplay);
			Scribe_Values.Look(ref useFullClassname, "useFullClassname", useFullClassname);
			Scribe_Values.Look(ref sortByAddedOrder, "sortByAddedOrder", sortByAddedOrder);
		}

		static bool ButtonText(Listing_Standard list, string label)
		{
			var rect = list.GetRect(30f);
			rect.width = 160;
			var result = Widgets.ButtonText(rect, label);
			list.Gap(list.verticalSpacing);
			return result;
		}

		public static void DoWindowContents(Rect canvas)
		{
			var list = new Listing_Standard { ColumnWidth = canvas.width };
			list.Begin(canvas);
			list.Gap();

			_ = list.Label("Path to dnSpy.exe");
			GUILocator.Settings.dnSpyPath = list.TextEntry(GUILocator.Settings.dnSpyPath);
			if (ButtonText(list, "Download"))
				_ = Process.Start("https://github.com/0xd4d/dnSpy/releases/latest");
			list.Gap();

			_ = list.Label($"Number of methods to display: {GUILocator.Settings.numberOfMethodsToDisplay}");
			GUILocator.Settings.numberOfMethodsToDisplay = (int)list.Slider(GUILocator.Settings.numberOfMethodsToDisplay, 1, 6);
			list.Gap();

			list.CheckboxLabeled("Use full class name", ref GUILocator.Settings.useFullClassname);
			list.Gap();

			list.CheckboxLabeled("Sort elements by order they are added", ref GUILocator.Settings.sortByAddedOrder);
			list.End();
		}
	}
}