using commonItems;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Outputter;
public static class SuccessionTriggersOutputter {
	public static void OutputSuccessionTriggers(string outputModName, Title.LandedTitles landedTitles, Date ck3BookmarkDate) {
		var outputPath = Path.Combine("output", outputModName, "common/scripted_triggers/IRToCK3_succession_triggers.txt");

		using var outputStream = File.OpenWrite(outputPath);
		using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);

		var primogenitureTitles = new List<string>();
		var seniorityTitles = new List<string>();

		foreach (var landedTitle in landedTitles) {
			if (landedTitle.GetDeFactoLiege(ck3BookmarkDate) is null) {
				if (landedTitle.SuccessionLaws.Contains("single_heir_succession_law")) {
					primogenitureTitles.Add(landedTitle.Id);
				}
				if (landedTitle.SuccessionLaws.Contains("single_heir_dynasty_house")) {
					seniorityTitles.Add(landedTitle.Id);
				}
			}
		}

		output.WriteLine("historical_succession_access_single_heir_succession_law_trigger = {");
		output.WriteLine(" OR = {");
		foreach (var primogenitureTitle in primogenitureTitles) {
			output.WriteLine($"  has_title = title:{primogenitureTitle}");
		}
		output.WriteLine(" }");
		output.WriteLine("}");

		output.WriteLine("historical_succession_access_single_heir_dynasty_house_trigger = {");
		output.WriteLine(" OR = {");
		foreach (var seniorityTitle in seniorityTitles) {
			output.WriteLine($"  has_title = title:{seniorityTitle}");
		}
		output.WriteLine(" }");
		output.WriteLine("}");
	}
}