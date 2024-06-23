using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;
public static class SuccessionTriggersOutputter {
	public static async Task OutputSuccessionTriggers(string outputModPath, Title.LandedTitles landedTitles, Date ck3BookmarkDate) {
		Logger.Info("Writing Succession Triggers...");
		
		var outputPath = Path.Combine(outputModPath, "common/scripted_triggers/IRToCK3_succession_triggers.txt");

		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		var primogenitureTitles = new List<string>();
		var seniorityTitles = new List<string>();

		foreach (var landedTitle in landedTitles) {
			if (landedTitle.GetDeFactoLiege(ck3BookmarkDate) is not null) {
				continue;
			}

			var laws = landedTitle.GetSuccessionLaws(ck3BookmarkDate);
			if (laws.Contains("single_heir_succession_law")) {
				primogenitureTitles.Add(landedTitle.Id);
			}
			if (laws.Contains("single_heir_dynasty_house")) {
				seniorityTitles.Add(landedTitle.Id);
			}
		}

		await output.WriteLineAsync("historical_succession_access_single_heir_succession_law_trigger={");
		await output.WriteLineAsync("\tOR={");
		foreach (var primogenitureTitle in primogenitureTitles) {
			await output.WriteLineAsync($"\t\thas_title=title:{primogenitureTitle}");
		}
		await output.WriteLineAsync("\t}");
		await output.WriteLineAsync("}");

		await output.WriteLineAsync("historical_succession_access_single_heir_dynasty_house_trigger={");
		await output.WriteLineAsync("\tOR={");
		foreach (var seniorityTitle in seniorityTitles) {
			await output.WriteLineAsync($"\t\thas_title=title:{seniorityTitle}");
		}
		await output.WriteLineAsync("\t}");
		await output.WriteLineAsync("}");
		
		Logger.IncrementProgress();
	}
}