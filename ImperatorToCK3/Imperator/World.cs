using commonItems;
using ImperatorToCK3.Imperator.Genes;
using ImperatorToCK3.Imperator.Pops;
using ImperatorToCK3.Imperator.Provinces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mods = System.Collections.Generic.List<commonItems.Mod>;

namespace ImperatorToCK3.Imperator {
	public class World : Parser {
		private readonly Date startDate = new("450.10.1", AUC: true);
		public Date EndDate { get; private set; } = new Date("727.2.17", AUC: true);
		private GameVersion imperatorVersion = new();
		public Mods Mods { get; private set; } = new();
		private readonly SortedSet<string> dlcs = new();
		public Families.Families Families { get; private set; } = new();
		public Characters.Characters Characters { get; private set; } = new();
		private Pops.Pops pops = new();
		public Provinces.Provinces Provinces { get; private set; } = new();
		public Countries.Countries Countries { get; private set; } = new();
		public Jobs.Jobs Jobs { get; private set; } = new();
		private GenesDB genesDB = new();

		private enum SaveType { INVALID, PLAINTEXT, COMPRESSED_ENCODED }
		private SaveType saveType = SaveType.INVALID;

		public World(Configuration configuration, ConverterVersion converterVersion) {
			Logger.Info("*** Hello Imperator, Roma Invicta! ***");
			ParseGenes(configuration);

			// parse the save
			RegisterRegex(@"\bSAV\w*\b", _ => { });
			RegisterKeyword("version", reader => {
				var versionString = ParserHelpers.GetString(reader);
				imperatorVersion = new GameVersion(versionString);
				Logger.Info($"Save game version: {versionString}");

				if (converterVersion.MinSource > imperatorVersion) {
					Logger.Error(
						$"Converter requires a minimum save from v{converterVersion.MinSource.ToShortString()}");
					throw new FormatException("Save game vs converter version mismatch!");
				}
				if (!converterVersion.MaxSource.IsLargerishThan(imperatorVersion)) {
					Logger.Error(
						$"Converter requires a maximum save from v{converterVersion.MaxSource.ToShortString()}");
					throw new FormatException("Save game vs converter version mismatch!");
				}
			});
			RegisterKeyword("date", reader => {
				var dateString = ParserHelpers.GetString(reader);
				EndDate = new Date(dateString, AUC: true);  // converted to AD
				Logger.Info($"Date: {dateString} AUC ({EndDate} AD)");
				if (EndDate > configuration.Ck3BookmarkDate) {
					Logger.Error("Save date is later than CK3 bookmark date, proceeding at your own risk!");
				}
			});
			RegisterKeyword("enabled_dlcs", reader => {
				var theDLCs = ParserHelpers.GetStrings(reader);
				dlcs.UnionWith(theDLCs);
				foreach (var dlc in dlcs) {
					Logger.Info($"Enabled DLC: {dlc}");
				}
			});
			RegisterKeyword("enabled_mods", reader => {
				Logger.Info("Detecting used mods...");
				var modsList = ParserHelpers.GetStrings(reader);
				Logger.Info($"Save game claims {modsList.Count} mods used:");
				Mods incomingMods = new();
				foreach (var modPath in modsList) {
					Logger.Info($"Used mod: {modPath}");
					incomingMods.Add(new Mod("", modPath));
				}

				// Let's locate, verify and potentially update those mods immediately.
				ModLoader modLoader = new();
				modLoader.LoadMods(configuration.ImperatorDocPath, incomingMods);
				Mods = modLoader.UsableMods;
			});
			RegisterKeyword("family", reader => {
				Logger.Info("Loading Families...");
				Families = Imperator.Families.Families.ParseBloc(reader);
				Logger.Info($"Loaded {Families.StoredFamilies.Count} families.");
			});
			RegisterKeyword("character", reader => {
				Logger.Info("Loading Characters...");
				Characters = Imperator.Characters.Characters.ParseBloc(reader, genesDB);
				Logger.Info($"Loaded {Characters.StoredCharacters.Count} characters.");
			});
			RegisterKeyword("provinces", reader => {
				Logger.Info("Loading Provinces...");
				Provinces = new Provinces.Provinces(reader);
				Logger.Debug($"Ignored Province tokens: {string.Join(", ", Province.IgnoredTokens)}");
				Logger.Info($"Loaded {Provinces.StoredProvinces.Count} provinces.");
			});
			RegisterKeyword("country", reader => {
				Logger.Info("Loading Countries...");
				Countries = Imperator.Countries.Countries.ParseBloc(reader);
				Logger.Info($"Loaded {Countries.StoredCountries.Count} countries.");
			});
			RegisterKeyword("population", reader => {
				Logger.Info("Loading Pops...");
				pops = new PopsBloc(reader).PopsFromBloc;
				Logger.Info($"Loaded {pops.StoredPops.Count} pops.");
			});
			RegisterKeyword("jobs", reader => {
				Logger.Info("Loading Jobs...");
				Jobs = new Jobs.Jobs(reader);
				Logger.Info($"Loaded {Jobs.Governorships.Capacity} governorships.");
			});
			RegisterKeyword("played_country", reader => {
				var playerCountriesToLog = new List<string>();
				var playedCountryBlocParser = new Parser();
				playedCountryBlocParser.RegisterKeyword("country", reader => {
					var countryId = ParserHelpers.GetULong(reader);
					var country = Countries.StoredCountries[countryId];
					country.PlayerCountry = true;
					playerCountriesToLog.Add(country.Tag);
				});
				playedCountryBlocParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
				playedCountryBlocParser.ParseStream(reader);
				Logger.Info($"Player countries: {string.Join(", ", playerCountriesToLog)}");
			});
			RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				ignoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});

			Logger.Info("Verifying Imperator save...");
			VerifySave(configuration.SaveGamePath);

			ParseStream(ProcessSave(configuration.SaveGamePath));
			ClearRegisteredRules();
			Logger.Debug($"Ignored World tokens: {string.Join(", ", ignoredTokens)}");

			Logger.Info("*** Building World ***");

			// Link all the intertwining references
			Logger.Info("Linking Characters with Families");
			Characters.LinkFamilies(Families);
			Families.RemoveUnlinkedMembers();
			Logger.Info("Linking Characters with Countries");
			Characters.LinkCountries(Countries);
			Logger.Info("Linking Provinces with Pops");
			Provinces.LinkPops(pops);
			Logger.Info("Linking Provinces with Countries");
			Provinces.LinkCountries(Countries);
			Logger.Info("Linking Countries with Families");
			Countries.LinkFamilies(Families);

			LoadPreImperatorRulers();

			Logger.Info("*** Good-bye Imperator, rest in peace. ***");
		}
		private void ParseGenes(Configuration config) {
			genesDB = new GenesDB(Path.Combine(config.ImperatorPath, "game/common/genes/00_genes.txt"));
		}
		private void LoadPreImperatorRulers() {
			const string filePath = "configurables/prehistory.txt";
			const string noRulerWarning = "Pre-Imperator ruler term has no pre-Imperator ruler!";
			const string noCountryIdWarning = "Pre-Imperator ruler term has no country ID!";

			var preImperatorRulerTerms = new Dictionary<ulong, List<Countries.RulerTerm>>(); // <country id, list of terms>
			var parser = new Parser();
			parser.RegisterKeyword("ruler", reader => {
				var rulerTerm = new Countries.RulerTerm(reader, Countries);
				if (rulerTerm.PreImperatorRuler is null) {
					Logger.Warn(noRulerWarning);
					return;
				}
				if (rulerTerm.PreImperatorRuler.Country is null) {
					Logger.Warn(noCountryIdWarning);
					return;
				}
				var countryId = rulerTerm.PreImperatorRuler.Country.Id;
				Countries.StoredCountries[countryId].RulerTerms.Add(rulerTerm);
				if (preImperatorRulerTerms.TryGetValue(countryId, out var list)) {
					list.Add(rulerTerm);
				} else {
					preImperatorRulerTerms[countryId] = new() { rulerTerm };
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseFile(filePath);

			foreach (var country in Countries.StoredCountries.Values) {
				country.RulerTerms = country.RulerTerms.OrderBy(t => t.StartDate).ToList();
			}

			// verify with data from historical_regnal_numbers
			var regnalNameCounts = new Dictionary<ulong, Dictionary<string, int>>(); // <country id, <name, count>>
			foreach (var countryId in Countries.StoredCountries.Keys) {
				if (!preImperatorRulerTerms.ContainsKey(countryId)) {
					continue;
				}

				regnalNameCounts.Add(countryId, new());
				var countryRulerTerms = regnalNameCounts[countryId];

				foreach (var term in preImperatorRulerTerms[countryId]) {
					if (term.PreImperatorRuler is null) {
						Logger.Warn(noRulerWarning);
						continue;
					}
					var name = term.PreImperatorRuler.Name;
					if (name is null) {
						Logger.Warn("Pre-Imperator ruler has no country name!");
						continue;
					}
					if (countryRulerTerms.ContainsKey(name)) {
						++countryRulerTerms[name];
					} else {
						countryRulerTerms[name] = 1;
					}
				}
			}
			foreach (var country in Countries.StoredCountries.Values) {
				bool equal;
				if (!regnalNameCounts.ContainsKey(country.Id)) {
					equal = country.HistoricalRegnalNumbers.Count == 0;
				} else {
					equal = country.HistoricalRegnalNumbers.OrderBy(kvp => kvp.Key)
						.SequenceEqual(regnalNameCounts[country.Id].OrderBy(kvp => kvp.Key)
					);
				}

				if (!equal) {
					Logger.Debug($"List of pre-Imperator rulers of {country.Tag} doesn't match data from save!");
				}
			}
		}

		private BufferedReader ProcessSave(string saveGamePath) {
			switch (saveType) {
				case SaveType.PLAINTEXT:
					Logger.Info("Importing debug_mode Imperator save.");
					return ProcessDebugModeSave(saveGamePath);
				case SaveType.COMPRESSED_ENCODED:
					Logger.Info("Importing regular Imperator save.");
					return ProcessCompressedEncodedSave(saveGamePath);
				default:
					throw new InvalidDataException("Unknown save type.");
			}
		}
		private void VerifySave(string saveGamePath) {
			using var saveStream = File.Open(saveGamePath, FileMode.Open);
			var buffer = new byte[10];
			saveStream.Read(buffer, 0, 4);
			if (buffer[0] != 'S' || buffer[1] != 'A' || buffer[2] != 'V') {
				throw new InvalidDataException("Save game of unknown type!");
			}

			char ch;
			do { // skip until newline
				ch = (char)saveStream.ReadByte();
			} while (ch != '\n' && ch != '\r');

			var length = saveStream.Length;
			if (length < 65536) {
				throw new InvalidDataException("Save game seems a bit too small.");
			}

			saveStream.Position = 0;
			var bigBuf = new byte[65536];
			var bytesReadCount = saveStream.Read(bigBuf);
			if (bytesReadCount < 65536) {
				throw new InvalidDataException($"Read only {bytesReadCount}bytes.");
			}
			saveType = SaveType.PLAINTEXT;
			for (var i = 0; i < 65533; ++i) {
				if (BitConverter.ToUInt32(bigBuf, i) == 0x04034B50 && BitConverter.ToUInt16(bigBuf, i - 2) == 4) {
					saveType = SaveType.COMPRESSED_ENCODED;
				}
			}
		}
		private static BufferedReader ProcessDebugModeSave(string saveGamePath) {
			return new BufferedReader(File.Open(saveGamePath, FileMode.Open));
		}
		private static BufferedReader ProcessCompressedEncodedSave(string saveGamePath) {
			var saveText = Helpers.RakalyCaller.ToPlainText(saveGamePath);
			return new BufferedReader(saveText);
		}

		private readonly HashSet<string> ignoredTokens = new();
	}
}
