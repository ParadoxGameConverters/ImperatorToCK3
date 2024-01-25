using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter;

public static class NamedColorsOutputter {
	/// <summary>
	/// Outputs named colors that exist in <paramref name="imperatorNamedColors"/> but not in <paramref name="ck3NamedColors"/>.
	/// </summary>
	/// <param name="outputModName"></param>
	/// <param name="imperatorNamedColors"></param>
	/// <param name="ck3NamedColors"></param>
	public static void OutputNamedColors(string outputModName, NamedColorCollection imperatorNamedColors, NamedColorCollection ck3NamedColors) {
		var diff = imperatorNamedColors.Where(colorPair => !ck3NamedColors.ContainsKey(colorPair.Key))
			.ToList();
		if (!diff.Any()) {
			return;
		}

		Logger.Info("Outputting named colors from Imperator game and mods...");

		var outputPath = Path.Combine("output", outputModName, "common", "named_colors", "IRtoCK3_colors_from_Imperator.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		output.WriteLine("colors = {");
		foreach (var (name, color) in diff) {
			output.WriteLine($"\t{name}={color.OutputRgb()}");
		}
		output.WriteLine("}");

		Logger.IncrementProgress();
	}
}