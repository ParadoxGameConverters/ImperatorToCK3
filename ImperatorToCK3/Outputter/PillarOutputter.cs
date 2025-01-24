using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal static class PillarOutputter {
	public static async Task OutputPillars(string outputPath, PillarCollection pillars) {
		Logger.Info("Outputting pillars...");

		var sb = new System.Text.StringBuilder();
		foreach (var pillar in pillars) {
			sb.AppendLine($"{pillar.Id}={PDXSerializer.Serialize(pillar)}");
		}

		const string pillarFileName = "IRtoCK3_all_pillars.txt";
		var outputFilePath = Path.Combine(outputPath, "common/culture/pillars", pillarFileName);
		await using var output = FileHelper.OpenWriteWithRetries(outputFilePath, System.Text.Encoding.UTF8);
		await output.WriteAsync(sb.ToString());

		// There may be other pillar files due to being in the removable_file_blocks*.txt or replaceable_file_blocks*.txt file.
		// Their contents are already in the IRtoCK3_all_pillars.txt file outputted above, so we can remove the extra files.
		var pillarFiles = Directory.GetFiles(Path.Combine(outputPath, "common/culture/pillars"), "*.txt");
		foreach (var file in pillarFiles) {
			if (Path.GetFileName(file) == pillarFileName) {
				continue;
			}
			File.Delete(file);
		}
	}
}