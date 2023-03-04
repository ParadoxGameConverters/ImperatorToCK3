using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3;
using System.IO;
using System.Text;

namespace ImperatorToCK3.Outputter;

public static class WorldOutputter {
	public static void OutputWorld(World ck3World, Imperator.World imperatorWorld, Configuration config) {
		ClearOutputModFolder(config);

		var outputName = config.OutputModName;
		CreateModFolder(outputName);
		OutputModFile(outputName);

		Logger.Info("Creating folders...");
		CreateFolders(outputName);
		Logger.IncrementProgress();

		Logger.Info("Writing Characters...");
		CharactersOutputter.OutputCharacters(outputName, ck3World.Characters, ck3World.CorrectedDate);
		Logger.IncrementProgress();

		Logger.Info("Writing Dynasties...");
		DynastiesOutputter.OutputDynasties(outputName, ck3World.Dynasties);
		Logger.IncrementProgress();

		Logger.Info("Writing Provinces...");
		ProvincesOutputter.OutputProvinces(outputName, ck3World.Provinces, ck3World.LandedTitles);
		Logger.IncrementProgress();

		Logger.Info("Writing Landed Titles...");
		TitlesOutputter.OutputTitles(
			outputName,
			ck3World.LandedTitles
		);
		Logger.IncrementProgress();

		ReligionsOutputter.OutputHolySites(outputName, ck3World.Religions);
		Logger.IncrementProgress();
		ReligionsOutputter.OutputModifiedReligions(outputName, ck3World.Religions);
		Logger.IncrementProgress();

		WarsOutputter.OutputWars(outputName, ck3World.Wars);

		Logger.Info("Writing Succession Triggers...");
		SuccessionTriggersOutputter.OutputSuccessionTriggers(outputName, ck3World.LandedTitles, config.CK3BookmarkDate);
		Logger.IncrementProgress();

		Logger.Info("Writing Localization...");
		LocalizationOutputter.OutputLocalization(
			imperatorWorld.ModFS,
			outputName,
			ck3World
		);
		Logger.IncrementProgress();

		if (config.LegionConversion == LegionConversion.MenAtArms) {
			MenAtArmsOutputter.OutputMenAtArms(outputName, ck3World.ModFS, ck3World.Characters, ck3World.MenAtArmsTypes);
		}

		var outputPath = Path.Combine("output", config.OutputModName);

		NamedColorsOutputter.OutputNamedColors(outputName, imperatorWorld.NamedColors, ck3World.NamedColors);

		ColoredEmblemsOutputter.CopyColoredEmblems(config, imperatorWorld.ModFS);
		CoatOfArmsOutputter.OutputCoas(outputName, ck3World.LandedTitles, ck3World.Dynasties);
		CoatOfArmsOutputter.CopyCoaPatterns(imperatorWorld.ModFS, outputPath);

		CopyBlankModFilesToOutput(outputPath);

		BookmarkOutputter.OutputBookmark(ck3World, config);

		if (config.RiseOfIslam) {
			CopyRiseOfIslamFilesToOutput(config);
		}
		Logger.IncrementProgress();

		OutputPlaysetInfo(ck3World, outputName);
	}

	private static void CopyBlankModFilesToOutput(string outputPath) {
		Logger.Info("Copying blankMod files to output...");
		SystemUtils.TryCopyFolder(
			Path.Combine("blankMod", "output"),
			outputPath
		);
		Logger.IncrementProgress();
	}

	private static void CopyRiseOfIslamFilesToOutput(Configuration config) {
		Logger.Info("Copying Rise of Islam files to output...");
		var outputPath = Path.Combine("output", config.OutputModName);
		const string riseOfIslamFilesPath = "blankMod/optionalFiles/RiseOfIslam";
		foreach (var fileName in SystemUtils.GetAllFilesInFolderRecursive(riseOfIslamFilesPath)) {
			var sourceFilePath = Path.Combine(riseOfIslamFilesPath, fileName);
			var destFilePath = Path.Combine(outputPath, fileName);

			var destDir = Path.GetDirectoryName(destFilePath);
			if (destDir is not null) {
				SystemUtils.TryCreateFolder(destDir);
			}
			File.Copy(sourceFilePath, destFilePath, true);
		}
	}

	private static void ClearOutputModFolder(Configuration config) {
		Logger.Info("Clearing the output mod folder...");

		var directoryToClear = $"output/{config.OutputModName}";
		var di = new DirectoryInfo(directoryToClear);
		if (!di.Exists) {
			return;
		}

		foreach (FileInfo file in di.EnumerateFiles()) {
			file.Delete();
		}
		foreach (DirectoryInfo dir in di.EnumerateDirectories()) {
			dir.Delete(true);
		}

		Logger.IncrementProgress();
	}

	private static void OutputModFile(string outputName) {
		var modFileBuilder = new StringBuilder();
		modFileBuilder.AppendLine($"name = \"Converted - {outputName}\"");
		modFileBuilder.AppendLine($"path = \"mod/{outputName}\"");
		modFileBuilder.AppendLine("replace_path=\"common/bookmarks\"");
		modFileBuilder.AppendLine("replace_path=\"common/landed_titles\"");
		modFileBuilder.AppendLine("replace_path=\"history/province_mapping\"");
		modFileBuilder.AppendLine("replace_path=\"history/provinces\"");
		modFileBuilder.AppendLine("replace_path=\"history/titles\"");
		modFileBuilder.AppendLine("replace_path=\"history/wars\"");
		var modText = modFileBuilder.ToString();

		var modFilePath = Path.Combine("output", $"{outputName}.mod");
		var descriptorFilePath = Path.Combine("output", outputName, "descriptor.mod");
		File.WriteAllText(modFilePath, modText);
		File.WriteAllText(descriptorFilePath, modText);
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
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "wars"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmarks"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmarks", "bookmarks"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmarks", "groups"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmark_portraits"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "casus_belli_types"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "character_interactions"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "coat_of_arms"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "coat_of_arms", "coat_of_arms"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "council_tasks"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "dna_data"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "dynasties"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "dynasty_houses"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "landed_titles"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "men_at_arms_types"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "named_colors"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "religion"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "religion", "holy_sites"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "religion", "religions"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "scripted_triggers"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "events"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gui"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace"));
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", language));
			SystemUtils.TryCreateFolder(Path.Combine(outputPath, "localization", "replace", language));
		}
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms", "colored_emblems"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms", "patterns"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "interface"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "interface", "bookmarks"));
	}

	private static void OutputPlaysetInfo(World ck3World, string outputModName) {
		Logger.Info("Outputting CK3 playset info...");

		var modsForPlayset = new OrderedSet<Mod>();
		foreach (var loadedMod in ck3World.LoadedMods) {
			if (loadedMod.Name == "blankMod") {
				modsForPlayset.Add(new Mod(name: $"Converted - {outputModName}", path: outputModName));
			} else {
				modsForPlayset.Add(loadedMod);
			}
		}

		const string outFilePath = "playset_info.txt";
		if (File.Exists(outFilePath)) {
			File.Delete(outFilePath);
		}
		using var outputStream = File.OpenWrite(outFilePath);
		using var output = new StreamWriter(outputStream, Encoding.UTF8);

		foreach (var mod in modsForPlayset) {
			output.WriteLine($"{mod.Name.AddQuotes()}={mod.Path.AddQuotes()}");
		}

		Logger.IncrementProgress();
	}
}