using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Text;
using System.Threading;

namespace ImperatorToCK3.Outputter;

public static class WorldOutputter {
	public static void OutputWorld(World ck3World, Imperator.World imperatorWorld, Configuration config) {
		ClearOutputModFolder(config);

		var outputName = config.OutputModName;
		CreateModFolder(outputName);
		OutputModFile(outputName);

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

		PillarOutputter.OutputPillars(outputName, ck3World.CulturalPillars);
		CulturesOutputter.OutputCultures(outputName, ck3World.Cultures);

		ReligionsOutputter.OutputHolySites(outputName, ck3World.Religions);
		Logger.IncrementProgress();
		ReligionsOutputter.OutputReligions(outputName, ck3World.Religions);
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

		Logger.Info("Writing game start on-action...");
		OnActionOutputter.OutputCustomGameStartOnAction(config);
		if (config.FallenEagleEnabled) {
			Logger.Info("Disabling unneeded Fallen Eagle on-actions...");
			OnActionOutputter.DisableUnneededFallenEagleOnActions(config.OutputModName);

			Logger.Info("Removing struggle start from Fallen Eagle on-actions...");
			OnActionOutputter.RemoveStruggleStartFromFallenEagleOnActions(ck3World.ModFS, config.OutputModName);
		}
		Logger.IncrementProgress();

		if (config.LegionConversion == LegionConversion.MenAtArms) {
			MenAtArmsOutputter.OutputMenAtArms(outputName, ck3World.ModFS, ck3World.Characters, ck3World.MenAtArmsTypes);
		}

		var outputPath = Path.Combine("output", config.OutputModName);

		WriteDummyStruggleHistory(outputPath);

		NamedColorsOutputter.OutputNamedColors(outputName, imperatorWorld.NamedColors, ck3World.NamedColors);

		CoatOfArmsEmblemsOutputter.CopyEmblems(config, imperatorWorld.ModFS);
		CoatOfArmsOutputter.OutputCoas(outputName, ck3World.LandedTitles, ck3World.Dynasties);
		CoatOfArmsOutputter.CopyCoaPatterns(imperatorWorld.ModFS, outputPath);

		CopyBlankModFilesToOutput(outputPath);

		BookmarkOutputter.OutputBookmark(ck3World, config);

		OutputPlaysetInfo(ck3World, outputName);
	}

	private static void WriteDummyStruggleHistory(string outputPath) {
		Logger.Info("Writing dummy struggles history file...");
		// Just to make sure the history/struggles folder exists.
		string struggleDummyPath = Path.Combine(outputPath, "history/struggles/IRToCK3_dummy.txt");
		File.WriteAllText(struggleDummyPath, string.Empty, Encoding.UTF8);
	}

	private static void CopyBlankModFilesToOutput(string outputPath) {
		Logger.Info("Copying blankMod files to output...");
		SystemUtils.TryCopyFolder(
			Path.Combine("blankMod", "output"),
			outputPath
		);
		Logger.IncrementProgress();
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
			// Try 5 times to delete the directory.
			// This is to avoid "The directory not empty" errors resulting from the OS not having
			// finished deleting files in the directory.
			var tries = 0;
			bool success = false;
			while (tries < 5) {
				try {
					dir.Delete(recursive: true);
					success = true;
					break;
				} catch (IOException) {
					Logger.Debug($"Attempt {tries+1} to delete \"{dir.FullName}\" failed.");
					Thread.Sleep(50);
					++tries;
				}
			}
			if (!success) {
				Logger.Error($"Failed to delete \"{dir.FullName}\"!");
			}
		}

		Logger.IncrementProgress();
	}

	private static void OutputModFile(string outputName) {
		var modFileBuilder = new StringBuilder();
		modFileBuilder.AppendLine($"name = \"Converted - {outputName}\"");
		modFileBuilder.AppendLine($"path = \"mod/{outputName}\"");
		modFileBuilder.AppendLine("replace_path=\"common/bookmarks\"");
		modFileBuilder.AppendLine("replace_path=\"common/culture/cultures\"");
		modFileBuilder.AppendLine("replace_path=\"common/culture/pillars\"");
		modFileBuilder.AppendLine("replace_path=\"common/landed_titles\"");
		modFileBuilder.AppendLine("replace_path=\"common/religion/religions\"");
		modFileBuilder.AppendLine("replace_path=\"history/characters\"");
		modFileBuilder.AppendLine("replace_path=\"history/province_mapping\"");
		modFileBuilder.AppendLine("replace_path=\"history/provinces\"");
		modFileBuilder.AppendLine("replace_path=\"history/struggles\"");
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
		Logger.Info("Creating folders...");
		
		var outputPath = Path.Combine("output", outputName);

		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "titles"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "characters"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "provinces"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "province_mapping"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "struggles"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "history", "wars"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmarks", "bookmarks"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmarks", "groups"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "bookmark_portraits"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "coat_of_arms", "coat_of_arms"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "culture", "cultures"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "culture", "pillars"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "dna_data"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "dynasties"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "landed_titles"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "men_at_arms_types"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "named_colors"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "common", "on_action"));
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
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms", "colored_emblems"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms", "patterns"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "coat_of_arms", "textured_emblems"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "interface", "bookmarks"));
		SystemUtils.TryCreateFolder(Path.Combine(outputPath, "gfx", "portraits", "portrait_modifiers"));
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
		using var output = FileOpeningHelper.OpenWriteWithRetries(outFilePath, Encoding.UTF8);

		foreach (var mod in modsForPlayset) {
			output.WriteLine($"{mod.Name.AddQuotes()}={mod.Path.AddQuotes()}");
		}

		Logger.IncrementProgress();
	}
}