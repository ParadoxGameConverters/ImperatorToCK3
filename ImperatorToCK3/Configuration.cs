using System.IO;
using System;
using commonItems;

namespace ImperatorToCK3 {
	public enum IMPERATOR_DE_JURE { REGIONS = 1, COUNTRIES = 2, NO = 3 };
	public class Configuration : Parser {
		public string SaveGamePath { get; internal set; } = "";
		public string ImperatorPath { get; internal set; } = "";
		public string ImperatorDocPath { get; internal set; } = "";
		public string Ck3Path { get; internal set; } = "";
		public string Ck3ModsPath { get; internal set; } = "";
		public string OutputModName { get; internal set; } = "";
		public IMPERATOR_DE_JURE ImperatorDeJure { get; internal set; } = IMPERATOR_DE_JURE.NO;
		public bool ConvertBirthAndDeathDates { get; internal set; } = true;
		public Date Ck3BookmarkDate { get; private set; } = new(867, 1, 1);

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
			RegisterKeyword("SaveGame", (sr) => {
				SaveGamePath = new SingleString(sr).String;
				Logger.Info("Save game set to: " + SaveGamePath);
			});
			RegisterKeyword("ImperatorDirectory", (sr) => {
				ImperatorPath = new SingleString(sr).String;
			});
			RegisterKeyword("ImperatorDocDirectory", (sr) => {
				ImperatorDocPath = new SingleString(sr).String;
			});
			RegisterKeyword("CK3directory", (sr) => {
				Ck3Path = new SingleString(sr).String;
			});
			RegisterKeyword("CK3ModsDirectory", (sr) => {
				Ck3ModsPath = new SingleString(sr).String;
			});
			RegisterKeyword("output_name", (sr) => {
				OutputModName = new SingleString(sr).String;
				Logger.Info("Output name set to: " + OutputModName);
			});
			RegisterKeyword("ImperatorDeJure", (sr) => {
				var deJureString = new SingleString(sr).String;
				try {
					ImperatorDeJure = (IMPERATOR_DE_JURE)Convert.ToInt32(deJureString);
					Logger.Info("ImperatorDeJure set to: " + deJureString);
				} catch (Exception e) {
					Logger.Error("Undefined error, ImperatorDeJure value was: " + deJureString + "; Error message: " + e.ToString());
				}
			});
			RegisterKeyword("ConvertCharacterBirthAndDeathDates", (sr) => {
				var valStr = new SingleString(sr).String;
				ConvertBirthAndDeathDates = valStr == "true";
				Logger.Info("Conversion of characters' birth and death dates set to: " + ConvertBirthAndDeathDates);
			});

			RegisterKeyword("bookmark_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
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
			var ImpVersion = GameVersion.ExtractVersionFromLauncher(path);
			if (ImpVersion is null) {
				Logger.Error("Imperator version could not be determined, proceeding blind!");
				return;
			}

			Logger.Info($"Imperator version: {ImpVersion.ToShortString()}");

			if (converterVersion.MinSource > ImpVersion) {
				Logger.Error($"Imperator version is v{ImpVersion.ToShortString()}," +
					$" converter requires minimum v{converterVersion.MinSource.ToShortString()}!");
				throw new ArgumentOutOfRangeException(nameof(ImpVersion), "Converter vs Imperator installation mismatch!");
			}
			if (!converterVersion.MaxSource.IsLargerishThan(ImpVersion)) {
				Logger.Error($"Imperator version is v{ImpVersion.ToShortString()}, converter requires maximum v" +
						 $"{converterVersion.MaxSource.ToShortString()}!");
				throw new ArgumentOutOfRangeException(nameof(ImpVersion), "Converter vs Imperator installation mismatch!");
			}
		}

		private void VerifyCK3Version(ConverterVersion converterVersion) {
			var path = Path.Combine(Ck3Path, "launcher/launcher-settings.json");
			var CK3Version = GameVersion.ExtractVersionFromLauncher(path);
			if (CK3Version is null) {
				Logger.Error("CK3 version could not be determined, proceeding blind!");
				return;
			}

			Logger.Info($"CK3 version: {CK3Version.ToShortString()}");

			if (converterVersion.MinTarget > CK3Version) {
				Logger.Error($"CK3 version is v{CK3Version.ToShortString()}, converter requires minimum v" +
						 $"{converterVersion.MinTarget.ToShortString()}!");
				throw new ArgumentOutOfRangeException(nameof(CK3Version), "Converter vs CK3 installation mismatch!");
			}
			if (!converterVersion.MaxTarget.IsLargerishThan(CK3Version)) {
				Logger.Error($"CK3 version is v{CK3Version.ToShortString()}, converter requires maximum v" +
						 $"{converterVersion.MaxTarget.ToShortString()} !");
				throw new ArgumentOutOfRangeException(nameof(CK3Version), "Converter vs CK3 installation mismatch!");
			}
		}
	}
}
