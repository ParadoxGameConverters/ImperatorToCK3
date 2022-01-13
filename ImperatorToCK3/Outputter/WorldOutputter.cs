using commonItems;
using ImperatorToCK3.CK3;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Outputter {
	public static class WorldOutputter {
		public static void OutputWorld(World ck3World, IEnumerable<Mod> imperatorMods, Configuration config) {
			ClearOutputModFolder();

			var outputName = config.OutputModName;
			CreateModFolder(outputName);
			OutputModFile(outputName);

			Logger.Info("Creating folders...");
			CreateFolders(outputName);

			Logger.Info("Writing Characters...");
			CharactersOutputter.OutputCharacters(outputName, ck3World.Characters, ck3World.CorrectedDate);

			Logger.Info("Writing Dynasties...");
			DynastiesOutputter.OutputDynasties(outputName, ck3World.Dynasties);

			Logger.Info("Writing Provinces...");
			ProvincesOutputter.OutputProvinces(outputName, ck3World.Provinces, ck3World.LandedTitles);

			Logger.Info("Writing Landed Titles...");
			TitlesOutputter.OutputTitles(
				outputName,
				ck3World.LandedTitles,
				config.ImperatorDeJure
			);

			Logger.Info("Writing Succession Triggers...");
			SuccessionTriggersOutputter.OutputSuccessionTriggers(outputName, ck3World.LandedTitles, config.CK3BookmarkDate);

			Logger.Info("Writing Localization...");
			LocalizationOutputter.OutputLocalization(
				config.ImperatorPath,
				outputName,
				ck3World,
				config.ImperatorDeJure
			);

			var outputPath = $"output/{config.OutputModName}";

			Logger.Info("Copying named colors...");
			SystemUtils.TryCopyFile($"{config.ImperatorPath}/game/common/named_colors/default_colors.txt",
									 $"{outputPath}/common/named_colors/imp_colors.txt");

			Logger.Info("Copying Coats of Arms...");
			ColoredEmblemsOutputter.CopyColoredEmblems(config, imperatorMods);
			CoatOfArmsOutputter.OutputCoas(outputName, ck3World.LandedTitles);
			SystemUtils.TryCopyFolder($"{config.ImperatorPath}/game/gfx/coat_of_arms/patterns",
							$"{outputPath}/gfx/coat_of_arms/patterns");

			Logger.Info("Copying blankMod files to output...");
			SystemUtils.TryCopyFolder("blankMod/output", outputPath);

			Logger.Info("Creating bookmark...");
			BookmarkOutputter.OutputBookmark(ck3World, config);

			void ClearOutputModFolder() {
				var directoryToClear = $"output/{config.OutputModName}";
				var di = new DirectoryInfo(directoryToClear);
				if (!di.Exists) {
					return;
				}

				Logger.Info("Clearing the output mod folder...");
				foreach (FileInfo file in di.EnumerateFiles()) {
					file.Delete();
				}
				foreach (DirectoryInfo dir in di.EnumerateDirectories()) {
					dir.Delete(true);
				}
			}
		}

		private static void OutputModFile(string outputName) {
			using var modFile = new StreamWriter($"output/{outputName}.mod");
			modFile.WriteLine($"name = \"Converted - {outputName}\"");
			modFile.WriteLine($"path = \"mod/{outputName}\"");
			modFile.WriteLine("replace_path = \"common/landed_titles\"");
			modFile.WriteLine("replace_path = \"history/province_mapping\"");
			modFile.WriteLine("replace_path = \"history/provinces\"");
			modFile.WriteLine("replace_path = \"history/titles\"");
		}

		private static void CreateModFolder(string outputName) {
			var modPath = Path.Combine("output", outputName);
			SystemUtils.TryCreateFolder(modPath);
		}

		private static void CreateFolders(string outputName) {
			SystemUtils.TryCreateFolder($"output/{outputName}/history");
			SystemUtils.TryCreateFolder($"output/{outputName}/history/titles");
			SystemUtils.TryCreateFolder($"output/{outputName}/history/characters");
			SystemUtils.TryCreateFolder($"output/{outputName}/history/provinces");
			SystemUtils.TryCreateFolder($"output/{outputName}/history/province_mapping");
			SystemUtils.TryCreateFolder($"output/{outputName}/common");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/bookmarks");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/bookmark_portraits");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/coat_of_arms");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/coat_of_arms/coat_of_arms");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/dna_data");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/dynasties");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/landed_titles");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/named_colors");
			SystemUtils.TryCreateFolder($"output/{outputName}/common/scripted_triggers");
			SystemUtils.TryCreateFolder($"output/{outputName}/localization");
			SystemUtils.TryCreateFolder($"output/{outputName}/localization/replace");
			SystemUtils.TryCreateFolder($"output/{outputName}/localization/replace/english");
			SystemUtils.TryCreateFolder($"output/{outputName}/localization/replace/french");
			SystemUtils.TryCreateFolder($"output/{outputName}/localization/replace/german");
			SystemUtils.TryCreateFolder($"output/{outputName}/localization/replace/russian");
			SystemUtils.TryCreateFolder($"output/{outputName}/localization/replace/simp_chinese");
			SystemUtils.TryCreateFolder($"output/{outputName}/localization/replace/spanish");
			SystemUtils.TryCreateFolder($"output/{outputName}/gfx");
			SystemUtils.TryCreateFolder($"output/{outputName}/gfx/coat_of_arms");
			SystemUtils.TryCreateFolder($"output/{outputName}/gfx/coat_of_arms/colored_emblems");
			SystemUtils.TryCreateFolder($"output/{outputName}/gfx/coat_of_arms/patterns");
			SystemUtils.TryCreateFolder($"output/{outputName}/gfx/interface");
			SystemUtils.TryCreateFolder($"output/{outputName}/gfx/interface/bookmarks");
		}
	}
}
