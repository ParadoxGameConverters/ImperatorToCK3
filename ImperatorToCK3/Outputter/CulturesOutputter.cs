using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter; 

public static class CulturesOutputter {
	public static async Task OutputCultures(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures...");
		var outputPath = Path.Combine(outputModPath, "common/culture/cultures/IRtoCK3_all_cultures.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);
		
		foreach (var culture in cultures) {
			await output.WriteLineAsync($"{culture.Id}={PDXSerializer.Serialize(culture)}");
		}

		await OutputCultureHistory(outputModPath, cultures, date);
	}
	
	private static async Task OutputCultureHistory(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures history...");
		
		foreach (var culture in cultures) {
			await culture.OutputHistory(outputModPath, date);
		}
	}
}