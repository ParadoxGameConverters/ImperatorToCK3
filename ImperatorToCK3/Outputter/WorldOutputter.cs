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

			var outputPath = Path.Combine("output", config.OutputModName);

			Logger.Info("Copying named colors...");
			SystemUtils.TryCopyFile(
				Path.Combine(config.ImperatorPath, "game", "common", "named_colors", "default_colors.txt"),
				Path.Combine(outputPath, "common", "named_colors", "imp_colors.txt")
			);

			Logger.Info("Copying Coats of Arms...");
			ColoredEmblemsOutputter.CopyColoredEmblems(config, imperatorMods);
			CoatOfArmsOutputter.OutputCoas(outputName, ck3World.LandedTitles);
			SystemUtils.TryCopyFolder(
				Path.Combine(config.ImperatorPath, "game", "gfx", "coat_of_arms", "patterns"),
				Path.Combine(outputPath, "gfx", "coat_of_arms", "patterns")
			);

			Logger.Info("Copying blankMod files to output...");
			SystemUtils.TryCopyFolder(
				Path.Combine("blankMod", "output"),
				outputPath
			);

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
			using var modFile = new StreamWriter(Path.Combine("output", $"{outputName}.mod"));
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
			var outputPath = Path.Combine("output", outputName);

			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "titles"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "characters"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "provinces"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "province_mapping"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmarks"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmark_portraits"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "coat_of_arms"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "coat_of_arms", "coat_of_arms"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "dna_data"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "dynasties"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "landed_titles"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "named_colors"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "scripted_triggers"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace", "english"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace", "french"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace", "german"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace", "russian"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace", "simp_chinese"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace", "spanish"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms", "colored_emblems"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms", "patterns"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "interface"));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "interface", "bookmarks"));
		}
	}
}
