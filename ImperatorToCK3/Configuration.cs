using commonItems;
using commonItems.Collections;
using commonItems.Exceptions;
using commonItems.Mods;
using DotLiquid;
using ImperatorToCK3.CommonUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ImperatorToCK3;

internal enum LegionConversion { No, SpecialTroops, MenAtArms }
internal sealed class Configuration {
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
	public float ImperatorCurrencyRate { get; set; } = 1.0f;
	public double ImperatorCivilizationWorth { get; set; } = 0.4;
	public LegionConversion LegionConversion { get; set; } = LegionConversion.MenAtArms;
	public Date CK3BookmarkDate { get; set; } = new(0, 1, 1);
	public bool SkipDynamicCoAExtraction { get; set; } = false;
	public bool SkipHoldingOwnersImport { get; set; } = true;
	public GameVersion IRVersion { get; private set; } = new();
	public GameVersion CK3Version { get; private set; } = new();
	public bool FallenEagleEnabled { get; private set; }
	public bool WhenTheWorldStoppedMakingSenseEnabled { get; private set; }
	public bool RajasOfAsiaEnabled { get; private set; }
	public bool AsiaExpansionProjectEnabled { get; private set; }

	public bool OutputCCUParameters => WhenTheWorldStoppedMakingSenseEnabled || FallenEagleEnabled || RajasOfAsiaEnabled;

	public Configuration() { }
	public Configuration(ConverterVersion converterVersion) {
		
		
		
		Logger.Info("Reading configuration file...");
		var parser = new Parser();
		RegisterConfigurationKeys(parser);
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

	private void ReadFronterOptions() {
		// fronter-options.txt is one directory above configuration.txt.
		const string fronterOptionsPath = "../fronter-options.txt";
		if (!File.Exists(fronterOptionsPath)) {
			Logger.Warn($"{fronterOptionsPath} not found! Skipping fronter options loading.");
			return;
		}
	}

	private void RegisterConfigurationKeys(Parser parser) {
		parser.RegisterKeyword("SaveGame", reader => {
			SaveGamePath = reader.GetString();
			Logger.Info($"Save game set to: {SaveGamePath}");
		});
		parser.RegisterKeyword("ImperatorDirectory", reader => ImperatorPath = PathHelper.RemoveTrailingSeparators(reader.GetString()));
		parser.RegisterKeyword("ImperatorDocDirectory", reader => ImperatorDocPath = PathHelper.RemoveTrailingSeparators(reader.GetString()));
		parser.RegisterKeyword("CK3directory", reader => CK3Path = PathHelper.RemoveTrailingSeparators(reader.GetString()));
		parser.RegisterKeyword("targetGameModPath", reader => CK3ModsPath = PathHelper.RemoveTrailingSeparators(reader.GetString()));
		parser.RegisterKeyword("selectedMods", reader => {
			SelectedCK3Mods.Clear();
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
			ImperatorCurrencyRate = reader.GetFloat();
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
		parser.RegisterKeyword("SkipHoldingOwnersImport", reader => {
			var valueString = reader.GetString();
			try {
				SkipHoldingOwnersImport = Convert.ToInt32(valueString, CultureInfo.InvariantCulture) == 1;
				Logger.Info($"{nameof(SkipHoldingOwnersImport)} set to: {SkipHoldingOwnersImport}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(SkipHoldingOwnersImport)} value was: {valueString}; Error message: {e}");
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

		var normalizedCK3ModsPath = Path.TrimEndingDirectorySeparator(
			CK3ModsPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
		);
		var expectedSuffix = Path.Combine("Paradox Interactive", "Crusader Kings III", "mod");
		var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		if (!normalizedCK3ModsPath.EndsWith(expectedSuffix, comparison)) {
			throw new UserErrorException($"{CK3ModsPath} is not a valid CK3 mods directory! It should end with {expectedSuffix}.");
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
		IRVersion = GameVersion.ExtractVersionFromLauncher(path) ??
		            GetImperatorVersionFromSullaBranchFile() ??
		            throw new ConverterException("Imperator version could not be determined.");

		Logger.Info($"Imperator version: {IRVersion.ToShortString()}");

		if (converterVersion.MinSource > IRVersion) {
			Logger.Error($"Imperator version is v{IRVersion.ToShortString()}, converter requires minimum v{converterVersion.MinSource.ToShortString()}!");
			throw new UserErrorException("Converter vs Imperator installation mismatch!");
		}
		if (!converterVersion.MaxSource.IsLargerishThan(IRVersion)) {
			Logger.Error($"Imperator version is v{IRVersion.ToShortString()}, converter requires maximum v{converterVersion.MaxSource.ToShortString()}!");
			throw new UserErrorException("Converter vs Imperator installation mismatch!");
		}
	}

	private GameVersion? GetImperatorVersionFromSullaBranchFile() {
		const string sullaBranchFileName = "sulla_branch.txt";
		var path = Path.Combine(ImperatorPath, sullaBranchFileName);

		if (!File.Exists(path)) {
			Logger.Warn($"{sullaBranchFileName} not found");
			return null;
		}

		// The file contains the game version in the following format: release/X.Y.Z.
		var versionStr = File.ReadAllText(path).Trim().Replace("release/", "");
		var version = new GameVersion(versionStr);
		return version;
	}

	private void VerifyCK3Version(ConverterVersion converterVersion) {
		var path = Path.Combine(CK3Path, "launcher/launcher-settings.json");
		CK3Version = GameVersion.ExtractVersionFromLauncher(path) ??
		             GetCK3VersionFromTitusBranchFile() ??
		             throw new ConverterException("CK3 version could not be determined.");

		Logger.Info($"CK3 version: {CK3Version.ToShortString()}");

		if (converterVersion.MinTarget > CK3Version) {
			Logger.Error($"CK3 version is v{CK3Version.ToShortString()}, converter requires minimum v{converterVersion.MinTarget.ToShortString()}!");
			throw new UserErrorException("Converter vs CK3 installation mismatch!");
		}
		if (!converterVersion.MaxTarget.IsLargerishThan(CK3Version)) {
			Logger.Error($"CK3 version is v{CK3Version.ToShortString()}, converter requires maximum v{converterVersion.MaxTarget.ToShortString()}!");
			throw new UserErrorException("Converter vs CK3 installation mismatch!");
		}
	}

	private GameVersion? GetCK3VersionFromTitusBranchFile() {
		const string titusBranchFileName = "titus_branch.txt";
		var path = Path.Combine(CK3Path, titusBranchFileName);

		if (!File.Exists(path)) {
			Logger.Warn($"{titusBranchFileName} not found");
			return null;
		}

		// The file contains the game version in the following format: release/X.Y.Z.
		var versionStr = File.ReadAllText(path).Trim().Replace("release/", "");
		var version = new GameVersion(versionStr);
		return version;
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

		ThrowUserErrorExceptionForUnsupportedModCombinations();
	}

	private void ThrowUserErrorExceptionForUnsupportedModCombinations() {
		if (FallenEagleEnabled && WhenTheWorldStoppedMakingSenseEnabled) {
			throw new UserErrorException("The converter doesn't support combining The Fallen Eagle with When the World Stopped Making Sense!");
		}
		if (RajasOfAsiaEnabled && AsiaExpansionProjectEnabled) {
			throw new UserErrorException("The converter doesn't support combining Rajas of Asia with Asia Expansion Project!");
		}
		if (FallenEagleEnabled && RajasOfAsiaEnabled) {
			throw new UserErrorException("The converter doesn't support combining The Fallen Eagle with Rajas of Asia!");
		}
		if (FallenEagleEnabled && AsiaExpansionProjectEnabled) {
			throw new UserErrorException("The converter doesn't support combining The Fallen Eagle with Asia Expansion Project!");
		}
	}

	/// <summary>
	/// Returns a collection of liquid template variables including CK3 mod flags and converter options.
	/// </summary>
	public Hash GetLiquidVariables() {
		var variables = new OrderedDictionary<string, object>();
		foreach (var modFlag in GetCK3ModFlags()) {
			variables[modFlag.Key] = modFlag.Value;
		}
		foreach (var option in GetConverterOptions()) {
			variables[option.Key] = option.Value;
		}

		return Hash.FromDictionary(variables);
	}

	/// <summary>Returns a collection of CK3 mod flags with values based on the enabled mods. "vanilla" flag is set to true if no other flags are set.</summary>
	internal OrderedDictionary<string, bool> GetCK3ModFlags() {
		var flags = new OrderedDictionary<string, bool> {
			["tfe"] = FallenEagleEnabled,
			["wtwsms"] = WhenTheWorldStoppedMakingSenseEnabled,
			["roa"] = RajasOfAsiaEnabled,
			["aep"] = AsiaExpansionProjectEnabled,
		};

		flags["vanilla"] = !flags.Any(f => f.Value);
		return flags;
	}

	internal IEnumerable<string> GetActiveCK3ModFlags() {
		return GetCK3ModFlags().Where(f => f.Value).Select(f => f.Key);
	}

	/// <summary>Returns a collection of converter frontend options with their selected choice values in the format "optionName:choiceValue".</summary>
	private OrderedDictionary<string, bool> GetConverterOptions() { // TODO: rework this to return the original values from configuration.txt instead of converting them to bool
		var options = new OrderedDictionary<string, bool> {
			// Boolean options - choice 0 is false, choice 1 is true
			["HeresiesInHistoricalAreas:0"] = !HeresiesInHistoricalAreas,
			["HeresiesInHistoricalAreas:1"] = HeresiesInHistoricalAreas,
			
			// StaticDeJure - choice 1 is dynamic (false), choice 2 is static (true)
			["StaticDeJure:1"] = !StaticDeJure,
			["StaticDeJure:2"] = StaticDeJure,
			
			// FillerDukes - choice 0 is count (false), choice 1 is duke (true)
			["FillerDukes:0"] = !FillerDukes,
			["FillerDukes:1"] = FillerDukes,
			
			// UseCK3Flags - choice 0 is false, choice 1 is true
			["UseCK3Flags:0"] = !UseCK3Flags,
			["UseCK3Flags:1"] = UseCK3Flags,
			
			// LegionConversion - enum values
			["LegionConversion:No"] = LegionConversion == LegionConversion.No,
			["LegionConversion:SpecialTroops"] = LegionConversion == LegionConversion.SpecialTroops,
			["LegionConversion:MenAtArms"] = LegionConversion == LegionConversion.MenAtArms,
			
			// SkipDynamicCoAExtraction - choice 0 is false, choice 1 is true
			["SkipDynamicCoAExtraction:0"] = !SkipDynamicCoAExtraction,
			["SkipDynamicCoAExtraction:1"] = SkipDynamicCoAExtraction,
			
			// SkipHoldingOwnersImport - choice 0 is false, choice 1 is true
			["SkipHoldingOwnersImport:0"] = !SkipHoldingOwnersImport,
			["SkipHoldingOwnersImport:1"] = SkipHoldingOwnersImport,
		};
		
		return options;
	}
}