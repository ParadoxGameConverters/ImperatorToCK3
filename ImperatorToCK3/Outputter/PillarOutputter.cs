using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Cultures;
using System.IO;

namespace ImperatorToCK3.Outputter;

public static class PillarOutputter {
	public static void OutputPillars(string outputModName, PillarCollection pillars) {
		Logger.Info("Outputting pillars...");
		var outputPath = Path.Combine("output", outputModName, "common/culture/pillars/IRtoCK3_all_pillars.txt");
		using var outputStream = File.OpenWrite(outputPath);
		using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);
		
		foreach (var pillar in pillars) {
			output.WriteLine($"{pillar.Id}={PDXSerializer.Serialize(pillar)}");
		}
	}
}