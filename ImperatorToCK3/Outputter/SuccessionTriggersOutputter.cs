using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Outputter;
public static class SuccessionTriggersOutputter {
	public static void OutputSuccessionTriggers(string outputModName, Title.LandedTitles landedTitles, Date ck3BookmarkDate) {
		var outputPath = Path.Combine("output", outputModName, "common", "scripted_triggers", "IRToCK3_succession_triggers.txt");

		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

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

		output.WriteLine("historical_succession_access_single_heir_succession_law_trigger={");
		output.WriteLine("\tOR={");
		foreach (var primogenitureTitle in primogenitureTitles) {
			output.WriteLine($"\t\thas_title=title:{primogenitureTitle}");
		}
		output.WriteLine("\t}");
		output.WriteLine("}");

		output.WriteLine("historical_succession_access_single_heir_dynasty_house_trigger={");
		output.WriteLine("\tOR={");
		foreach (var seniorityTitle in seniorityTitles) {
			output.WriteLine($"\t\thas_title=title:{seniorityTitle}");
		}
		output.WriteLine("\t}");
		output.WriteLine("}");
	}
}