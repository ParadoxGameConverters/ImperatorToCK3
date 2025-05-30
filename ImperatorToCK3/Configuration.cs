﻿using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ImperatorToCK3;

public enum LegionConversion { No, SpecialTroops, MenAtArms }
public sealed class Configuration {
	public string SaveGamePath { get; set; } = "";
	public string ImperatorPath { get; set; } = "";
	public string ImperatorDocPath { get; set; } = "";
	public string CK3Path { get; set; } = "";
	public string CK3ModsPath { get; set; } = "";
	public OrderedSet<string> SelectedCK3Mods { get; } = new();
	public string OutputModName { get; set; } = "";
	public bool HeresiesInHistoricalAreas { get; set; } = false;
	public bool StaticDeJure { get; set; } = false;
	public bool FillerDukes { get; set; } = true;
	public bool UseCK3Flags { get; set; } = true;
	public double ImperatorCurrencyRate { get; set; } = 1.0d;
	public double ImperatorCivilizationWorth { get; set; } = 0.4;
	public LegionConversion LegionConversion { get; set; } = LegionConversion.MenAtArms;
	public Date CK3BookmarkDate { get; set; } = new(0, 1, 1);
	public bool SkipDynamicCoAExtraction { get; set; } = false;
	public bool FallenEagleEnabled { get; private set; }
	public bool WhenTheWorldStoppedMakingSenseEnabled { get; private set; }
	public bool RajasOfAsiaEnabled { get; private set; }
	public bool AsiaExpansionProjectEnabled { get; private set; }

	public bool OutputCCUParameters => WhenTheWorldStoppedMakingSenseEnabled || FallenEagleEnabled || RajasOfAsiaEnabled;

	public Configuration() { }
	public Configuration(ConverterVersion converterVersion) {
		Logger.Info("Reading configuration file...");
		var parser = new Parser();
		RegisterKeys(parser);
		const string configurationPath = "configuration.txt";
		if (!File.Exists(configurationPath)) {
			throw new ConverterException($"{configurationPath} not found! Run ConverterFrontend to generate it.");
		}
		parser.ParseFile(configurationPath);

		SetOutputName();
		VerifyImperatorPath();
		VerifyImperatorVersion(converterVersion);
		VerifyCK3Path();
		VerifyCK3Version(converterVersion);
		VerifyImperatorDocPath();
		VerifyCK3ModsPath();

		Logger.IncrementProgress();
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("SaveGame", reader => {
			SaveGamePath = reader.GetString();
			Logger.Info($"Save game set to: {SaveGamePath}");
		});
		parser.RegisterKeyword("ImperatorDirectory", reader => ImperatorPath = reader.GetString());
		parser.RegisterKeyword("ImperatorDocDirectory", reader => ImperatorDocPath = reader.GetString());
		parser.RegisterKeyword("CK3directory", reader => CK3Path = reader.GetString());
		parser.RegisterKeyword("targetGameModPath", reader => CK3ModsPath = reader.GetString());
		parser.RegisterKeyword("selectedMods", reader => {
			SelectedCK3Mods.UnionWith(reader.GetStrings());
			Logger.Info($"{SelectedCK3Mods.Count} mods selected by configuration.");
		});
		parser.RegisterKeyword("output_name", reader => {
			OutputModName = reader.GetString();
			Logger.Info($"Output name set to: {OutputModName}");
		});
		parser.RegisterKeyword("HeresiesInHistoricalAreas", reader => {
			var valueString = reader.GetString();
			try {
				HeresiesInHistoricalAreas = Convert.ToInt32(valueString, CultureInfo.InvariantCulture) == 1;
				Logger.Info($"{nameof(HeresiesInHistoricalAreas)} set to: {HeresiesInHistoricalAreas}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(HeresiesInHistoricalAreas)} value was: {valueString}; Error message: {e}");
			}
		});
		parser.RegisterKeyword("StaticDeJure", reader => {
			var valueString = reader.GetString();
			try {
				StaticDeJure = Convert.ToInt32(valueString, CultureInfo.InvariantCulture) == 2;
				Logger.Info($"{nameof(StaticDeJure)} set to: {StaticDeJure}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(StaticDeJure)} value was: {valueString}; Error message: {e}");
			}
		});
		parser.RegisterKeyword("FillerDukes", reader => {
			var valueString = reader.GetString();
			try {
				FillerDukes = Convert.ToInt32(valueString, CultureInfo.InvariantCulture) == 1;
				Logger.Info($"{nameof(FillerDukes)} set to: {FillerDukes}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(FillerDukes)} value was: {valueString}; Error message: {e}");
			}
		});
		parser.RegisterKeyword("UseCK3Flags", reader => {
			var valueString = reader.GetString();
			try {
				UseCK3Flags = Convert.ToInt32(valueString, CultureInfo.InvariantCulture) == 1;
				Logger.Info($"{nameof(UseCK3Flags)} set to: {UseCK3Flags}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(UseCK3Flags)} value was: {valueString}; Error message: {e}");
			}
		});
		parser.RegisterKeyword("ImperatorCurrencyRate", reader => {
			ImperatorCurrencyRate = reader.GetDouble();
			Logger.Info($"{nameof(ImperatorCurrencyRate)} set to: {ImperatorCurrencyRate}");
		});
		parser.RegisterKeyword("ImperatorCivilizationWorth", reader => {
			ImperatorCivilizationWorth = reader.GetDouble();
			Logger.Info($"{nameof(ImperatorCivilizationWorth)} set to: {ImperatorCivilizationWorth}");
		});
		parser.RegisterKeyword("LegionConversion", reader => {
			var valueString = reader.GetString();
			var success = Enum.TryParse(valueString, out LegionConversion selection);
			if (success) {
				LegionConversion = selection;
				Logger.Info($"{nameof(LegionConversion)} set to {LegionConversion}.");
			} else {
				Logger.Warn($"Failed to parse {valueString} as value for {nameof(LegionConversion)}.");
			}
		});
		parser.RegisterKeyword("bookmark_date", reader => {
			var dateStr = reader.GetString();
			if (string.IsNullOrEmpty(dateStr)) {
				return;
			}

			Logger.Info($"Entered CK3 bookmark date: {dateStr}");
			CK3BookmarkDate = new Date(dateStr);
			var earliestAllowedDate = new Date(2,1,1);
			if (CK3BookmarkDate < earliestAllowedDate) {
				Logger.Warn($"CK3 bookmark date cannot be earlier than {earliestAllowedDate} AD (Y.M.D format), you should fix your configuration. Setting to earliest allowed date...");
				CK3BookmarkDate = earliestAllowedDate;
				Logger.Info($"Changed CK3 bookmark date to {earliestAllowedDate}");
			}
			Logger.Info($"CK3 bookmark date set to: {CK3BookmarkDate}");
		});
		parser.RegisterKeyword("SkipDynamicCoAExtraction", reader => {
			var valueString = reader.GetString();
			try {
				SkipDynamicCoAExtraction = Convert.ToInt32(valueString, CultureInfo.InvariantCulture) == 1;
				Logger.Info($"{nameof(SkipDynamicCoAExtraction)} set to: {SkipDynamicCoAExtraction}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(SkipDynamicCoAExtraction)} value was: {valueString}; Error message: {e}");
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	private void VerifyImperatorPath() {
		if (!Directory.Exists(ImperatorPath)) {
			throw new UserErrorException($"{ImperatorPath} does not exist!");
		}

		var binariesPath = Path.Combine(ImperatorPath, "binaries");
		var imperatorExePath = Path.Combine(binariesPath, "imperator");
		if (OperatingSystem.IsWindows()) {
			imperatorExePath += ".exe";
		}

		bool installVerified = File.Exists(imperatorExePath);
		if (!installVerified) {
			try {
				var appIdPath = Path.Combine(binariesPath, "steam_appid.txt");
				var appId = File.ReadAllText(appIdPath).Trim();
				if (appId == "859580") {
					installVerified = true;
				}
			} catch(Exception e) {
				Logger.Debug($"Exception was raised when checking I:R steam_appid: {e.Message}");
			}
		}

		if (installVerified) {
			Logger.Info($"\tI:R install path is {ImperatorPath}");
		} else {
			throw new UserErrorException($"{ImperatorPath} does not contain Imperator: Rome!");
		}
	}

	private void VerifyCK3Path() {
		if (!Directory.Exists(CK3Path)) {
			throw new UserErrorException($"{CK3Path} does not exist!");
		}

		var binariesPath = Path.Combine(CK3Path, "binaries");
		var ck3ExePath = Path.Combine(binariesPath, "ck3");
		if (OperatingSystem.IsWindows()) {
			ck3ExePath += ".exe";
		}

		bool installVerified = File.Exists(ck3ExePath);
		if (!installVerified) {
			try {
				var appIdPath = Path.Combine(binariesPath, "steam_appid.txt");
				var appId = File.ReadAllText(appIdPath).Trim();
				if (appId == "1158310") {
					installVerified = true;
				}
			} catch(Exception e) {
				Logger.Debug($"Exception was raised when checking CK3 steam_appid: {e.Message}");
			}
		}

		if (installVerified) {
			Logger.Info($"\tCK3 install path is {CK3Path}");
		} else{
			throw new UserErrorException($"{CK3Path} does not contain Crusader Kings III!");
		}
	}

	private void VerifyImperatorDocPath() {
		if (!Directory.Exists(ImperatorDocPath)) {
			throw new UserErrorException($"{ImperatorDocPath} does not exist!");
		}

		string[] dirsInDocFolder = ["mod/", "logs/", "save_games/", "cache/"];
		string[] filesInDocFolder = [
			"continue_game.json", "dlc_load.json", "dlc_signature", "game_data.json", "pdx_settings.txt"
		];
		// If at least one of the paths exists, we consider the folder to be valid.
		bool docFolderVerified = dirsInDocFolder.Any(dir => Directory.Exists(Path.Combine(ImperatorDocPath, dir)));
		if (!docFolderVerified) {
			docFolderVerified = filesInDocFolder.Any(file => File.Exists(Path.Combine(ImperatorDocPath, file)));
		}

		if (!docFolderVerified) {
			throw new UserErrorException($"{ImperatorDocPath} is not a valid I:R documents path!\n" +
			                             "It should contain one of the following files: " +
			                             $"{string.Join(", ", filesInDocFolder)}");
		}
		
		Logger.Debug($"I:R documents path {ImperatorDocPath} is valid.");
	}
	
	private void VerifyCK3ModsPath() {
		if (!Directory.Exists(CK3ModsPath)) {
			throw new UserErrorException($"{CK3ModsPath} does not exist!");
		}
		
		// If the mods folder contains any files, at least one on them should have a .mod extension.
		var filesInFolder = Directory.GetFiles(CK3ModsPath);
		if (filesInFolder.Length > 0) {
			var modFiles = filesInFolder.Where(f => f.EndsWith(".mod", StringComparison.OrdinalIgnoreCase));
			if (!modFiles.Any()) {
				throw new UserErrorException($"{CK3ModsPath} does not contain any .mod files!");
			}
		}
	}

	private void SetOutputName() {
		if (string.IsNullOrWhiteSpace(OutputModName)) {
			OutputModName = CommonFunctions.TrimExtension(CommonFunctions.TrimPath(SaveGamePath));
		}
		OutputModName = OutputModName.Replace('-', '_');
		OutputModName = OutputModName.Replace(' ', '_');

		OutputModName = CommonFunctions.NormalizeUTF8Path(OutputModName);
		Logger.Info($"Using output name {OutputModName}");
	}

	private void VerifyImperatorVersion(ConverterVersion converterVersion) {
		var path = Path.Combine(ImperatorPath, "launcher/launcher-settings.json");
		var irVersion = GameVersion.ExtractVersionFromLauncher(path);
		if (irVersion is null) {
			Logger.Error("Imperator version could not be determined, proceeding blind!");
			return;
		}

		Logger.Info($"Imperator version: {irVersion.ToShortString()}");

		if (converterVersion.MinSource > irVersion) {
			Logger.Error($"Imperator version is v{irVersion.ToShortString()}, converter requires minimum v{converterVersion.MinSource.ToShortString()}!");
			throw new UserErrorException("Converter vs Imperator installation mismatch!");
		}
		if (!converterVersion.MaxSource.IsLargerishThan(irVersion)) {
			Logger.Error($"Imperator version is v{irVersion.ToShortString()}, converter requires maximum v{converterVersion.MaxSource.ToShortString()}!");
			throw new UserErrorException("Converter vs Imperator installation mismatch!");
		}
	}

	private void VerifyCK3Version(ConverterVersion converterVersion) {
		var path = Path.Combine(CK3Path, "launcher/launcher-settings.json");
		var ck3Version = GameVersion.ExtractVersionFromLauncher(path);
		if (ck3Version is null) {
			Logger.Error("CK3 version could not be determined, proceeding blind!");
			return;
		}

		Logger.Info($"CK3 version: {ck3Version.ToShortString()}");

		if (converterVersion.MinTarget > ck3Version) {
			Logger.Error($"CK3 version is v{ck3Version.ToShortString()}, converter requires minimum v{converterVersion.MinTarget.ToShortString()}!");
			throw new UserErrorException("Converter vs CK3 installation mismatch!");
		}
		if (!converterVersion.MaxTarget.IsLargerishThan(ck3Version)) {
			Logger.Error($"CK3 version is v{ck3Version.ToShortString()}, converter requires maximum v{converterVersion.MaxTarget.ToShortString()}!");
			throw new UserErrorException("Converter vs CK3 installation mismatch!");
		}
	}

	public void DetectSpecificCK3Mods(ICollection<Mod> loadedMods) {
		var tfeMod = loadedMods.FirstOrDefault(m => m.Name.StartsWith("The Fallen Eagle", StringComparison.Ordinal));
		if (tfeMod is not null) {
			FallenEagleEnabled = true;
			Logger.Info($"TFE detected: {tfeMod.Name}");
		}
		
		var wtwsmsMod = loadedMods.FirstOrDefault(m => m.Name.StartsWith("When the World Stopped Making Sense", StringComparison.Ordinal));
		if (wtwsmsMod is not null) {
			WhenTheWorldStoppedMakingSenseEnabled = true;
			Logger.Info($"WtWSMS detected: {wtwsmsMod.Name}");
		}
		
		var roaMod = loadedMods.FirstOrDefault(m => m.Name.StartsWith("Rajas of Asia", StringComparison.Ordinal));
		if (roaMod is not null) {
			RajasOfAsiaEnabled = true;
			Logger.Info($"RoA detected: {roaMod.Name}");
		}
		
		var aepMod = loadedMods.FirstOrDefault(m => m.Name.StartsWith("Asia Expansion Project", StringComparison.Ordinal));
		if (aepMod is not null) {
			AsiaExpansionProjectEnabled = true;
			Logger.Info($"AEP detected: {aepMod.Name}");
		}
	}

	/// <summary>Returns a collection of CK3 mod flags with values based on the enabled mods. "vanilla" flag is set to true if no other flags are set.</summary>
	public OrderedDictionary<string, bool> GetCK3ModFlags() {
		var flags = new OrderedDictionary<string, bool> {
			["tfe"] = FallenEagleEnabled,
			["wtwsms"] = WhenTheWorldStoppedMakingSenseEnabled,
			["roa"] = RajasOfAsiaEnabled,
			["aep"] = AsiaExpansionProjectEnabled,
		};

		flags["vanilla"] = !flags.Any(f => f.Value);
		return flags;
	}
	
	public IEnumerable<string> GetActiveCK3ModFlags() {
		return GetCK3ModFlags().Where(f => f.Value).Select(f => f.Key);
	}
}