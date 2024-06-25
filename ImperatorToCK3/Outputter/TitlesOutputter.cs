using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;
public static class TitlesOutputter {
	private static async Task OutputTitlesHistory(string outputModPath, Title.LandedTitles titles) {
		//output title history
		var alreadyOutputtedTitles = new HashSet<string>();
		foreach (var title in titles) {
			// first output kingdoms + their de jure vassals to files named after the kingdoms

			if (title.Rank != TitleRank.kingdom || title.DeJureVassals.Count == 0) {
				// title is a not de jure kingdom
				continue;
			}

			var historyOutputPath = Path.Combine(outputModPath, "history", "titles", $"{title.Id}.txt");
			await using var historyOutput = new StreamWriter(historyOutputPath); // output the kingdom's history
			await title.OutputHistory(historyOutput);
			alreadyOutputtedTitles.Add(title.Id);

			// output the kingdom's de jure vassals' history
			foreach (var (deJureVassalName, deJureVassal) in title.GetDeJureVassalsAndBelow()) {
				await deJureVassal.OutputHistory(historyOutput);
				alreadyOutputtedTitles.Add(deJureVassalName);
			}
		}

		var otherTitlesPath = Path.Combine(outputModPath, "history", "titles", "00_other_titles.txt");
		await using (var historyOutput = new StreamWriter(otherTitlesPath)) {
			foreach (var title in titles) {
				// output the remaining titles
				if (alreadyOutputtedTitles.Contains(title.Id)) {
					continue;
				}
				await title.OutputHistory(historyOutput);
				alreadyOutputtedTitles.Add(title.Id);
			}
		}
	}

	public static async Task OutputTitles(string outputModPath, Title.LandedTitles titles) {
		Logger.Info("Writing Landed Titles...");
		
		var sb = new System.Text.StringBuilder();
		foreach (var (name, value) in titles.Variables) {
			sb.AppendLine($"@{name}={value}");
		}

		// titles with a de jure liege will be outputted under the liege
		var topDeJureTitles = titles.Where(t => t.DeJureLiege is null);
		sb.Append(PDXSerializer.Serialize(topDeJureTitles, string.Empty, false));
		
		var outputPath = Path.Combine(outputModPath, "common/landed_titles/00_landed_titles.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);
		await output.WriteAsync(sb.ToString());

		await OutputTitlesHistory(outputModPath, titles);
		Logger.IncrementProgress();
	}
}