using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class NamedColorsOutputter {
	/// <summary>
	/// Outputs named colors that exist in <paramref name="imperatorNamedColors"/> but not in <paramref name="ck3NamedColors"/>.
	/// </summary>
	/// <param name="outputModPath"></param>
	/// <param name="imperatorNamedColors"></param>
	/// <param name="ck3NamedColors"></param>
	public static async Task OutputNamedColors(string outputModPath, NamedColorCollection imperatorNamedColors, NamedColorCollection ck3NamedColors) {
		var diff = imperatorNamedColors.Where(colorPair => !ck3NamedColors.ContainsKey(colorPair.Key))
			.ToList();
		if (diff.Count == 0) {
			return;
		}

		Logger.Info("Outputting named colors from Imperator game and mods...");

		var outputPath = Path.Combine(outputModPath, "common", "named_colors", "IRtoCK3_colors_from_Imperator.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		await output.WriteLineAsync("colors = {");
		foreach (var (name, color) in diff) {
			await output.WriteLineAsync($"\t{name}={color.OutputRgb()}");
		}
		await output.WriteLineAsync("}");

		Logger.IncrementProgress();
	}
}