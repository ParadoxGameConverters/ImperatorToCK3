using commonItems;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ImperatorToCK3 {
	public enum IMPERATOR_DE_JURE { REGIONS = 1, COUNTRIES = 2, NO = 3 }
	public class Configuration {
		public string SaveGamePath { get; set; } = "";
		public string ImperatorPath { get; set; } = "";
		public string ImperatorDocPath { get; set; } = "";
		public string CK3Path { get; set; } = "";
		public string CK3ModsPath { get; set; } = "";
		public string OutputModName { get; set; } = "";
		public IMPERATOR_DE_JURE ImperatorDeJure { get; set; } = IMPERATOR_DE_JURE.NO;
		public bool HeresiesInHistoricalAreas { get; set; } = false;
		public Date CK3BookmarkDate { get; set; } = new(0, 1, 1);

		public Configuration() { }
		public Configuration(ConverterVersion converterVersion) {
			Logger.Info("Reading configuration file...");
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseFile("configuration.txt");

			SetOutputName();
			VerifyImperatorPath();
			VerifyImperatorVersion(converterVersion);
			VerifyCK3Path();
			VerifyCK3Version(converterVersion);
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
			parser.RegisterKeyword("output_name", reader => {
				OutputModName = reader.GetString();
				Logger.Info($"Output name set to: {OutputModName}");
			});
			parser.RegisterKeyword("ImperatorDeJure", reader => {
				var valueString = reader.GetString();
				try {
					ImperatorDeJure = (IMPERATOR_DE_JURE)Convert.ToInt32(valueString);
					Logger.Info($"ImperatorDeJure set to: {valueString}");
				} catch (Exception e) {
					Logger.Error($"Undefined error, ImperatorDeJure value was: {valueString}; Error message: {e}");
				}
			});
			parser.RegisterKeyword("HeresiesInHistoricalAreas", reader => {
				var valueString = reader.GetString();
				try {
					HeresiesInHistoricalAreas = Convert.ToInt32(valueString) == 1;
					Logger.Info($"HeresiesInHistoricalAreas set to: {HeresiesInHistoricalAreas}");
				} catch (Exception e) {
					Logger.Error($"Undefined error, HeresiesInHistoricalAreas value was: {valueString}; Error message: {e}");
				}
			});

			parser.RegisterKeyword("bookmark_date", reader => {
				var dateStr = reader.GetString();
				if (string.IsNullOrEmpty(dateStr)) {
					return;
				}

				Logger.Info($"Entered CK3 bookmark date: {dateStr}");
				CK3BookmarkDate = new Date(dateStr);
				Logger.Info($"CK3 bookmark date set to: {CK3BookmarkDate}");
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		private void VerifyImperatorPath() {
			if (!Directory.Exists(ImperatorPath)) {
				throw new DirectoryNotFoundException($"{ImperatorPath} does not exist!");
			}

			var imperatorExePath = Path.Combine(ImperatorPath, "binaries", "imperator");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				imperatorExePath += ".exe";
			}
			if (!File.Exists(imperatorExePath)) {
				throw new FileNotFoundException($"{ImperatorPath} does not contains Imperator: Rome!");
			}
			Logger.Info($"\tI:R install path is {ImperatorPath}");
		}

		private void VerifyCK3Path() {
			if (!Directory.Exists(CK3Path)) {
				throw new DirectoryNotFoundException($"{CK3Path} does not exist!");
			}
			
			var ck3ExePath = Path.Combine(CK3Path, "binaries", "ck3");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				ck3ExePath += ".exe";
			}
			if (!File.Exists(ck3ExePath)) {
				throw new FileNotFoundException($"{CK3Path} does not contain Crusader Kings III!");
			}
			Logger.Info($"\tCK3 install path is {CK3Path}");
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
}
