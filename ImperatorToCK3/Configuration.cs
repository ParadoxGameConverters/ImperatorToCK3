using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Exceptions;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ImperatorToCK3;

public enum LegionConversion { No, SpecialTroops, MenAtArms }
public class Configuration {
	public string SaveGamePath { get; set; } = "";
	public string ImperatorPath { get; set; } = "";
	public string ImperatorDocPath { get; set; } = "";
	public string CK3Path { get; set; } = "";
	public string CK3ModsPath { get; set; } = "";
	public OrderedSet<string> SelectedCK3Mods { get; } = new();
	public string OutputModName { get; set; } = "";
	public bool HeresiesInHistoricalAreas { get; set; } = false;
	public bool StaticDeJure { get; set; } = false;
	public bool RiseOfIslam { get; set; } = true;
	public double ImperatorCurrencyRate { get; set; } = 1.0d;
	public double ImperatorCivilizationWorth { get; set; } = 0.4;
	public LegionConversion LegionConversion { get; set; } = LegionConversion.MenAtArms;
	public Date CK3BookmarkDate { get; set; } = new(0, 1, 1);

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
				HeresiesInHistoricalAreas = Convert.ToInt32(valueString) == 1;
				Logger.Info($"{nameof(HeresiesInHistoricalAreas)} set to: {HeresiesInHistoricalAreas}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(HeresiesInHistoricalAreas)} value was: {valueString}; Error message: {e}");
			}
		});
		parser.RegisterKeyword("StaticDeJure", reader => {
			var valueString = reader.GetString();
			try {
				StaticDeJure = Convert.ToInt32(valueString) == 2;
				Logger.Info($"{nameof(StaticDeJure)} set to: {StaticDeJure}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(StaticDeJure)} value was: {valueString}; Error message: {e}");
			}
		});
		parser.RegisterKeyword("RiseOfIslam", reader => {
			var valueString = reader.GetString();
			try {
				RiseOfIslam = Convert.ToInt32(valueString) == 2;
				Logger.Info($"{nameof(RiseOfIslam)} set to: {RiseOfIslam}");
			} catch (Exception e) {
				Logger.Error($"Undefined error, {nameof(RiseOfIslam)} value was: {valueString}; Error message: {e}");
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
				throw new ConverterException($"CK3 bookmark date must be {earliestAllowedDate} AD or later. Fix your configuration.");
			}
			Logger.Info($"CK3 bookmark date set to: {CK3BookmarkDate}");
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	private void VerifyImperatorPath() {
		if (!Directory.Exists(ImperatorPath)) {
			throw new DirectoryNotFoundException($"{ImperatorPath} does not exist!");
		}

		var binariesPath = Path.Combine(ImperatorPath, "binaries");
		var imperatorExePath = Path.Combine(binariesPath, "imperator");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
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
			throw new FileNotFoundException($"{ImperatorPath} does not contain Imperator: Rome!");
		}
	}

	private void VerifyCK3Path() {
		if (!Directory.Exists(CK3Path)) {
			throw new DirectoryNotFoundException($"{CK3Path} does not exist!");
		}

		var binariesPath = Path.Combine(CK3Path, "binaries");
		var ck3ExePath = Path.Combine(binariesPath, "ck3");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
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
			throw new FileNotFoundException($"{CK3Path} does not contain Crusader Kings III!");
		}
	}

	private void SetOutputName() {
		if (OutputModName.Length == 0) {
			OutputModName = CommonFunctions.TrimPath(SaveGamePath);
		}
		OutputModName = CommonFunctions.TrimExtension(OutputModName);
		OutputModName = OutputModName.Replace('-', '_');
		OutputModName = OutputModName.Replace(' ', '_');

		OutputModName = CommonFunctions.NormalizeUTF8Path(OutputModName);
		Logger.Info($"Using output name {OutputModName}");
	}

	private void VerifyImperatorVersion(ConverterVersion converterVersion) {
		var path = Path.Combine(ImperatorPath, "launcher/launcher-settings.json");
		var impVersion = GameVersion.ExtractVersionFromLauncher(path);
		if (impVersion is null) {
			Logger.Error("Imperator version could not be determined, proceeding blind!");
			return;
		}

		Logger.Info($"Imperator version: {impVersion.ToShortString()}");

		if (converterVersion.MinSource > impVersion) {
			Logger.Error($"Imperator version is v{impVersion.ToShortString()}, converter requires minimum v{converterVersion.MinSource.ToShortString()}!");
			throw new ArgumentOutOfRangeException(nameof(impVersion), "Converter vs Imperator installation mismatch!");
		}
		if (!converterVersion.MaxSource.IsLargerishThan(impVersion)) {
			Logger.Error($"Imperator version is v{impVersion.ToShortString()}, converter requires maximum v{converterVersion.MaxSource.ToShortString()}!");
			throw new ArgumentOutOfRangeException(nameof(impVersion), "Converter vs Imperator installation mismatch!");
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
			throw new ArgumentOutOfRangeException(nameof(ck3Version), "Converter vs CK3 installation mismatch!");
		}
		if (!converterVersion.MaxTarget.IsLargerishThan(ck3Version)) {
			Logger.Error($"CK3 version is v{ck3Version.ToShortString()}, converter requires maximum v{converterVersion.MaxTarget.ToShortString()}!");
			throw new ArgumentOutOfRangeException(nameof(ck3Version), "Converter vs CK3 installation mismatch!");
		}
	}
}