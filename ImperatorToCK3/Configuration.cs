using commonItems;
using System;
using System.IO;

namespace ImperatorToCK3 {
	public enum IMPERATOR_DE_JURE { REGIONS = 1, COUNTRIES = 2, NO = 3 }
	public class Configuration : Parser {
		public string SaveGamePath { get; internal set; } = "";
		public string ImperatorPath { get; internal set; } = "";
		public string ImperatorDocPath { get; internal set; } = "";
		public string Ck3Path { get; internal set; } = "";
		public string Ck3ModsPath { get; internal set; } = "";
		public string OutputModName { get; internal set; } = "";
		public IMPERATOR_DE_JURE ImperatorDeJure { get; internal set; } = IMPERATOR_DE_JURE.NO;
		public Date Ck3BookmarkDate { get; private set; } = new(867, 1, 1);

		public Configuration() { }
		public Configuration(ConverterVersion converterVersion) {
			Logger.Info("Reading configuration file");
			RegisterKeys();
			ParseFile("configuration.txt");
			ClearRegisteredRules();
			SetOutputName();
			VerifyImperatorPath();
			VerifyImperatorVersion(converterVersion);
			VerifyCK3Path();
			VerifyCK3Version(converterVersion);
		}

		private void RegisterKeys() {
			RegisterKeyword("SaveGame", reader => {
				SaveGamePath = reader.GetString();
				Logger.Info("Save game set to: " + SaveGamePath);
			});
			RegisterKeyword("ImperatorDirectory", reader => ImperatorPath = reader.GetString());
			RegisterKeyword("ImperatorDocDirectory", reader => ImperatorDocPath = reader.GetString());
			RegisterKeyword("CK3directory", reader => Ck3Path = reader.GetString());
			RegisterKeyword("CK3ModsDirectory", reader => Ck3ModsPath = reader.GetString());
			RegisterKeyword("output_name", reader => {
				OutputModName = reader.GetString();
				Logger.Info($"Output name set to: {OutputModName}");
			});
			RegisterKeyword("ImperatorDeJure", reader => {
				var deJureString = reader.GetString();
				try {
					ImperatorDeJure = (IMPERATOR_DE_JURE)Convert.ToInt32(deJureString);
					Logger.Info($"ImperatorDeJure set to: {deJureString}");
				} catch (Exception e) {
					Logger.Error($"Undefined error, ImperatorDeJure value was: {deJureString}; Error message: {e}");
				}
			});

			RegisterKeyword("bookmark_date", reader => {
				var dateStr = reader.GetString();
				Logger.Info($"Entered CK3 bookmark date: {dateStr}");
				Ck3BookmarkDate = new Date(dateStr);
				Logger.Info($"CK3 bookmark date set to: {Ck3BookmarkDate}");
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}

		private void VerifyImperatorPath() {
			if (!Directory.Exists(ImperatorPath)) {
				throw new DirectoryNotFoundException(ImperatorPath + " does not exist!");
			}
			if (!File.Exists(ImperatorPath + "/binaries/imperator.exe")) {
				throw new FileNotFoundException(ImperatorPath + "does not contains Imperator: Rome!");
			}
			Logger.Info("\tI:R install path is " + ImperatorPath);
		}

		private void VerifyCK3Path() {
			if (!Directory.Exists(Ck3Path)) {
				throw new DirectoryNotFoundException(Ck3Path + " does not exist!");
			}
			if (!File.Exists(Ck3Path + "/binaries/ck3.exe")) {
				throw new FileNotFoundException(Ck3Path + " does not contain Crusader Kings III!");
			}
			Logger.Info("\tCK3 install path is " + Ck3Path);
		}

		private void SetOutputName() {
			if (OutputModName.Length == 0) {
				OutputModName = CommonFunctions.TrimPath(SaveGamePath);
			}
			OutputModName = CommonFunctions.TrimExtension(OutputModName);
			OutputModName = OutputModName.Replace('-', '_');
			OutputModName = OutputModName.Replace(' ', '_');

			OutputModName = CommonFunctions.NormalizeUTF8Path(OutputModName);
			Logger.Info("Using output name " + OutputModName);
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
				Logger.Error($"Imperator version is v{impVersion.ToShortString()}," +
					$" converter requires minimum v{converterVersion.MinSource.ToShortString()}!");
				throw new ArgumentOutOfRangeException(nameof(impVersion), "Converter vs Imperator installation mismatch!");
			}
			if (!converterVersion.MaxSource.IsLargerishThan(impVersion)) {
				Logger.Error($"Imperator version is v{impVersion.ToShortString()}, converter requires maximum v" +
						 $"{converterVersion.MaxSource.ToShortString()}!");
				throw new ArgumentOutOfRangeException(nameof(impVersion), "Converter vs Imperator installation mismatch!");
			}
		}

		private void VerifyCK3Version(ConverterVersion converterVersion) {
			var path = Path.Combine(Ck3Path, "launcher/launcher-settings.json");
			var ck3Version = GameVersion.ExtractVersionFromLauncher(path);
			if (ck3Version is null) {
				Logger.Error("CK3 version could not be determined, proceeding blind!");
				return;
			}

			Logger.Info($"CK3 version: {ck3Version.ToShortString()}");

			if (converterVersion.MinTarget > ck3Version) {
				Logger.Error($"CK3 version is v{ck3Version.ToShortString()}, converter requires minimum v" +
						 $"{converterVersion.MinTarget.ToShortString()}!");
				throw new ArgumentOutOfRangeException(nameof(ck3Version), "Converter vs CK3 installation mismatch!");
			}
			if (!converterVersion.MaxTarget.IsLargerishThan(ck3Version)) {
				Logger.Error($"CK3 version is v{ck3Version.ToShortString()}, converter requires maximum v" +
						 $"{converterVersion.MaxTarget.ToShortString()} !");
				throw new ArgumentOutOfRangeException(nameof(ck3Version), "Converter vs CK3 installation mismatch!");
			}
		}
	}
}
