using System.Collections.Generic;
using System.IO;
using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;

namespace ImperatorToCK3.Outputter {
	public static class TitlesOutputter {
		public static void OutputTitleHistory(Title title, StreamWriter writer) {
			writer.WriteLine(title.Name + " = {");

			writer.WriteLine("\t867.1.1 = {");

			var deFactoLiege = title.DeFactoLiege;
			if (deFactoLiege is not null) {
				writer.WriteLine("\t\tliege = " + deFactoLiege.Name);
			}

			writer.WriteLine("\t\tholder = " + title.HolderID);

			if (title.Government is not null) {
				writer.WriteLine("\t\tgovernment = " + title.Government);
			}

			var succLaws = title.SuccessionLaws;
			if (succLaws.Count > 0) {
				writer.WriteLine("\t\tsuccession_laws = {");
				foreach (var law in succLaws) {
					writer.WriteLine("\t\t\t" + law);
				}
				writer.WriteLine("\t\t}");
			}

			if (title.Rank != TitleRank.barony) {
				var developmentLevelOpt = title.DevelopmentLevel;
				if (developmentLevelOpt is not null) {
					writer.WriteLine("\t\tchange_development_level = " + developmentLevelOpt);
				}
			}

			writer.WriteLine("\t}");

			writer.WriteLine("}");
		}

		public static void OutputTitlesHistory(string outputModName, Dictionary<string, Title> titles) {
			//output title history
			var alreadyOutputtedTitles = new HashSet<string>();
			foreach (var (name, title) in titles) { // first output kindoms + their de jure vassals to files named after the kingdoms
				if (title.Rank == TitleRank.kingdom && title.DeJureVassals.Count > 0) { // is a de jure kingdom
					var historyOutputPath = Path.Combine("output", outputModName, "history", "titles", name + ".txt");
					using var historyOutput = new StreamWriter(historyOutputPath);                      // output the kingdom's history
					OutputTitleHistory(title, historyOutput);
					alreadyOutputtedTitles.Add(name);

					// output the kingdom's de jure vassals' history
					foreach (var (deJureVassalName, deJureVassal) in title.GetDeJureVassalsAndBelow()) {
						OutputTitleHistory(deJureVassal, historyOutput);
						alreadyOutputtedTitles.Add(deJureVassalName);
					}
				}
			}

			var otherTitlesPath = Path.Combine("output", outputModName, "history/titles/00_other_titles.txt");
			using (var historyOutput = new StreamWriter(otherTitlesPath)) {
				foreach (var (name, title) in titles) { // output the remaining titles
					if (!alreadyOutputtedTitles.Contains(name)) {
						OutputTitleHistory(title, historyOutput);
						alreadyOutputtedTitles.Add(name);
					}
				}
			}
		}

		public static void OutputTitles(string outputModName, string ck3Path, Dictionary<string, Title> titles, IMPERATOR_DE_JURE deJure) {
			//output to landed_titles folder
			foreach (var (name, title) in titles) {
				var impCountry = title.ImperatorCountry;
				if (impCountry is not null && impCountry.CountryType != CountryType.real) { // we don't need pirates, barbarians etc.
					continue;
				}

				if (title.IsImportedOrUpdatedFromImperator && name.IndexOf("IMPTOCK3") != -1) {  // title is not from CK3
					var outputPath = Path.Combine("output", outputModName, "common", "landed_titles", name + ".txt");
					using var output = new StreamWriter(outputPath);
					output.Write(CommonFunctions.UTF8BOM);
					TitleOutputter.OutputTitle(output, title);
				}
			}
			if (deJure == IMPERATOR_DE_JURE.REGIONS) {
				if (!SystemUtils.TryCopyFolder("blankMod/optionalFiles/ImperatorDeJure/common/landed_titles", "output/" + outputModName + "/common/landed_titles/")) {
					Logger.Error("Could not copy ImperatorDeJure landed titles!");
				}
			}

			OutputTitlesHistory(outputModName, titles);
		}
	}
}
