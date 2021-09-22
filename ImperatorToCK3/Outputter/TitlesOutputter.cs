using System.Collections.Generic;
using System.IO;
using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;

namespace ImperatorToCK3.Outputter {
	public static class TitlesOutputter {
		public static void OutputTitlesHistory(string outputModName, Dictionary<string, Title> titles, Date ck3BookmarkDate) {
			//output title history
			var alreadyOutputtedTitles = new HashSet<string>();
			foreach (var (name, title) in titles) { // first output kindoms + their de jure vassals to files named after the kingdoms
				if (title.Rank == TitleRank.kingdom && title.DeJureVassals.Count > 0) { // is a de jure kingdom
					var historyOutputPath = Path.Combine("output", outputModName, "history", "titles", name + ".txt");
					using var historyOutput = new StreamWriter(historyOutputPath); // output the kingdom's history
					title.OutputHistory(historyOutput, ck3BookmarkDate);
					alreadyOutputtedTitles.Add(name);

					// output the kingdom's de jure vassals' history
					foreach (var (deJureVassalName, deJureVassal) in title.GetDeJureVassalsAndBelow()) {
						deJureVassal.OutputHistory(historyOutput, ck3BookmarkDate);
						alreadyOutputtedTitles.Add(deJureVassalName);
					}
				}
			}

			var otherTitlesPath = Path.Combine("output", outputModName, "history/titles/00_other_titles.txt");
			using (var historyOutput = new StreamWriter(otherTitlesPath)) {
				foreach (var (name, title) in titles) { // output the remaining titles
					if (!alreadyOutputtedTitles.Contains(name)) {
						title.OutputHistory(historyOutput, ck3BookmarkDate);
						alreadyOutputtedTitles.Add(name);
					}
				}
			}
		}

		public static void OutputTitles(string outputModName, string ck3Path, Dictionary<string, Title> titles, IMPERATOR_DE_JURE deJure, Date ck3BookmarkDate) {
			//output to landed_titles folder
			foreach (var (name, title) in titles) {
				var impCountry = title.ImperatorCountry;
				if (impCountry is not null && impCountry.CountryType != CountryType.real) { // we don't need pirates, barbarians etc.
					continue;
				}

				if (title.IsImportedOrUpdatedFromImperator && name.Contains("IMPTOCK3")) {  // title is not from CK3
					var outputPath = Path.Combine("output", outputModName, "common", "landed_titles", name + ".txt");
					using var outputStream = File.OpenWrite(outputPath);
					using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);
					TitleOutputter.OutputTitle(output, title);
				}
			}
			if (deJure == IMPERATOR_DE_JURE.REGIONS) {
				if (!SystemUtils.TryCopyFolder("blankMod/optionalFiles/ImperatorDeJure/common/landed_titles", "output/" + outputModName + "/common/landed_titles/")) {
					Logger.Error("Could not copy ImperatorDeJure landed titles!");
				}
			}

			OutputTitlesHistory(outputModName, titles, ck3BookmarkDate);
		}
	}
}
