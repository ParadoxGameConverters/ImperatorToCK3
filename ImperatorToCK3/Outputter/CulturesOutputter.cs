using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using System.IO;

namespace ImperatorToCK3.Outputter; 

public static class CulturesOutputter {
	public static void OutputCultures(string outputModName, CultureCollection cultures) {
		Logger.Info("Outputting cultures...");
		var outputPath = Path.Combine("output", outputModName, "common/culture/cultures/IRtoCK3_all_cultures.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);
		
		foreach (var culture in cultures) {
			output.WriteLine($"{culture.Id}={PDXSerializer.Serialize(culture)}");
		}
	}
}