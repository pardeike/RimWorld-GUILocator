using System.Diagnostics;
using UnityEngine;
using Verse;

namespace GUILocator
{
	public class GUILocatorSettings : ModSettings
	{
		public string dnSpyPath = $"C:\\Program Files (x86)\\dnSpy\\dnSpy.exe";
		public string dllPath = $"C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll";

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref dnSpyPath, "dnSpyPath", dnSpyPath);
			Scribe_Values.Look(ref dllPath, "dllPath", dllPath);
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

			_ = list.Label("Path to Assembly-CSharp.dll");
			GUILocator.Settings.dllPath = list.TextEntry(GUILocator.Settings.dllPath);
			list.End();
		}
	}
}