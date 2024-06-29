using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class CulturesOutputter {
	public static async Task OutputCultures(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures...");

		var sb = new StringBuilder();
		foreach (var culture in cultures) {
			sb.AppendLine($"{culture.Id}={PDXSerializer.Serialize(culture)}");
		}

		var outputPath = Path.Combine(outputModPath, "common/culture/cultures/IRtoCK3_all_cultures.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());

		await OutputCultureHistory(outputModPath, cultures, date);
	}

	private static async Task OutputCultureHistory(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures history...");

		foreach (var culture in cultures) {
			await culture.OutputHistory(outputModPath, date);
		}
	}
}