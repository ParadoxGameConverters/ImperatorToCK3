using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class PillarOutputter {
	public static async Task OutputPillars(string outputPath, PillarCollection pillars) {
		Logger.Info("Outputting pillars...");
		var outputFilePath = Path.Combine(outputPath, "common/culture/pillars/IRtoCK3_all_pillars.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputFilePath, System.Text.Encoding.UTF8);
		
		foreach (var pillar in pillars) {
			await output.WriteLineAsync($"{pillar.Id}={PDXSerializer.Serialize(pillar)}");
		}
	}
}