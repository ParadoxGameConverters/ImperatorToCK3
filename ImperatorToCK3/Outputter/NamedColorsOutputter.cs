using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;
using System.Text;
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
			.ToArray();
		if (diff.Length == 0) {
			return;
		}

		Logger.Info("Outputting named colors from Imperator game and mods...");

		var sb = new StringBuilder();
		sb.AppendLine("colors = {");
		foreach (var (name, color) in diff) {
			sb.AppendLine($"\t{name}={color.OutputRgb()}");
		}

		sb.AppendLine("}");

		var outputPath = Path.Combine(outputModPath, "common", "named_colors", "IRtoCK3_colors_from_Imperator.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());

		Logger.IncrementProgress();
	}
}