using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter {
	public static class TitlesOutputter {
		private static void OutputTitlesHistory(string outputModName, LandedTitles titles, Date conversionDate) {
			//output title history
			var alreadyOutputtedTitles = new HashSet<string>();
			foreach (var (name, title) in titles) {
				// first output kingdoms + their de jure vassals to files named after the kingdoms

				if (title.Rank != TitleRank.kingdom || title.DeJureVassals.Count == 0) {
					// title is a not de jure kingdom
					continue;
				}

				var historyOutputPath = Path.Combine("output", outputModName, "history", "titles", name + ".txt");
				using var historyOutput = new StreamWriter(historyOutputPath); // output the kingdom's history
				title.OutputHistory(historyOutput, conversionDate);
				alreadyOutputtedTitles.Add(name);

				// output the kingdom's de jure vassals' history
				foreach (var (deJureVassalName, deJureVassal) in title.GetDeJureVassalsAndBelow()) {
					deJureVassal.OutputHistory(historyOutput, conversionDate);
					alreadyOutputtedTitles.Add(deJureVassalName);
				}
			}

			var otherTitlesPath = Path.Combine("output", outputModName, "history/titles/00_other_titles.txt");
			using (var historyOutput = new StreamWriter(otherTitlesPath)) {
				foreach (var (name, title) in titles) {
					// output the remaining titles
					if (alreadyOutputtedTitles.Contains(name)) {
						continue;
					}
					title.OutputHistory(historyOutput, conversionDate);
					alreadyOutputtedTitles.Add(name);
				}
			}
		}

		public static void OutputTitles(string outputModName, LandedTitles titles, IMPERATOR_DE_JURE deJure, Date conversionDate) {
			var outputPath = Path.Combine("output", outputModName, "common/landed_titles/00_landed_titles.txt");
			using var outputStream = File.OpenWrite(outputPath);
			using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);

			output.Write(PDXSerializer.Serialize(titles.Variables, string.Empty, false));
			// titles with a de jure liege will be outputted under the liege
			var topDeJureTitles = new Dictionary<string, Title>(titles.Where(pair => pair.Value.DeJureLiege is null));
			output.Write(PDXSerializer.Serialize(topDeJureTitles, string.Empty, false));

			if (deJure == IMPERATOR_DE_JURE.REGIONS) {
				if (!SystemUtils.TryCopyFolder("blankMod/optionalFiles/ImperatorDeJure/common/landed_titles", "output/" + outputModName + "/common/landed_titles/")) {
					Logger.Error("Could not copy ImperatorDeJure landed titles!");
				}
			}

			OutputTitlesHistory(outputModName, titles, conversionDate);
		}
	}
}
