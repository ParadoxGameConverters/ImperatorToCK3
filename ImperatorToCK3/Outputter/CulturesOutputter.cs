using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using System.IO;

namespace ImperatorToCK3.Outputter; 

public static class CulturesOutputter {
	public static void OutputCultures(string outputModName, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures...");
		var outputModPath = Path.Combine("output", outputModName);
		var outputPath = Path.Combine(outputModPath, "common/culture/cultures/IRtoCK3_all_cultures.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);
		
		foreach (var culture in cultures) {
			output.WriteLine($"{culture.Id}={PDXSerializer.Serialize(culture)}");
		}

		OutputCultureHistory(outputModPath, cultures, date);
	}
	
	private static void OutputCultureHistory(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures history...");
		
		foreach (var culture in cultures) {
			culture.OutputHistory(outputModPath, date);
		}
	}
}