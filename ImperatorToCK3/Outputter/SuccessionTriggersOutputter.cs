using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class SuccessionTriggersOutputter {
	public static async Task OutputSuccessionTriggers(string outputModPath, Title.LandedTitles landedTitles, Date ck3BookmarkDate) {
		Logger.Info("Writing Succession Triggers...");

		var primogenitureTitles = new List<string>();
		var seniorityTitles = new List<string>();

		var sb = new StringBuilder();

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

		sb.AppendLine("historical_succession_access_single_heir_succession_law_trigger={");
		sb.AppendLine("\tOR={");
		foreach (var primogenitureTitle in primogenitureTitles) {
			sb.AppendLine($"\t\thas_title=title:{primogenitureTitle}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine("}");

		sb.AppendLine("historical_succession_access_single_heir_dynasty_house_trigger={");
		sb.AppendLine("\tOR={");
		foreach (var seniorityTitle in seniorityTitles) {
			sb.AppendLine($"\t\thas_title=title:{seniorityTitle}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine("}");

		var outputPath = Path.Combine(outputModPath, "common/scripted_triggers/IRToCK3_succession_triggers.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());

		Logger.IncrementProgress();
	}
}