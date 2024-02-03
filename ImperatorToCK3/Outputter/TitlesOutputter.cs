using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter;
public static class TitlesOutputter {
	private static void OutputTitlesHistory(string outputModName, Title.LandedTitles titles) {
		//output title history
		var alreadyOutputtedTitles = new HashSet<string>();
		foreach (var title in titles) {
			// first output kingdoms + their de jure vassals to files named after the kingdoms

			if (title.Rank != TitleRank.kingdom || title.DeJureVassals.Count == 0) {
				// title is a not de jure kingdom
				continue;
			}

			var historyOutputPath = Path.Combine("output", outputModName, "history", "titles", $"{title.Id}.txt");
			using var historyOutput = new StreamWriter(historyOutputPath); // output the kingdom's history
			title.OutputHistory(historyOutput);
			alreadyOutputtedTitles.Add(title.Id);

			// output the kingdom's de jure vassals' history
			foreach (var (deJureVassalName, deJureVassal) in title.GetDeJureVassalsAndBelow()) {
				deJureVassal.OutputHistory(historyOutput);
				alreadyOutputtedTitles.Add(deJureVassalName);
			}
		}

		var otherTitlesPath = Path.Combine("output", outputModName, "history", "titles", "00_other_titles.txt");
		using (var historyOutput = new StreamWriter(otherTitlesPath)) {
			foreach (var title in titles) {
				// output the remaining titles
				if (alreadyOutputtedTitles.Contains(title.Id)) {
					continue;
				}
				title.OutputHistory(historyOutput);
				alreadyOutputtedTitles.Add(title.Id);
			}
		}
	}

	public static void OutputTitles(string outputModName, Title.LandedTitles titles) {
		var outputPath = Path.Combine("output", outputModName, "common", "landed_titles", "00_landed_titles.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		foreach (var (name, value) in titles.Variables) {
			output.WriteLine($"@{name}={value}");
		}

		// titles with a de jure liege will be outputted under the liege
		var topDeJureTitles = titles.Where(t => t.DeJureLiege is null);
		output.Write(PDXSerializer.Serialize(topDeJureTitles, string.Empty, false));

		OutputTitlesHistory(outputModName, titles);
	}
}