using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Armies;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Inventions;
using ImperatorToCK3.Imperator.Pops;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Imperator.Religions;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Region;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mods = System.Collections.Generic.List<commonItems.Mods.Mod>;
using Parser = commonItems.Parser;

namespace ImperatorToCK3.Imperator;

internal partial class World {
	public Date EndDate { get; private set; } = new Date("727.2.17", AUC: true);
	private readonly IList<string> incomingModPaths = []; // List of all mods used in the save.
	public ModFilesystem ModFS { get; private set; }
	private readonly SortedSet<string> dlcs = [];
	public IReadOnlySet<string> GlobalFlags { get; private set; } = ImmutableHashSet<string>.Empty;
	private readonly ScriptValueCollection scriptValues = new();
	public ImperatorDefines Defines { get; } = new();
	public LocDB LocDB { get; } = new(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);

	public NamedColorCollection NamedColors { get; } = [];
	public FamilyCollection Families { get; } = [];
	public CharacterCollection Characters { get; } = [];
	private readonly PopCollection pops = [];
	public ProvinceCollection Provinces { get; } = [];
	public CountryCollection Countries { get; } = [];
	internal CoaMapper CoaMapper { get; private set; } = new();
	public MapData MapData { get; private set; }
	public AreaCollection Areas { get; } = [];
	public ImperatorRegionMapper ImperatorRegionMapper { get; private set; }
	public StateCollection States { get; } = [];

	private DiplomacyDB diplomacyDB;
	public IReadOnlyCollection<War> Wars => diplomacyDB.Wars;
	public IReadOnlyCollection<Dependency> Dependencies => diplomacyDB.Dependencies;
	public IReadOnlyCollection<List<ulong>> DefensiveLeagues => diplomacyDB.DefensiveLeagues; // TODO: convert this to CK3
	
	public Jobs.JobsDB JobsDB { get; private set; } = new();
	internal UnitCollection Units { get; } = [];
	public CulturesDB CulturesDB { get; } = [];
	public ReligionCollection Religions { get; private set; }
	private GenesDB genesDB = new();
	public InventionsDB InventionsDB { get; } = new();
	public ColorFactory ColorFactory { get; } = new();
	
	public IReadOnlyList<Mod> UsableMods { get; private set; } = Array.Empty<Mod>();
	
	public bool InvictusDetected { get; private set; }
	public bool TerraIndomitaDetected { get; private set; }

	private enum SaveType { Invalid, Plaintext, CompressedEncoded }
	private SaveType saveType = SaveType.Invalid;
	private string metaPlayerName = string.Empty;

	public ulong RandomSeed { get; private set; }

	protected World(Configuration config) {
		ModFS = new ModFilesystem(Path.Combine(config.ImperatorPath, "game"), Array.Empty<Mod>());
		MapData = new MapData(ModFS);
		
		Religions = new ReligionCollection(new ScriptValueCollection());
		ImperatorRegionMapper = new ImperatorRegionMapper(Areas, MapData);

		diplomacyDB = new();
	}
	
	private static void OutputGuiContainer(ModFilesystem modFS, IEnumerable<string> tagsNeedingFlags, Configuration config) {
		Logger.Debug("Modifying gui for exporting CoAs...");
		
		const string relativeTopBarGuiPath = "gui/ingame_topbar.gui";
		var topBarGuiPath = modFS.GetActualFileLocation(relativeTopBarGuiPath);
		if (topBarGuiPath is null) {
			Logger.Warn($"{relativeTopBarGuiPath} not found, can't write CoA export commands!");
			return;
		}

		var guiTextBuilder = new StringBuilder();
		guiTextBuilder.AppendLine("\tstate = {");
		guiTextBuilder.AppendLine("\t\tname = _show");
		string commandsString = string.Join(';', tagsNeedingFlags.Select(tag => $"coat_of_arms {tag}"));
		commandsString += ";dumpdatatypes"; // This will let us know when the commands finished executing.
		guiTextBuilder.AppendLine($"\t\ton_start=\"[ExecuteConsoleCommandsForced('{commandsString}')]\"");
		guiTextBuilder.AppendLine("\t}");
		
		List<string> lines = [.. File.ReadAllLines(topBarGuiPath)];
		int index = lines.FindIndex(line => line.Contains("name = \"ingame_topbar\""));
		if (index != -1) {
			lines.Insert(index + 1, guiTextBuilder.ToString());
		}

		var topBarOutputPath = Path.Combine(config.ImperatorDocPath, "mod/coa_export_mod", relativeTopBarGuiPath);
		Logger.Debug($"Writing modified GUI to \"{topBarOutputPath}\"...");
		var topBarOutputDir = Path.GetDirectoryName(topBarOutputPath);
		if (topBarOutputDir is not null) {
			Directory.CreateDirectory(topBarOutputDir);
		}
		File.WriteAllLines(topBarOutputPath, lines);
		
		// Create a .mod file for the temporary mod.
		Logger.Debug("Creating temporary mod file...");
		string modFileContents = 
			"""
			name = "IRToCK3 CoA export mod"
			path = "mod/coa_export_mod"
			""";
		File.WriteAllText(Path.Combine(config.ImperatorDocPath, "mod/coa_export_mod/descriptor.mod"), modFileContents);

		var absoluteModPath = Path.Combine(config.ImperatorDocPath, "mod/coa_export_mod").Replace('\\', '/');
		modFileContents = modFileContents.Replace("path = \"mod/coa_export_mod\"", $"path = \"{absoluteModPath}\"");
		File.WriteAllText(Path.Combine(config.ImperatorDocPath, "mod/coa_export_mod.mod"), modFileContents);
	}
	
	private void OutputContinueGameJson(Configuration config) {
		// Set the current save to be used when launching the game with the continuelastsave option.
		Logger.Debug("Modifying continue_game.json...");
		File.WriteAllText(Path.Join(config.ImperatorDocPath, "continue_game.json"),
			contents: $$"""
            {
                "title": "{{Path.GetFileNameWithoutExtension(config.SaveGamePath)}}",
                "desc": "Playing as {{metaPlayerName}} - {{EndDate}} AD",
                "date": "{{DateTime.Now:yyyy-MM-dd HH:mm:ss}}"
            }
            """);
	}

	private void OutputDlcLoadJson(Configuration config) {
		Logger.Debug("Outputting dlc_load.json...");
		var dlcLoadBuilder = new StringBuilder();
		dlcLoadBuilder.AppendLine("{");
		dlcLoadBuilder.Append(@"""enabled_mods"": [");
		dlcLoadBuilder.AppendJoin(", ", incomingModPaths.Select(modPath => $"\"{modPath}\""));
		dlcLoadBuilder.AppendLine(",");
		dlcLoadBuilder.AppendLine("\"mod/coa_export_mod.mod\"");
		dlcLoadBuilder.AppendLine("],");
		dlcLoadBuilder.AppendLine(@"""disabled_dlcs"":[]");
		dlcLoadBuilder.AppendLine("}");
		File.WriteAllText(Path.Join(config.ImperatorDocPath, "dlc_load.json"), dlcLoadBuilder.ToString());
	}

	private void LaunchImperatorToExportCountryFlags(Configuration config) {
		Logger.Info("Retrieving random CoAs from Imperator...");
		OutputContinueGameJson(config);
		OutputDlcLoadJson(config);
		
		string imperatorBinaryName = OperatingSystem.IsWindows() ? "imperator.exe" : "imperator";
		var imperatorBinaryPath = Path.Combine(config.ImperatorPath, "binaries", imperatorBinaryName);
		if (!File.Exists(imperatorBinaryPath)) {
			Logger.Warn("Imperator binary not found! Aborting the random CoA extraction!");
			return;
		}
		
		string dataTypesLogPath = Path.Combine(config.ImperatorDocPath, "logs/data_types.log");
		if (File.Exists(dataTypesLogPath)) {
			FileHelper.DeleteWithRetries(dataTypesLogPath);
		}
		
		Logger.Debug("Launching Imperator to extract coats of arms...");

		var processStartInfo = new ProcessStartInfo {
			FileName = imperatorBinaryPath,
			Arguments = "-continuelastsave -debug_mode",
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			WindowStyle = ProcessWindowStyle.Hidden,
		};
		var imperatorProcess = Process.Start(processStartInfo);
		if (imperatorProcess is null) {
			Logger.Warn("Failed to start Imperator process! Aborting!");
			return;
		}
		
		imperatorProcess.Exited += HandleImperatorProcessExit(config, imperatorProcess);
		
		// Make sure that if converter is closed, Imperator is closed as well.
		AppDomain.CurrentDomain.ProcessExit += (_, _) => {
			if (!imperatorProcess.HasExited) {
				imperatorProcess.Kill();
			}
		};
		
		// Wait until data_types.log exists (it will be created by the dumpdatatypes command).
		var stopwatch = new Stopwatch();
		stopwatch.Start();
		while (!imperatorProcess.HasExited && !File.Exists(dataTypesLogPath)) {
			if (stopwatch.Elapsed > TimeSpan.FromMinutes(5)) {
				Logger.Warn("Imperator process took too long to execute console commands! Aborting!");
				imperatorProcess.Kill();
				break;
			}
			
			if (imperatorProcess.StandardOutput.ReadLine()?.Contains("Updating cached data done") == true) {
				Logger.Debug("Imperator finished loading. Waiting for console commands to execute...");
			}
			
			Thread.Sleep(100);
		}

		if (!imperatorProcess.HasExited) {
			Logger.Debug("Killing Imperator process...");
			imperatorProcess.Kill();
		}
	}

	private static EventHandler HandleImperatorProcessExit(Configuration config, Process imperatorProcess) {
		return (_, _) => {
			Logger.Debug($"Imperator process exited with code {imperatorProcess.ExitCode}. Removing temporary mod files...");
			try {
				FileHelper.DeleteWithRetries(Path.Combine(config.ImperatorDocPath, "mod/coa_export_mod.mod"));
				Directory.Delete(Path.Combine(config.ImperatorDocPath, "mod/coa_export_mod"), recursive: true);
			} catch (Exception e) {
				Logger.Warn($"Failed to remove temporary mod files: {e.Message}");
			}
		};
	}

	private void ReadCoatsOfArmsFromGameLog(string imperatorDocPath) {
		Logger.Info("Reading CoAs from game log...");
		string inputFilePath = Path.Combine(imperatorDocPath, "logs/game.log");
		if (!File.Exists(inputFilePath)) {
			Logger.Warn("Imperator's game.log not found!");
			return;
		}

		using var saveStream = File.Open(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(saveStream);
		string content = reader.ReadToEnd();

		// Remove everything prior to the first line contatining "Coat of arms:"
		int startIndex = content.IndexOf("Coat of arms:", StringComparison.Ordinal);
		if (startIndex == -1) {
			Logger.Warn("No CoAs found in game log.");
			return;
		}
		content = content[startIndex..];
		MatchCollection matches = FlagDefinitionRegex().Matches(content);

		CoaMapper.ParseCoAs(matches.Select(match => match.Value));
		Logger.Info("Finished reading CoAs from I:R game.log.");
	}

	private void ExtractDynamicCoatsOfArms(Configuration config) {
		var countryFlags = Countries.Select(country => country.Flag).ToArray();
		var missingFlags = CoaMapper.GetAllMissingFlagKeys(countryFlags);
		if (missingFlags.Count == 0) {
			return;
		}

		Logger.Debug("Missing country flag definitions: " + string.Join(", ", missingFlags));

		var tagsWithMissingFlags = Countries
			.Where(country => missingFlags.Contains(country.Flag))
			.Select(country => country.Tag);

		OutputGuiContainer(ModFS, tagsWithMissingFlags, config);
		LaunchImperatorToExportCountryFlags(config);
		ReadCoatsOfArmsFromGameLog(config.ImperatorDocPath);

		var missingFlagsAfterExtraction = CoaMapper.GetAllMissingFlagKeys(countryFlags);
		if (missingFlagsAfterExtraction.Count > 0) {
			Logger.Warn("Failed to export the following country flags: " + string.Join(", ", missingFlagsAfterExtraction));
		}
	}

	public World(Configuration config, ConverterVersion converterVersion, out Thread? coaExtractionThread) {
		Logger.Info("*** Hello Imperator, Roma Invicta! ***");

		Logger.Info("Verifying Imperator save...");
		VerifySave(config.SaveGamePath);
		Logger.IncrementProgress();

		ParseSave(config, converterVersion, out coaExtractionThread);
		
		// Throw exceptions if any important data is missing.
		if (ModFS is null) {
			throw new InvalidOperationException($"{nameof(ModFS)} is not initialized!");
		}
		if (MapData is null) {
			throw new InvalidOperationException($"{nameof(MapData)} is not initialized!");
		}
		if (Religions is null) {
			throw new InvalidOperationException($"{nameof(Religions)} is not initialized!");
		}
		if (ImperatorRegionMapper is null) {
			throw new InvalidOperationException($"{nameof(ImperatorRegionMapper)} is not initialized!");
		}

		Logger.Info("*** Building World ***");

		// Link all the intertwining references
		Logger.Info("Linking Characters with Families...");
		Characters.LinkFamilies(Families);
		Families.RemoveUnlinkedMembers(Characters);
		Families.MergeDividedFamilies(Characters);
		Logger.Info("Linking Characters with Countries...");
		Characters.LinkCountries(Countries);
		Logger.Info("Linking Provinces with Pops...");
		Provinces.LinkPops(pops);
		Logger.Info("Linking Countries with Families...");
		Countries.LinkFamilies(Families);

		LoadPreImperatorRulers();
		
		// Detect specific mods.
		InvictusDetected = GlobalFlags.Contains("is_playing_invictus");
		TerraIndomitaDetected = Countries.Any(c => c.Variables.Contains("unification_points")) ||
		                        UsableMods.Any(m => m.Name == "Antiquitas");

		Logger.Info("*** Good-bye Imperator, rest in peace. ***");
	}

	private void ParseSave(Configuration config, ConverterVersion converterVersion, out Thread? coaExtractionThread) {
		var imperatorRoot = Path.Combine(config.ImperatorPath, "game");
		var parser = new Parser();

		Thread? localCoaExtractThread = null;
		
		parser.RegisterRegex(SaveStartRegex(), _ => { });
		parser.RegisterKeyword("version", reader => VerifySaveVersion(converterVersion, reader));
		parser.RegisterKeyword("date", reader => LoadSaveDate(config, reader));
		parser.RegisterKeyword("enabled_dlcs", LogEnabledDLCs);
		parser.RegisterKeyword("enabled_mods", reader => {
			Mods incomingMods = DetectUsedMods(reader);

			// Let's locate, verify and potentially update those mods immediately.
			ModLoader modLoader = new();
			modLoader.LoadMods(config.ImperatorDocPath, incomingMods);
			UsableMods = new Mods(modLoader.UsableMods);
			ModFS = new ModFilesystem(imperatorRoot, modLoader.UsableMods);

			// Now that we have the list of mods used, we can load data from Imperator mod filesystem
			LoadModFilesystemDependentData();
		});
		parser.RegisterKeyword("variables", ReadVariablesFromSave);
		parser.RegisterKeyword("family", LoadFamilies);
		parser.RegisterKeyword("character", LoadCharacters);
		parser.RegisterKeyword("state", LoadStates);
		parser.RegisterKeyword("provinces", LoadProvinces);
		parser.RegisterKeyword("armies", LoadArmies);
		parser.RegisterKeyword("country", reader => {
			LoadCountries(reader);
			
			// Now that the countries are loaded, we can start I:R CoA extraction
			// in parallel to other converter operations.
			if (!config.SkipDynamicCoAExtraction) {
				localCoaExtractThread = new Thread(() => ExtractDynamicCoatsOfArms(config));
				localCoaExtractThread.Start();
			}
		});
		parser.RegisterKeyword("population", LoadPops);
		parser.RegisterKeyword("diplomacy", LoadDiplomacy);
		parser.RegisterKeyword("jobs", LoadJobs);
		parser.RegisterKeyword("deity_manager", reader => Religions.LoadHolySiteDatabase(reader));
		parser.RegisterKeyword("meta_player_name", reader => metaPlayerName = reader.GetString());
		parser.RegisterKeyword("speed", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("random_seed", reader => RandomSeed = reader.GetULong());
		parser.RegisterKeyword("tutorial_disable", ParserHelpers.IgnoreItem);
		var playerCountriesToLog = new OrderedSet<string>();
		parser.RegisterKeyword("played_country", LoadPlayerCountries(playerCountriesToLog));
		parser.IgnoreAndStoreUnregisteredItems(ignoredTokens);

		parser.ParseStream(ProcessSave(config.SaveGamePath));

		Logger.Debug($"Ignored World tokens: {ignoredTokens}");
		Logger.Info($"Player countries: {string.Join(", ", playerCountriesToLog)}");
		Logger.IncrementProgress();

		// The CoA extraction may continue after ParseSave finishes executing.
		coaExtractionThread = localCoaExtractThread;
	}

	private Mods DetectUsedMods(BufferedReader reader) {
		Logger.Info("Detecting used mods...");
		foreach (var modPath in reader.GetStrings()) {
			incomingModPaths.Add(modPath);
		}
		if (incomingModPaths.Count == 0) {
			Logger.Warn("Save game claims no mods used.");
		} else {
			Logger.Info($"Save game claims {incomingModPaths.Count} mods used:");
		}
		Mods incomingMods = [];
		foreach (var modPath in incomingModPaths) {
			Logger.Info($"Used mod: {modPath}");
			incomingMods.Add(new Mod(string.Empty, modPath));
		}
		Logger.IncrementProgress();
		return incomingMods;
	}

	private void LoadFamilies(BufferedReader reader) {
		Logger.Info("Loading families...");
		Families.LoadFamiliesFromBloc(reader);
		Logger.Info($"Loaded {Families.Count} families.");
		Logger.IncrementProgress();
	}

	private void LoadCharacters(BufferedReader reader) {
		Logger.Info("Loading characters...");
		Characters.GenesDB = genesDB;
		Characters.LoadCharactersFromBloc(reader);
		Logger.Info($"Loaded {Characters.Count} characters.");
		Logger.IncrementProgress();
	}

	private void LoadCountries(BufferedReader reader) {
		Logger.Info("Loading countries...");
		Countries.LoadCountriesFromBloc(reader);
		Logger.Info($"Loaded {Countries.Count} countries.");
		Logger.IncrementProgress();
	}

	private void LoadPops(BufferedReader reader) {
		Logger.Info("Loading pops...");
		pops.LoadPopsFromBloc(reader);
		Logger.Info($"Loaded {pops.Count} pops.");
		Logger.IncrementProgress();
	}

	private void LoadJobs(BufferedReader reader) {
		Logger.Info("Loading Jobs...");
		JobsDB = new Jobs.JobsDB(reader, Characters, Countries, ImperatorRegionMapper);
		Logger.Info($"Loaded {JobsDB.Governorships.Count} governorships.");
		Logger.IncrementProgress();
	}

	private void LoadDiplomacy(BufferedReader reader) {
		Logger.Info("Loading diplomacy...");
		diplomacyDB = new DiplomacyDB(reader);
		Logger.IncrementProgress();
	}

	private void LogEnabledDLCs(BufferedReader reader) {
		dlcs.UnionWith(reader.GetStrings());
		foreach (var dlc in dlcs) {
			Logger.Info($"Enabled DLC: {dlc}");
		}

		Logger.IncrementProgress();
	}

	private void LoadArmies(BufferedReader reader) {
		Logger.Info("Loading armies...");
		var armiesParser = new Parser();
		armiesParser.RegisterKeyword("subunit_database", subunitsReader => Units.LoadSubunits(subunitsReader));
		armiesParser.RegisterKeyword("units_database", unitsReader => Units.LoadUnits(unitsReader, LocDB, Defines));

		armiesParser.ParseStream(reader);
	}

	private SimpleDel LoadPlayerCountries(OrderedSet<string> playerCountriesToLog) {
		return reader => {
			var playedCountryBlocParser = new Parser();
			playedCountryBlocParser.RegisterKeyword("country", countryReader => {
				var countryId = countryReader.GetULong();
				var country = Countries[countryId];
				country.PlayerCountry = true;
				playerCountriesToLog.Add(country.Tag);
			});
			playedCountryBlocParser.IgnoreUnregisteredItems();
			playedCountryBlocParser.ParseStream(reader);
		};
	}

	private void LoadProvinces(BufferedReader reader) {
		Logger.Info("Loading provinces...");
		Provinces.LoadProvinces(reader, States, Countries, MapData);
		Logger.Debug($"Ignored Province tokens: {Province.IgnoredTokens}");
		Logger.Info($"Loaded {Provinces.Count} provinces.");

		Logger.IncrementProgress();
	}

	private void LoadStates(BufferedReader reader) {
		Logger.Info("Loading states...");
		var statesBlocParser = new Parser();
		statesBlocParser.RegisterKeyword("state_database", statesReader => States.LoadStates(statesReader, Areas, Countries));
		statesBlocParser.IgnoreAndLogUnregisteredItems();
		statesBlocParser.ParseStream(reader);
		Logger.Debug($"Ignored state keywords: {StateCollection.IgnoredStateKeywords}");
		Logger.Info($"Loaded {States.Count} states.");
		Logger.IncrementProgress();
	}

	private static void VerifySaveVersion(ConverterVersion converterVersion, BufferedReader reader) {
		var imperatorVersion = new GameVersion(reader.GetString());
		Logger.Info($"Save game version: {imperatorVersion}");

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
	}

	private void LoadSaveDate(Configuration config, BufferedReader reader) {
		var dateString = reader.GetString();
		EndDate = new Date(dateString, AUC: true);  // converted to AD
		Logger.Info($"Date: {dateString} AUC ({EndDate} AD)");

		if (EndDate > config.CK3BookmarkDate) {
			config.CK3BookmarkDate = new Date(EndDate);
			Logger.Warn($"CK3 bookmark date can't be earlier than save date. Changed to {config.CK3BookmarkDate}.");
		}
	}

	private void ReadVariablesFromSave(BufferedReader reader) {
		Logger.Info("Reading global variables...");

		var variables = new HashSet<string>();
		var variablesParser = new Parser();
		variablesParser.RegisterKeyword("data", dataReader => {
			var blobParser = new Parser();
			blobParser.RegisterKeyword("flag", blobReader => variables.Add(blobReader.GetString()));
			blobParser.IgnoreUnregisteredItems();
			foreach (var blob in new BlobList(dataReader).Blobs) {
				var blobReader = new BufferedReader(blob);
				blobParser.ParseStream(blobReader);
			}
		});
		variablesParser.IgnoreAndLogUnregisteredItems();
		variablesParser.ParseStream(reader);
		GlobalFlags = variables.ToImmutableHashSet();

		Logger.IncrementProgress();
	}

	private void ParseGenes() {
		Logger.Debug("Parsing genes...");
		genesDB = new GenesDB(ModFS);
	}
	private void LoadPreImperatorRulers() {
		const string filePath = "configurables/characters_prehistory.txt";
		const string noRulerWarning = "Pre-Imperator ruler term has no pre-Imperator ruler!";
		const string noCountryIdWarning = "Pre-Imperator ruler term has no country ID!";

		var preImperatorRulerTerms = new Dictionary<ulong, List<RulerTerm>>(); // <country id, list of terms>
		var parser = new Parser();
		parser.RegisterKeyword("ruler", reader => {
			var rulerTerm = new RulerTerm(reader, Countries);
			if (rulerTerm.PreImperatorRuler is null) {
				Logger.Warn(noRulerWarning);
				return;
			}
			if (rulerTerm.PreImperatorRuler.Country is null) {
				Logger.Warn(noCountryIdWarning);
				return;
			}
			var countryId = rulerTerm.PreImperatorRuler.Country.Id;
			Countries[countryId].RulerTerms.Add(rulerTerm);
			if (preImperatorRulerTerms.TryGetValue(countryId, out var list)) {
				list.Add(rulerTerm);
			} else {
				preImperatorRulerTerms[countryId] = new List<RulerTerm> { rulerTerm };
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(filePath);

		foreach (var country in Countries) {
			country.RulerTerms = [.. country.RulerTerms.OrderBy(t => t.StartDate)];
		}

		// verify with data from historical_regnal_numbers
		var regnalNameCounts = new Dictionary<ulong, Dictionary<string, int>>(); // <country id, <name, count>>
		foreach (var country in Countries) {
			if (!preImperatorRulerTerms.ContainsKey(country.Id)) {
				continue;
			}

			regnalNameCounts.Add(country.Id, []);
			var countryRulerTerms = regnalNameCounts[country.Id];

			foreach (var term in preImperatorRulerTerms[country.Id]) {
				if (term.PreImperatorRuler is null) {
					Logger.Warn(noRulerWarning);
					continue;
				}
				var name = term.PreImperatorRuler.Name;
				if (name is null) {
					Logger.Warn("Pre-Imperator ruler has no country name!");
					continue;
				}
				if (countryRulerTerms.TryGetValue(name, out int value)) {
					countryRulerTerms[name] = value + 1;
				} else {
					countryRulerTerms[name] = 1;
				}
			}
		}
		foreach (var country in Countries) {
			bool equal;
			if (!regnalNameCounts.TryGetValue(country.Id, out Dictionary<string, int>? countsForTitle)) {
				equal = country.HistoricalRegnalNumbers.Count == 0;
			} else {
				equal = country.HistoricalRegnalNumbers.OrderBy(kvp => kvp.Key)
					.SequenceEqual(countsForTitle.OrderBy(kvp => kvp.Key)
					);
			}

			if (!equal) {
				Logger.Debug($"List of pre-Imperator rulers of {country.Tag} doesn't match data from save!");
			}
		}
	}

	private void LoadModFilesystemDependentData() {
		// Some stuff can be loaded in parallel to save time.
		Parallel.Invoke(
			() => LoadImperatorLocalization(),
			() => {
				MapData = new MapData(ModFS);
				
				Areas.LoadAreas(ModFS, Provinces);
				ImperatorRegionMapper = new ImperatorRegionMapper(Areas, MapData);
			},
			() => InventionsDB.LoadInventions(ModFS),
			() => Country.LoadGovernments(ModFS),
			ParseGenes,
			() => {
				Defines.LoadDefines(ModFS);
				scriptValues.LoadScriptValues(ModFS, Defines);
				Logger.IncrementProgress();
			},
			() => {
				Logger.Info("Loading named colors...");
				NamedColors.LoadNamedColors("common/named_colors", ModFS);
				ColorFactory.AddNamedColorDict(NamedColors);
				Logger.IncrementProgress();
			},
			() => CoaMapper = new CoaMapper(ModFS),
			() => CulturesDB.Load(ModFS)
		);
		
		Parallel.Invoke(
			() => ImperatorRegionMapper.LoadRegions(ModFS, ColorFactory), // depends on ColorFactory
			() => {
				Religions = new ReligionCollection(scriptValues); // depends on scriptValues
				Religions.LoadDeities(ModFS);
				Religions.LoadReligions(ModFS);
			}
		);
		
		Logger.IncrementProgress();
	}

	private void LoadImperatorLocalization(){
		LocDB.ScrapeLocalizations(ModFS);

		// Now that all the I:R loc is loaded, replace substitution parameters with actual loc.
		// For example:
		//  E23: "$NABATEAN_SUBJECT$"
		//  NABATEAN_SUBJECT: "Edom"
		// Becomes:
		//  E23: "Edom"
		//  NABATEAN_SUBJECT: "Edom"

		foreach (var locBlock in LocDB) {
			foreach (var (language, loc) in locBlock.ToArray()) {
				if (loc is null) {
					continue;
				}
				
				ReplaceSubstitutionKeysInLoc(locBlock, language, loc);
			}
		}
	}

	private void ReplaceSubstitutionKeysInLoc(LocBlock locBlock, string language, string loc) {
		Regex substitutionRegex = SubstitutionRegex();
		
		var matches = substitutionRegex.Matches(loc);
		foreach (Match? match in matches) {
			if (match is null) {
				continue;
			}
					
			var substitutionKey = match.Value[1..^1];
			
			// Avoid infinite recursion by checking if the key is already in the loc block.
			if (substitutionKey == locBlock.Id) {
				continue;
			}
			
			var substitutionLocBlock = LocDB.GetLocBlockForKey(substitutionKey);
			if (substitutionLocBlock is null) {
				continue;
			}
			var substitutionLoc = substitutionLocBlock[language];
			if (substitutionLoc is null) {
				Logger.Debug($"Substitution for key {substitutionKey} not found in {language} localization for key {locBlock.Id}.");
				continue;
			}
			
			// If the substitution loc contains a substitution key, replace it first.
			ReplaceSubstitutionKeysInLoc(substitutionLocBlock, language, substitutionLoc);
					
			locBlock[language] = loc.Replace(match.Value, substitutionLoc);
		}
	}

	private BufferedReader ProcessSave(string saveGamePath) {
		switch (saveType) {
			case SaveType.Plaintext:
				Logger.Info("Importing debug_mode Imperator save.");
				return ProcessDebugModeSave(saveGamePath);
			case SaveType.CompressedEncoded:
				Logger.Info("Importing regular Imperator save.");
				return ProcessCompressedEncodedSave(saveGamePath);
			case SaveType.Invalid:
			default:
				throw new InvalidDataException("Unknown save type.");
		}
	}
	private void VerifySave(string saveGamePath) {
		if (new FileInfo(saveGamePath).Length < 4) {
			throw new UserErrorException("Save file is too small for a valid save.");
		}
		
		using var saveStream = File.Open(saveGamePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		var buffer = new byte[10];
		var bytesRead = saveStream.Read(buffer, 0, 4);
		if (bytesRead < 4) {
			throw new InvalidDataException("Failed to read 4 bytes from save.");
		}
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
		saveType = SaveType.Plaintext;
		for (var i = 0; i < 65533; ++i) {
			if (BitConverter.ToUInt32(bigBuf, i) == 0x04034B50 && BitConverter.ToUInt16(bigBuf, i - 2) == 4) {
				saveType = SaveType.CompressedEncoded;
			}
		}
	}
	private static BufferedReader ProcessDebugModeSave(string saveGamePath) {
		try {
			var fileStream = File.Open(saveGamePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return new BufferedReader(fileStream);
		} catch (IOException e) {
			Logger.Debug($"Failed to open save file \"{saveGamePath}\": {e.Message}");
			throw new UserErrorException("Could not open the save file! " +
			                             "Close Imperator: Rome before running the converter.");
		}
	}
	private static BufferedReader ProcessCompressedEncodedSave(string saveGamePath) {
		Helpers.RakalyCaller.MeltSave(saveGamePath);
		return new BufferedReader(File.Open("temp/melted_save.rome", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
	}

	private readonly IgnoredKeywordsSet ignoredTokens = [];

	[GeneratedRegex(@"\bSAV\w*\b")]
	private static partial Regex SaveStartRegex();
	[GeneratedRegex(@"^\S+=\s*\{[\s\S]*?^\}", RegexOptions.Multiline)]
	private static partial Regex FlagDefinitionRegex();
	[GeneratedRegex(@"\$[A-Z_]*\$")]
	private static partial Regex SubstitutionRegex();
}