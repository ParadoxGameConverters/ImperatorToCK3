using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Exceptions;
using commonItems.Mods;
using DotLiquid;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Legends;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.HolySiteEffect;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.Mappers.War;
using ImperatorToCK3.Mappers.UnitType;
using ImperatorToCK3.Outputter;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Open.Collections;
using DiplomacyDB = ImperatorToCK3.CK3.Diplomacy.DiplomacyDB;
using System.Collections.Frozen;

namespace ImperatorToCK3.CK3;

internal sealed class World {
	public OrderedSet<Mod> LoadedMods { get; } = [];
	public ModFilesystem ModFS { get; }
	public CK3LocDB LocDB { get; } = [];
	private ScriptValueCollection ScriptValues { get; } = new();
	public NamedColorCollection NamedColors { get; } = new();
	public CharacterCollection Characters { get; } = new();
	public DynastyCollection Dynasties { get; } = [];
	public HouseCollection DynastyHouses { get; } = [];
	public ProvinceCollection Provinces { get; } = new();
	public Title.LandedTitles LandedTitles { get; } = new();
	public PillarCollection CulturalPillars { get; private set; } = null!;
	public CultureCollection Cultures { get; private set; } = null!;
	public ReligionCollection Religions { get; }
	public IdObjectCollection<string, MenAtArmsType> MenAtArmsTypes { get; } = new();
	public MapData MapData { get; private set; } = null!;
	public List<Wars.War> Wars { get; } = [];
	public LegendSeedCollection LegendSeeds { get; } = [];
	public DiplomacyDB Diplomacy { get; } = new();

	public CK3RegionMapper CK3RegionMapper { get; }

	internal CoaMapper CK3CoaMapper { get; private set; } = null!;
	private readonly List<string> enabledDlcFlags = [];

	/// <summary>
	/// Date based on I:R save date, but normalized for CK3 purposes.
	/// </summary>
	public Date CorrectedDate { get; private set; } = new Date(2, 1, 1); // overwritten by DetermineCK3BookmarkDate

	internal World(Imperator.World impWorld, Configuration config, Thread? irCoaExtractThread) {
		Logger.Info("*** Hello CK3, let's get painting. ***");

		Religions = new ReligionCollection(LandedTitles);
		var liquidVariables = InitializeWorldBootstrap(impWorld, config);
		var (modFS, ck3ColorFactory) = SetupOutputAndBaseData(impWorld, config, liquidVariables);
		ModFS = modFS;
		var ck3RegionMapper = LoadCulturesAndTitles(impWorld, config, liquidVariables, ck3ColorFactory);
		CK3RegionMapper = ck3RegionMapper;
		var irRegionMapper = impWorld.ImperatorRegionMapper;
		imperatorRegionMapper = irRegionMapper;
		var (cultureMapper, traitMapper, dnaFactory) = InitializeReligionsCharactersAndMappers(impWorld, config, liquidVariables, ck3ColorFactory, ck3RegionMapper, irRegionMapper);
		var religionMapper = new ReligionMapper(Religions, irRegionMapper, ck3RegionMapper);
		ValidateMappingsAndTraits(impWorld, config, liquidVariables, cultureMapper, religionMapper, traitMapper);
		ImportCharactersAndPreserveIds(impWorld, religionMapper, cultureMapper, traitMapper, dnaFactory, config);
		var governmentMapper = LoadDynastiesAndGovernments(impWorld, cultureMapper);
		var (countyLevelCountries, countyLevelGovernorships) = ImportCountriesAndProvinces(impWorld, config, liquidVariables, irCoaExtractThread, cultureMapper, religionMapper, governmentMapper, irRegionMapper);
		ApplyProvinceAndReligionTweaks(impWorld, config, countyLevelCountries, countyLevelGovernorships);
		FinalizeWorldState(impWorld, config, dnaFactory);
	}

	private Hash InitializeWorldBootstrap(Imperator.World impWorld, Configuration config) {
		warMapper.DetectUnmappedWarGoals(impWorld.ModFS);
		DetermineCK3Dlcs(config);
		LoadAndDetectCK3Mods(config);
		var liquidVariables = config.GetLiquidVariables();
		DetermineCK3BookmarkDate(impWorld, config);
		return liquidVariables;
	}

	private (ModFilesystem modFS, ColorFactory ck3ColorFactory) SetupOutputAndBaseData(Imperator.World impWorld, Configuration config, Hash liquidVariables) {
		string outputModPath = Path.Join("output", config.OutputModName);
		WorldOutputter.ClearOutputModFolder(outputModPath);
		WorldOutputter.CreateModFolder(outputModPath);
		WorldOutputter.CopyBlankModFilesToOutput(outputModPath, liquidVariables);
		LoadedMods.Add(new Mod("blankMod", outputModPath));
		var modFS = new ModFilesystem(Path.Combine(config.CK3Path, "game"), LoadedMods);

		var ck3Defines = new Defines();
		ck3Defines.LoadDefines(modFS);
		ColorFactory ck3ColorFactory = new();
		Parallel.Invoke(
			() => LoadCorrectProvinceMappingsFile(impWorld, config),
			() => {
				LocDB.LoadLocFromModFS(modFS, config.GetActiveCK3ModFlags());
				Logger.IncrementProgress();
			},
			() => ScriptValues.LoadScriptValues(modFS, ck3Defines),
			() => {
				NamedColors.LoadNamedColors("common/named_colors", modFS);
				ck3ColorFactory.AddNamedColorDict(NamedColors);
			},
			() => {
				Logger.Info("Loading map data...");
				MapData = new MapData(modFS);
			},
			() => CK3CoaMapper = new(modFS),
			() => FileTweaker.ModifyAndRemovePartsOfFiles(modFS, outputModPath, config).Wait()
		);
		return (modFS, ck3ColorFactory);
	}

	private CK3RegionMapper LoadCulturesAndTitles(Imperator.World impWorld, Configuration config, Hash liquidVariables, ColorFactory ck3ColorFactory) {
		var ck3ModFlags = config.GetCK3ModFlags();
		Parallel.Invoke(
			() => provinceMapper.DetectInvalidMappings(impWorld.MapData, MapData),
			() => {
				Logger.Info("Loading cultural pillars...");
				CulturalPillars = new(ck3ColorFactory, ck3ModFlags);
				CulturalPillars.LoadPillars(ModFS, ck3ModFlags);
				Logger.Info("Loading converter cultural pillars...");
				CulturalPillars.LoadConverterPillars("configurables/cultural_pillars", ck3ModFlags, liquidVariables);
				Cultures = new CultureCollection(ck3ColorFactory, CulturalPillars, ck3ModFlags);
				Cultures.LoadNameLists(ModFS);
				Cultures.LoadInnovationIds(ModFS);
				Cultures.LoadCultures(ModFS);
				Cultures.LoadConverterCultures("configurables/converter_cultures.txt");
				Cultures.WarnAboutCircularParents();
				Logger.IncrementProgress();
			},
			() => LoadMenAtArmsTypes(ModFS, ScriptValues),
			() => LoadTitles(config, ck3ColorFactory)
		);
		return new CK3RegionMapper(ModFS, LandedTitles);
	}

	private void LoadTitles(Configuration config, ColorFactory ck3ColorFactory) {
		LandedTitles.LoadTitles(ModFS, LocDB, ck3ColorFactory);
		if (config.StaticDeJure) {
			Logger.Info("Setting static de jure kingdoms and empires...");
			Title.LandedTitles overrideTitles = [];
			overrideTitles.LoadStaticTitles(ck3ColorFactory);
			LandedTitles.CarveTitles(overrideTitles);
			Logger.IncrementProgress();
		}
		LandedTitles.SetCoatsOfArms(CK3CoaMapper);
		LandedTitles.LoadHistory(config, ModFS);
		LandedTitles.LoadCulturalNamesFromConfigurables();
	}

	private (CultureMapper cultureMapper, TraitMapper traitMapper, DNAFactory dnaFactory) InitializeReligionsCharactersAndMappers(Imperator.World impWorld, Configuration config, Hash liquidVariables, ColorFactory ck3ColorFactory, CK3RegionMapper ck3RegionMapper, ImperatorRegionMapper irRegionMapper) {
		CultureMapper cultureMapper = null!;
		TraitMapper traitMapper = null!;
		DNAFactory dnaFactory = null!;
		Parallel.Invoke(
			() => LoadReligions(liquidVariables, ck3ColorFactory),
			() => cultureMapper = new CultureMapper(irRegionMapper, ck3RegionMapper, Cultures),
			() => {
				traitMapper = new("configurables/trait_map.txt", ModFS);
				traitMapper.LogUnmappedImperatorTraits(impWorld.ModFS);
			},
			() => {
				Logger.Info("Initializing DNA factory...");
				dnaFactory = new(impWorld.ModFS, ModFS);
				Logger.IncrementProgress();
			},
			() => {
				Characters.LoadCK3Characters(ModFS, config.CK3BookmarkDate);
				Logger.IncrementProgress();
			}
		);
		return (cultureMapper, traitMapper, dnaFactory);
	}

	private void LoadReligions(Hash liquidVariables, ColorFactory ck3ColorFactory) {
		Religions.LoadDoctrines(ModFS);
		Logger.Info("Loaded CK3 doctrines.");
		Religions.LoadConverterHolySites("configurables/converter_holy_sites.txt");
		Logger.Info("Loaded converter holy sites.");
		Religions.LoadHolySites(ModFS);
		Logger.Info("Loaded CK3 holy sites.");
		Logger.Info("Loading religions from CK3 game and mods...");
		Religions.LoadReligions(ModFS, ck3ColorFactory);
		Logger.Info("Loaded CK3 religions.");
		Logger.IncrementProgress();
		Logger.Info("Loading converter faiths...");
		Religions.LoadConverterFaiths("configurables/converter_faiths.liquid", ck3ColorFactory, liquidVariables);
		Logger.Info("Loaded converter faiths.");
		Logger.IncrementProgress();
		Religions.RemoveChristianAndIslamicSyncretismFromAllFaiths();
		LandedTitles.RemoveLiegeEntriesFromReligiousHeadHistory(Religions);
		Religions.LoadReplaceableHolySites("configurables/replaceable_holy_sites.txt");
		Logger.Info("Loaded replaceable holy sites.");
	}

	private void ValidateMappingsAndTraits(Imperator.World impWorld, Configuration config, Hash liquidVariables, CultureMapper cultureMapper, ReligionMapper religionMapper, TraitMapper traitMapper) {
		Parallel.Invoke(
			() => Cultures.ImportTechnology(impWorld.Countries, cultureMapper, provinceMapper, impWorld.InventionsDB, impWorld.LocDB, liquidVariables),
			() => LogMissingImperatorReligionMappings(impWorld, config, religionMapper),
			() => LogMissingImperatorCultureMappings(impWorld, cultureMapper),
			() => Characters.RemoveUndefinedTraits(traitMapper)
		);
	}

	private static void LogMissingImperatorReligionMappings(Imperator.World impWorld, Configuration config, ReligionMapper religionMapper) {
		foreach (var irReligionId in impWorld.Religions.Select(r => r.Id)) {
			var baseMapping = religionMapper.Match(irReligionId, null, null, null, null, config);
			if (baseMapping is not null) {
				continue;
			}

			string religionStr = "ID: " + irReligionId;
			var localizedName = impWorld.LocDB.GetLocBlockForKey(irReligionId)?["english"];
			if (localizedName is not null) {
				religionStr += $", name: {localizedName}";
			}
			Logger.Warn($"No base mapping found for I:R religion {religionStr}!");
		}
	}

	private static void LogMissingImperatorCultureMappings(Imperator.World impWorld, CultureMapper cultureMapper) {
		var irCultureIds = impWorld.CulturesDB.SelectMany(g => g.Select(c => c.Id));
		foreach (var irCultureId in irCultureIds) {
			var baseMapping = cultureMapper.Match(irCultureId, null, null, null);
			if (baseMapping is not null) {
				continue;
			}

			string cultureStr = "ID: " + irCultureId;
			var localizedName = impWorld.LocDB.GetLocBlockForKey(irCultureId)?["english"];
			if (localizedName is not null) {
				cultureStr += $", name: {localizedName}";
			}
			Logger.Warn($"No base mapping found for I:R culture {cultureStr}!");
		}
	}

	private void ImportCharactersAndPreserveIds(Imperator.World impWorld, ReligionMapper religionMapper, CultureMapper cultureMapper, TraitMapper traitMapper, DNAFactory dnaFactory, Configuration config) {
		Characters.ImportImperatorCharacters(impWorld, religionMapper, cultureMapper, Cultures, traitMapper, nicknameMapper, provinceMapper, deathReasonMapper, dnaFactory, LocDB, impWorld.EndDate, config);
		Characters.LoadCharacterIDsToPreserve(config.CK3BookmarkDate);
		ClearFeaturedCharactersDescriptions(config.CK3BookmarkDate);
	}

	private GovernmentMapper LoadDynastiesAndGovernments(Imperator.World impWorld, CultureMapper cultureMapper) {
		Dynasties.LoadCK3Dynasties(ModFS);
		Characters.RemoveInvalidDynastiesFromHistory(Dynasties);
		Dynasties.ImportImperatorFamilies(impWorld, cultureMapper, impWorld.LocDB, LocDB, CorrectedDate);
		DynastyHouses.LoadCK3Houses(ModFS);
		return InitializeGovernmentMapper();
	}

	private (List<KeyValuePair<Country, Dependency?>> countyLevelCountries, List<Governorship> countyLevelGovernorships) ImportCountriesAndProvinces(Imperator.World impWorld, Configuration config, Hash liquidVariables, Thread? irCoaExtractThread, CultureMapper cultureMapper, ReligionMapper religionMapper, GovernmentMapper governmentMapper, ImperatorRegionMapper irRegionMapper) {
		irCoaExtractThread?.Join();
		SuccessionLawMapper successionLawMapper = new("configurables/succession_law_map.liquid", liquidVariables);
		List<KeyValuePair<Country, Dependency?>> countyLevelCountries = [];
		LandedTitles.ImportImperatorCountries(impWorld.Countries, impWorld.Dependencies, tagTitleMapper, impWorld.LocDB, LocDB, provinceMapper, impWorld.CoaMapper, governmentMapper, successionLawMapper, definiteFormMapper, religionMapper, cultureMapper, nicknameMapper, Characters, CorrectedDate, config, countyLevelCountries, enabledDlcFlags);
		Provinces.ImportVanillaProvinces(ModFS, MapData.ProvinceDefinitions, Religions, Cultures);
		Provinces.ImportImperatorProvinces(impWorld, MapData, LandedTitles, cultureMapper, religionMapper, provinceMapper, CorrectedDate, config);
		Provinces.LoadPrehistory();
		var countyLevelGovernorships = new List<Governorship>();
		LandedTitles.ImportImperatorGovernorships(impWorld, Provinces, tagTitleMapper, impWorld.LocDB, LocDB, config, provinceMapper, definiteFormMapper, irRegionMapper, impWorld.CoaMapper, countyLevelGovernorships);
		return (countyLevelCountries, countyLevelGovernorships);
	}

	private void ApplyProvinceAndReligionTweaks(Imperator.World impWorld, Configuration config, List<KeyValuePair<Country, Dependency?>> countyLevelCountries, List<Governorship> countyLevelGovernorships) {
		OverwriteCountiesHistory(impWorld.Countries, impWorld.JobsDB.Governorships, countyLevelCountries, countyLevelGovernorships, impWorld.Characters, impWorld.Provinces, CorrectedDate);
		ImportImperatorHoldingsIfNotDisabledByConfiguration(impWorld, config);
		LandedTitles.ImportDevelopmentFromImperator(Provinces, CorrectedDate, config.ImperatorCivilizationWorth);
		HandleIcelandAndFaroeIslands(impWorld, config);
		RemoveIslamFromMapIfNotInImperator(impWorld, config);
		HandleChristianity(impWorld, config);
		HandleManichaeism(impWorld, config);
		GenerateFillerHoldersForUnownedLands(impWorld.Provinces, Cultures, config);
		LandedTitles.RemoveInvalidLandlessTitles(config.CK3BookmarkDate);
		Logger.IncrementProgress();
		if (!config.StaticDeJure) {
			LandedTitles.SetDeJureKingdomsAndAbove(config.CK3BookmarkDate, Cultures, Characters, MapData, CK3RegionMapper, LocDB, provinceMapper);
		}
	}

	private void FinalizeWorldState(Imperator.World impWorld, Configuration config, DNAFactory dnaFactory) {
		Dynasties.SetCoasForRulingDynasties(LandedTitles, config.CK3BookmarkDate);
		Characters.RemoveEmployerIdFromLandedCharacters(LandedTitles, CorrectedDate);
		Characters.PurgeUnneededCharacters(LandedTitles, Dynasties, DynastyHouses, config.CK3BookmarkDate);
		Characters.ConvertImperatorCharacterDNA(dnaFactory);
		if (config.CK3BookmarkDate.DiffInYears(impWorld.EndDate) > 1) {
			Characters.GenerateSuccessorsForOldCharacters(LandedTitles, Cultures, impWorld.EndDate, config.CK3BookmarkDate, impWorld.RandomSeed);
		}
		Characters.DistributeCountriesGold(LandedTitles, config);
		Characters.ImportLegions(LandedTitles, impWorld.Units, impWorld.Characters, impWorld.Countries, config.CK3BookmarkDate, unitTypeMapper, MenAtArmsTypes, provinceMapper, LocDB, config);
		Characters.CalculateChineseDynasticCycleVariables(LandedTitles, impWorld.EndDate, config.CK3BookmarkDate);
		LandedTitles.CleanUpHistory(Characters, config.CK3BookmarkDate);
		LandedTitles.ImportImperatorGovernmentOffices(impWorld.JobsDB.OfficeJobs, Religions, impWorld.EndDate);
		Parallel.Invoke(
			() => ImportImperatorWars(impWorld, config.CK3BookmarkDate),
			() => {
				var holySiteEffectMapper = new HolySiteEffectMapper("configurables/holy_site_effect_mappings.txt");
				Religions.DetermineHolySites(Provinces, impWorld.Religions, holySiteEffectMapper, config.CK3BookmarkDate);
				Religions.GenerateMissingReligiousHeads(LandedTitles, Characters, Provinces, Cultures, config.CK3BookmarkDate);
				Logger.IncrementProgress();
			},
			() => {
				LegendSeeds.LoadSeeds(ModFS);
				LegendSeeds.RemoveAnachronisticSeeds("configurables/legend_seeds_to_remove.txt");
			},
			() => Diplomacy.ImportImperatorLeagues(impWorld.DefensiveLeagues, impWorld.Countries)
		);
	}

	private void ImportImperatorHoldingsIfNotDisabledByConfiguration(Imperator.World irWorld, Configuration config) {
		if (!config.SkipHoldingOwnersImport) {
			// Import holding owners as barons and counts.
			LandedTitles.ImportImperatorHoldings(Provinces, irWorld.Characters, irWorld.EndDate);
		} else {
			Logger.Info("Skipping holding owners import per configuration.");
		}
	}

	private GovernmentMapper InitializeGovernmentMapper() {
		// Load existing CK3 government IDs.
		Logger.Info("Loading CK3 government IDs...");
		var ck3GovernmentIds = new HashSet<string>();
		var governmentsParser = new Parser(implicitVariableHandling: true);
		governmentsParser.RegisterRegex(CommonRegexes.String, (reader, governmentId) => {
			ck3GovernmentIds.Add(governmentId);
			ParserHelpers.IgnoreItem(reader);
		});
		governmentsParser.ParseGameFolder("common/governments", ModFS, "txt", recursive: false, logFilePaths: true);
		Logger.IncrementProgress();

		GovernmentMapper governmentMapper = new([.. ck3GovernmentIds]);
		Logger.IncrementProgress();
		return governmentMapper;
	}

	private void RemoveIslamFromMapIfNotInImperator(Imperator.World irWorld, Configuration config) {
		// Check if any muslim religion exists in Imperator. Otherwise, remove Islam from the entire CK3 map.
		var possibleMuslimReligionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "muslim", "islam", "sunni", "shiite" };
		var muslimReligionExists = irWorld.Religions.Any(r => possibleMuslimReligionIds.Contains(r.Id));
		if (muslimReligionExists) {
			Logger.Info("Found muslim religion in Imperator save, keeping Islam in CK3.");
		} else {
			RemoveIslam(config);
		}
		Logger.IncrementProgress();
	}

	private void HandleChristianity(Imperator.World irWorld, Configuration config) {
		var ck3BookmarkDate = config.CK3BookmarkDate;
		if (!Religions.TryGetValue("christianity_religion", out var christianity)) {
			Logger.Debug("christianity_religion not found in religions.");
			return;
		}

		bool irChristianityExists = irWorld.Religions.Any(r => r.Id.Equals("christianity", StringComparison.OrdinalIgnoreCase));
		if (irChristianityExists) {
			Date nestorianSchismDate = new(432, 10, 30); // should match @after_nestorian_schism value from religion_map.txt
			Date chalcedonianSchismDate = new(451, 8, 25); // should match @after_chalcedon value from religion_map.txt
			if (ck3BookmarkDate < chalcedonianSchismDate) {
				ReplaceMiaphysiteChristianityWithNiceneChristianity(christianity, ck3BookmarkDate);
			}
			if (ck3BookmarkDate < nestorianSchismDate) {
				ReplaceNestorianChristianityWithNiceneChristianity(christianity, ck3BookmarkDate);
			}
		} else {
			RemoveChristianity(christianity, ck3BookmarkDate);
		}
	}

	private void HandleManichaeism(Imperator.World irWorld, Configuration config) {
		// Check if the manichaeism_spreads variable from the Timeline Extension from Invictus is set.
		bool irHasManichaeismSpreadVariable = irWorld.GlobalFlags.Any(f => f.Equals("manichaeism_spreads", StringComparison.OrdinalIgnoreCase));
		if (irHasManichaeismSpreadVariable) {
			Logger.Info("Found manichaeism_spreads variable set to yes in Imperator save, keeping Manichaeism in CK3.");
			return;
		}

		Logger.Info("No indication of Manichaeism spreading in Imperator save, removing Manichaeism from CK3...");
		HashSet<string> ck3ManicheanFaithIds = ["manichean", "mingism"];
		var ck3BookmarkDate = config.CK3BookmarkDate;
		var manichaeanProvinces = Provinces
			.Where(p => p.GetFaithId(ck3BookmarkDate) is string faithId && ck3ManicheanFaithIds.Contains(faithId))
			.ToHashSet();

		UseNeighborProvincesToConvertProvincesOfReligion(manichaeanProvinces, ck3BookmarkDate);
		UseClosestProvincesToConvertProvincesOfReligion(manichaeanProvinces, ck3BookmarkDate);

		// Log warning if there are still Manichaean provinces left.
		if (manichaeanProvinces.Count > 0) {
			Logger.Warn($"{manichaeanProvinces.Count} Manichaean provinces left after removing Manichaeism: " +
			            $"{string.Join(", ", manichaeanProvinces.Select(p => p.Id))}");
		}
	}

	private void DetermineCK3BookmarkDate(Imperator.World irWorld, Configuration config) {
		CorrectedDate = irWorld.EndDate.Year > 1 ? irWorld.EndDate : new Date(2, 1, 1);
		if (config.CK3BookmarkDate.Year == 0) { // bookmark date is not set
			config.CK3BookmarkDate = CorrectedDate;
			Logger.Info($"CK3 bookmark date set to: {config.CK3BookmarkDate}");
		} else if (CorrectedDate > config.CK3BookmarkDate) {
			Logger.Warn($"Corrected save can't be later than CK3 bookmark date, setting CK3 bookmark date to {CorrectedDate}!");
			config.CK3BookmarkDate = CorrectedDate;
		}
	}

	private void LoadAndDetectCK3Mods(Configuration config) {
		Logger.Info("Detecting selected CK3 mods...");
		List<Mod> incomingCK3Mods = new();
		foreach (var modPath in config.SelectedCK3Mods) {
			Logger.Info($"\tSelected CK3 mod: {modPath}");
			incomingCK3Mods.Add(new Mod(string.Empty, modPath));
		}
		Logger.IncrementProgress();

		// Let's locate, verify and potentially update those mods immediately.
		ModLoader modLoader = new();
		modLoader.LoadMods(Directory.GetParent(config.CK3ModsPath)!.FullName, incomingCK3Mods, config.CK3Version, throwForOutOfDateMods: true);
		
		// Add modLoader's UsableMods to LoadedMods.
		LoadedMods.AddRange(modLoader.UsableMods);
		config.DetectSpecificCK3Mods(LoadedMods);
	}

	private void ImportImperatorWars(Imperator.World irWorld, Date ck3BookmarkDate) {
		Logger.Info("Importing I:R wars...");

		foreach (var irWar in irWorld.Wars) {
			try {
				var ck3War = new Wars.War(irWar, warMapper, provinceMapper, irWorld.Countries, irWorld.States, Provinces, LandedTitles, ck3BookmarkDate);
				if (ck3War.Attackers.Count == 0) {
					Logger.Info($"Skipping war that starts at {ck3War.StartDate}: no CK3 attackers!");
					continue;
				}
				if (ck3War.Defenders.Count == 0) {
					Logger.Info($"Skipping war that starts at {ck3War.StartDate}: no CK3 defenders!");
					continue;
				}
				if (ck3War.CasusBelli is null) {
					Logger.Info($"Skipping war that starts at {ck3War.StartDate}: no CK3 casus belli!");
					continue;
				}
				Wars.Add(ck3War);
			} catch (ConverterException e) {
				Logger.Debug($"Can't import war that starts at {irWar.StartDate}: {e.Message}");
			}
		}
		Logger.IncrementProgress();
	}

	private void LoadCorrectProvinceMappingsFile(Imperator.World irWorld, Configuration config) {
		// Terra Indomita mappings should be used if either TI or Antiquitas is detected.
		bool irHasTI = config.TerraIndomitaDetected;

		bool ck3HasRajasOfAsia = config.RajasOfAsiaEnabled;
		bool ck3HasAEP = config.AsiaExpansionProjectEnabled;

		string mappingsToUse;
		if (irHasTI && ck3HasRajasOfAsia) {
			mappingsToUse = "terra_indomita_to_rajas_of_asia";
		} else if (irHasTI && ck3HasAEP) {
			mappingsToUse = "terra_indomita_to_aep";
		} else if (irHasTI) {
			mappingsToUse = "terra_indomita_to_vanilla_ck3";
		} else if (config.WhenTheWorldStoppedMakingSenseEnabled) {
			mappingsToUse = "invictus_to_wtwsms";
		} else if (config is {InvictusDetected: true, Invictus1_7Detected: true}) {
			mappingsToUse = "invictus_1_7_to_vanilla_ck3";
		} else if (config.InvictusDetected) {
			mappingsToUse = "invictus_to_vanilla_ck3";
		} else {
			mappingsToUse = "vanilla_ir_to_vanilla_ck3";
			Logger.Warn("Support for non-Invictus Imperator saves is deprecated.");
		}
		
		Logger.Info($"Using province mappings: {mappingsToUse}");
		var mappingsPath = Path.Combine("configurables/province_mappings", mappingsToUse + ".txt");
		
		provinceMapper.LoadMappings(mappingsPath);
	}

	private void LoadMenAtArmsTypes(ModFilesystem ck3ModFS, ScriptValueCollection scriptValues) {
		Logger.Info("Loading men-at-arms types...");

		const string maaPath = "common/men_at_arms_types";
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.String, (reader, typeId) => {
			MenAtArmsTypes.AddOrReplace(new MenAtArmsType(typeId, reader, scriptValues));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder(maaPath, ck3ModFS, "txt", true);
	}

	private void ClearFeaturedCharactersDescriptions(Date ck3BookmarkDate) {
		Logger.Info("Clearing featured characters' descriptions...");
		foreach (var title in LandedTitles) {
			if (!title.PlayerCountry) {
				continue;
			}
			var holderId = title.GetHolderId(ck3BookmarkDate);
			if (holderId != "0" && Characters.TryGetValue(holderId, out var holder)) {
				var locBlock = LocDB.GetOrCreateLocBlock($"{holder.GetName(ck3BookmarkDate)}_desc");
				locBlock.ModifyForEveryLanguage((loc, language) => string.Empty);
			}
		}
	}

	private void OverwriteCountiesHistory(CountryCollection irCountries, List<Governorship> governorships, List<KeyValuePair<Country, Dependency?>> countyLevelCountries, List<Governorship> countyLevelGovernorships, Imperator.Characters.CharacterCollection impCharacters, Imperator.Provinces.ProvinceCollection irProvinces, Date conversionDate) {
		Logger.Info("Overwriting counties' history...");
		var countyLevelCountriesByCountryId = GetFirstValuesByKey(countyLevelCountries, entry => entry.Key.Id);
		var governorshipsByCountryAndRegion = GetFirstValuesByKey(governorships, governorship => (governorship.Country.Id, governorship.Region.Id));
		FrozenSet<Governorship> countyLevelGovernorshipsSet = countyLevelGovernorships.ToFrozenSet();

		foreach (var county in LandedTitles.Counties) {
			if (county.NobleFamily == true) {
				continue;
			}
			if (county.CapitalBaronyProvinceId is null) {
				Logger.Warn($"County {county} has no capital barony province!");
				continue;
			}
			ulong capitalBaronyProvId = (ulong)county.CapitalBaronyProvinceId;
			if (capitalBaronyProvId == 0) {
				// title's capital province has an invalid ID (0 is not a valid province in CK3)
				Logger.Warn($"County {county} has invalid capital barony province!");
				continue;
			}

			if (!Provinces.ContainsKey(capitalBaronyProvId)) {
				Logger.Warn($"Capital barony province not found: {capitalBaronyProvId}");
				continue;
			}

			var ck3CapitalBaronyProvince = Provinces[capitalBaronyProvId];
			var irProvince = FindImperatorOwnerProvinceForCounty(county, capitalBaronyProvId, ck3CapitalBaronyProvince);

			if (irProvince is null) { // probably outside of Imperator map
				continue;
			}

			OverwriteCountyHistory(county, irProvince, irCountries, countyLevelCountriesByCountryId, governorshipsByCountryAndRegion, countyLevelGovernorshipsSet, impCharacters, irProvinces, conversionDate);
		}
		Logger.IncrementProgress();
	}

	private Imperator.Provinces.Province? FindImperatorOwnerProvinceForCounty(Title county, ulong capitalBaronyProvId, Province ck3CapitalBaronyProvince) {
		var irProvince = ck3CapitalBaronyProvince.PrimaryImperatorProvince;
		if (irProvince?.OwnerCountry is not null) {
			return irProvince;
		}

		var ownedSecondarySourceProvince = ck3CapitalBaronyProvince.SecondaryImperatorProvinces.FirstOrDefault(p => p.OwnerCountry is not null);
		if (ownedSecondarySourceProvince is not null) {
			Logger.Debug($"Using secondary source province {ownedSecondarySourceProvince.Id} of capital barony" +
			             $"province {capitalBaronyProvId} for history of county {county.Id}!");
			return ownedSecondarySourceProvince;
		}

		var ownedPrimaryProvinceFromBarony = FindOwnerProvinceFromCountyBaronies(county, useSecondarySources: false);
		if (ownedPrimaryProvinceFromBarony is not null) {
			var (province, ck3ProvinceId, baronyId) = ownedPrimaryProvinceFromBarony.Value;
			Logger.Debug($"Using province {ck3ProvinceId} of barony {baronyId} instead of" +
			             $"capital barony province {capitalBaronyProvId} for history of county {county.Id}!");
			return province;
		}

		var ownedSecondaryProvinceFromBarony = FindOwnerProvinceFromCountyBaronies(county, useSecondarySources: true);
		if (ownedSecondaryProvinceFromBarony is not null) {
			var (province, ck3ProvinceId, baronyId) = ownedSecondaryProvinceFromBarony.Value;
			Logger.Debug($"Using province {ck3ProvinceId} of barony {baronyId} instead of" +
			             $"capital barony province {capitalBaronyProvId} for history of county {county.Id}!");
			return province;
		}
		return null;
	}

	private (Imperator.Provinces.Province province, ulong ck3ProvinceId, string baronyId)? FindOwnerProvinceFromCountyBaronies(Title county, bool useSecondarySources) {
		foreach (var barony in county.DeJureVassals) {
			var baronyCk3ProvId = barony.ProvinceId;
			if (baronyCk3ProvId is null) {
				continue;
			}

			var province = useSecondarySources
				? Provinces[baronyCk3ProvId.Value].SecondaryImperatorProvinces.FirstOrDefault(p => p.OwnerCountry is not null)
				: Provinces[baronyCk3ProvId.Value].PrimaryImperatorProvince;
			if (province?.OwnerCountry is null) {
				continue;
			}

			return (province, baronyCk3ProvId.Value, barony.Id);
		}

		return null;
	}

	internal static Dictionary<TKey, TValue> GetFirstValuesByKey<TValue, TKey>(
		IEnumerable<TValue> values,
		Func<TValue, TKey> keySelector,
		IEqualityComparer<TKey>? comparer = null
	) where TKey : notnull {
		var indexedValues = comparer is null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(comparer);
		foreach (var value in values) {
			indexedValues.TryAdd(keySelector(value), value);
		}

		return indexedValues;
	}

	private void OverwriteCountyHistory(Title county, Imperator.Provinces.Province irProvince, CountryCollection irCountries,
		IReadOnlyDictionary<ulong, KeyValuePair<Country, Dependency?>> countyLevelCountriesByCountryId,
		IReadOnlyDictionary<(ulong CountryId, string RegionId), Governorship> governorshipsByCountryAndRegion,
		FrozenSet<Governorship> countyLevelGovernorshipsSet,
		Imperator.Characters.CharacterCollection irCharacters, Imperator.Provinces.ProvinceCollection irProvinces, Date conversionDate) {
		var irCountry = irProvince.OwnerCountry;

		if (irCountry is null || irCountry.CountryType == CountryType.rebels) { // e.g. uncolonized Imperator province
			county.SetHolder(null, conversionDate);
			county.SetDeFactoLiege(null, conversionDate);
			RevokeBaroniesFromCountyGivenToImperatorCharacter(county);
		} else {
			bool given = TryGiveCountyToCountyLevelRuler(county, irCountry, countyLevelCountriesByCountryId, irCountries);
			if (!given) {
				given = TryGiveCountyToGovernor(county, irProvince, irCountry, governorshipsByCountryAndRegion, irProvinces, countyLevelGovernorshipsSet, irCharacters);
			}
			if (!given) {
				given = TryGiveCountyToMonarch(county, irCountry);
			}
			if (!given) {
				Logger.Warn($"County {county} was not given to anyone!");
			}
		}
	}

	private bool TryGiveCountyToMonarch(Title county, Country irCountry) {
		var ck3Country = irCountry.CK3Title;
		if (ck3Country is null) {
			Logger.Warn($"{irCountry.Name} has no CK3 title!"); // should not happen
			return false;
		}

		GiveCountyToMonarch(county, ck3Country);
		RevokeBaroniesFromCountyGivenToImperatorCharacter(county);
		return true;
	}

	// Decides if governor assignment should be skipped due to capital duchy constraints.
	private static bool ShouldSkipGovernorDueToCapitalDuchy(Title county, Title ck3Country) {
		var ck3CapitalCounty = ck3Country.CapitalCounty;
		if (ck3CapitalCounty is null) {
			var logLevel = ck3Country.ImperatorCountry?.PlayerCountry == true ? Level.Warn : Level.Debug;
			Logger.Log(logLevel, $"{ck3Country} has no capital county!");
			return true;
		}
		// If title belongs to country ruler's capital's de jure duchy, it needs to be directly held by the ruler.
		var countryCapitalDuchy = ck3CapitalCounty.DeJureLiege;
		var deJureDuchyOfCounty = county.DeJureLiege;
		return countryCapitalDuchy is not null && deJureDuchyOfCounty is not null && countryCapitalDuchy.Id == deJureDuchyOfCounty.Id;
	}

	private bool TryGiveCountyToGovernor(Title county,
		Imperator.Provinces.Province irProvince,
		Country irCountry,
		IReadOnlyDictionary<(ulong CountryId, string RegionId), Governorship> governorshipsByCountryAndRegion,
		Imperator.Provinces.ProvinceCollection irProvinces,
		FrozenSet<Governorship> countyLevelGovernorshipsSet,
		Imperator.Characters.CharacterCollection irCharacters) {
		var ck3Country = irCountry.CK3Title;
		if (ck3Country is null) {
			Logger.Warn($"{irCountry.Name} has no CK3 title!"); // should not happen
			return false;
		}

		var parentRegionName = imperatorRegionMapper.GetParentRegionName(irProvince.Id);
		if (parentRegionName is null || !governorshipsByCountryAndRegion.TryGetValue((irCountry.Id, parentRegionName), out var governorship)) {
			// We have no matching governorship.
			return false;
		}

		if (ShouldSkipGovernorDueToCapitalDuchy(county, ck3Country)) {
			return false;
		}

		// give county to governor
		var ck3GovernorshipId = tagTitleMapper.GetTitleForGovernorship(governorship, LandedTitles, irProvinces, Provinces, imperatorRegionMapper, provinceMapper);
		if (ck3GovernorshipId is null) {
			Logger.Warn($"{nameof(ck3GovernorshipId)} is null for {ck3Country} {governorship.Region.Id}!");
			return false;
		}

		if (countyLevelGovernorshipsSet.Contains(governorship)) {
			GiveCountyToCountyLevelGovernor(county, governorship, ck3Country, irCharacters);
		} else {
			GiveCountyToGovernor(county, ck3GovernorshipId);
		}
		RevokeBaroniesFromCountyGivenToImperatorCharacter(county);
		return true;
	}

	private void GiveCountyToMonarch(Title county, Title ck3Country) {
		var date = ck3Country.GetDateOfLastHolderChange();
		var holderId = ck3Country.GetHolderId(date);

		if (Characters.TryGetValue(holderId, out var holder)) {
			county.ClearHolderSpecificHistory();
			county.SetHolder(holder, date);
		} else {
			Logger.Warn($"Holder {holderId} of county {county} doesn't exist!");
		}
		county.SetDeFactoLiege(null, date);
	}

	private void GiveCountyToGovernor(Title county, string ck3GovernorshipId) {
		var ck3Governorship = LandedTitles[ck3GovernorshipId];
		var holderChangeDate = ck3Governorship.GetDateOfLastHolderChange();
		var holderId = ck3Governorship.GetHolderId(holderChangeDate);
		if (Characters.TryGetValue(holderId, out var governor)) {
			county.ClearHolderSpecificHistory();
			county.SetHolder(governor, holderChangeDate);
		} else {
			Logger.Warn($"Holder {holderId} of county {county} doesn't exist!");
		}
		county.SetDeFactoLiege(null, holderChangeDate);
	}

	private static void GiveCountyToCountyLevelGovernor(Title county,
		Governorship governorship,
		Title ck3Country,
		Imperator.Characters.CharacterCollection impCharacters) {
		var holderChangeDate = governorship.StartDate;
		var impGovernor = impCharacters[governorship.CharacterId];
		var governor = impGovernor.CK3Character;

		county.ClearHolderSpecificHistory();
		county.SetHolder(governor, holderChangeDate);
		county.SetDeFactoLiege(ck3Country, holderChangeDate);
	}

	private bool TryGiveCountyToCountyLevelRuler(Title county,
		Country irCountry,
		IReadOnlyDictionary<ulong, KeyValuePair<Country, Dependency?>> countyLevelCountriesByCountryId,
		CountryCollection irCountries) {
		if (!countyLevelCountriesByCountryId.TryGetValue(irCountry.Id, out var matchingCountyLevelRuler)) {
			return false;
		}
		var dependency = matchingCountyLevelRuler.Value;

		// Give county to ruler.
		var ck3Ruler = irCountry.Monarch?.CK3Character;
		county.ClearHolderSpecificHistory();
		var ruleStartDate = irCountry.RulerTerms.OrderBy(t => t.StartDate).Last().StartDate;
		county.SetHolder(ck3Ruler, ruleStartDate);
		if (dependency is not null) {
			var irOverlord = dependency.OverlordId;
			var ck3Overlord = irCountries[irOverlord].CK3Title;
			county.SetDeFactoLiege(ck3Overlord, dependency.StartDate);
		} else {
			county.SetDeFactoLiege(null, ruleStartDate);
		}
		RevokeBaroniesFromCountyGivenToImperatorCharacter(county);
		return true;
	}

	private static void RevokeBaroniesFromCountyGivenToImperatorCharacter(Title county) {
		foreach (var barony in county.DeJureVassals) {
			// Skip the county capital barony.
			if (barony.ProvinceId == county.CapitalBaronyProvinceId) {
				continue;
			}
			
			// Clear the barony holders history.
			barony.ClearHolderSpecificHistory();
		}
	}

	private void HandleIcelandAndFaroeIslands(Imperator.World irWorld, Configuration config) {
		Logger.Info("Handling Iceland and Faroe Islands...");
		Date bookmarkDate = config.CK3BookmarkDate;
		var year = bookmarkDate.Year;

		var faiths = Religions.Faiths.ToArray();

		var titleIdsToHandle = GetIcelandAndFaroeTitleIdsToHandle(config);
		var (generateHermits, faithCandidates, cultureId, namePool) = DetermineIcelandAndFaroeRulerSetup(year, bookmarkDate);

		if (generateHermits) {
			var faithId = faithCandidates.First(c => faiths.Any(f => f.Id == c));
			foreach (var titleId in titleIdsToHandle) {
				if (!LandedTitles.TryGetValue(titleId, out var title)) {
					Logger.Warn($"Title {titleId} not found!");
					continue;
				}

				GenerateHermitForTitle(title, namePool, bookmarkDate, faithId, cultureId, config);
			}
		}

		Logger.IncrementProgress();
	}

	private static OrderedSet<string> GetIcelandAndFaroeTitleIdsToHandle(Configuration config) {
		if (config.FallenEagleEnabled) {
			return ["c_faereyar"];
		}
		if (config.TerraIndomitaDetected) {
			return ["d_iceland"];
		}
		return ["d_iceland", "c_faereyar"];
	}

	private (bool generateHermits, IEnumerable<string> faithCandidates, string cultureId, Queue<string> namePool) DetermineIcelandAndFaroeRulerSetup(int year, Date bookmarkDate) {
		switch (year) {
			case <= 300:
				UsePaganRulersForIcelandAndFaroeIslands(out var paganFaithCandidates, out var paganCultureId, out var paganNamePool);
				return (true, paganFaithCandidates, paganCultureId, paganNamePool);
			case < 874:
				return DeterminePaparRulerSetup(bookmarkDate);
			default:
				Logger.Info("Keeping Iceland and Faroe Islands as is in history...");
				return (false, [], "irish", new Queue<string>());
		}
	}

	private (bool generateHermits, IEnumerable<string> faithCandidates, string cultureId, Queue<string> namePool) DeterminePaparRulerSetup(Date bookmarkDate) {
		IEnumerable<string> faithCandidates = new OrderedSet<string> { "insular_celtic", "catholic", "orthodox", "chalcedonian", "nicene" };
		var christianFaiths = Religions.TryGetValue("christianity_religion", out var christianityReligion) ? christianityReligion.Faiths : [];
		const string defaultCultureId = "irish";
		var sourceProvince = FindPaparSourceProvince(bookmarkDate, christianFaiths);
		if (sourceProvince is null) {
			UsePaganRulersForIcelandAndFaroeIslands(out var paganFaithCandidates, out var paganCultureId, out var paganNamePool);
			return (true, paganFaithCandidates, paganCultureId, paganNamePool);
		}

		var sourceFaithId = sourceProvince.GetFaithId(bookmarkDate)!;
		faithCandidates = faithCandidates.Prepend(sourceFaithId);
		Logger.Info("Giving Iceland and Faroe Islands to Papar...");
		return (true, faithCandidates, sourceProvince.GetCultureId(bookmarkDate) ?? defaultCultureId, new Queue<string>(["Canann", "Petair", "Fergus"]));
	}

	private Province? FindPaparSourceProvince(Date bookmarkDate, IdObjectCollection<string, Faith> christianFaiths) {
		foreach (var potentialCultureId in new[] { "irish", "gaelic" }) {
			var sourceProvince = Provinces.FirstOrDefault(p => p.GetCultureId(bookmarkDate) == potentialCultureId && ProvinceHasChristianFaith(p, bookmarkDate, christianFaiths));
			if (sourceProvince is not null) {
				return sourceProvince;
			}
		}

		return Provinces.FirstOrDefault(p =>
			(CK3RegionMapper.ProvinceIsInRegion(p.Id, "custom_ireland") || CK3RegionMapper.ProvinceIsInRegion(p.Id, "custom_scotland")) &&
			ProvinceHasChristianFaith(p, bookmarkDate, christianFaiths));
	}

	private static bool ProvinceHasChristianFaith(Province province, Date bookmarkDate, IdObjectCollection<string, Faith> christianFaiths) {
		var faithId = province.GetFaithId(bookmarkDate);
		return faithId is not null && christianFaiths.ContainsKey(faithId);
	}

	private void UsePaganRulersForIcelandAndFaroeIslands(out IEnumerable<string> faithCandidates, out string cultureId, out Queue<string> namePool) {
		Logger.Info("Giving Iceland and Faroe Islands to pagan Gaels...");
		faithCandidates = new OrderedSet<string> { "gaelic_paganism", "celtic_pagan", "briton_paganism", "pagan" };
		cultureId = "gaelic";
		// ReSharper disable once StringLiteralTypo
		namePool = new Queue<string>(["A_engus", "Domnall", "Rechtabra"]);
	}

	private void GenerateHermitForTitle(Title title, Queue<string> namePool, Date bookmarkDate, string faithId, string cultureId, Configuration config) {
		Logger.Debug($"Generating hermit for {title.Id}...");

		var hermit = new Character($"IRToCK3_{title.Id}_hermit", namePool.Dequeue(), bookmarkDate.ChangeByYears(-50), Characters);
		hermit.SetFaithId(faithId, date: null);
		hermit.SetCultureId(cultureId, date: null);
		hermit.History.AddFieldValue(date: null, "traits", "trait", "chaste");
		hermit.History.AddFieldValue(date: null, "traits", "trait", "celibate");
		hermit.History.AddFieldValue(date: null, "traits", "trait", "devoted");
		var eremiteEffect = new StringOfItem("{ set_variable = IRToCK3_eremite_flag }");
		hermit.History.AddFieldValue(config.CK3BookmarkDate, "effects", "effect", eremiteEffect);
		Characters.AddOrReplace(hermit);

		title.SetHolder(hermit, bookmarkDate);
		title.SetGovernment("eremitic_government", bookmarkDate);

		OrderedSet<Title> countiesToHandle = [..title.GetDeJureVassalsAndBelow(rankFilter: "c").Values];
		if (title.Rank == TitleRank.county) {
			countiesToHandle.Add(title);
		}
		foreach (var county in countiesToHandle) {
			county.SetHolder(hermit, bookmarkDate);
			county.SetDevelopmentLevel(0, bookmarkDate);
			foreach (var provinceId in county.CountyProvinceIds) {
				if (!Provinces.TryGetValue(provinceId, out var province)) {
					Logger.Warn($"Province {provinceId} not found for county {county.Id}!");
					continue;
				}
				
				province.History.RemoveHistoryPastDate("1.1.1");
				province.SetFaithId(faithId, date: null);
				province.SetCultureId(cultureId, date: null);
				province.SetBuildings(new List<string>(), date: null);
				province.History.Fields["holding"].RemoveAllEntries();
			}
		}
	}

	/// <summary>
	/// It makes no sense to have Islam on the map before the rise of Islam.
	/// This method removes it.
	/// </summary>
	private void RemoveIslam(Configuration config) {
		Logger.Info("Removing Islam from the map...");
		var date = config.CK3BookmarkDate;

		if (!Religions.TryGetValue("islam_religion", out var islam)) {
			Logger.Debug("islam_religion not found in religions.");
			return;
		}

		var muslimFaiths = islam.Faiths;
		var muslimProvinces = Provinces
			.Where(p => p.GetFaithId(date) is string faithId && muslimFaiths.ContainsKey(faithId))
			.ToHashSet();

		var regionToNewFaithMap = new List<KeyValuePair<string, string>> {
			// Africa
			new("world_africa_north", "berber_pagan"),
			new("world_africa_west", "berber_pagan"),
			new("world_africa_east", "waaqism_pagan"),
			new("world_africa_sahara", "berber_pagan"),
			new("world_africa", "berber_pagan"),
			// Rest of the world
			new("world_middle_east", "arabic_pagan"),
		}.Where(kvp => Religions.GetFaith(kvp.Value) is not null);

		foreach (var province in muslimProvinces.ToArray()) {
			foreach (var (regionId, faithId) in regionToNewFaithMap) {
				if (!CK3RegionMapper.ProvinceIsInRegion(province.Id, regionId)) {
					continue;
				}

				province.SetFaithIdAndOverrideExistingEntries(faithId);
				muslimProvinces.Remove(province);
				break;
			}
		}
		
		UseNeighborProvincesToConvertProvincesOfReligion(muslimProvinces, date);
		UseClosestProvincesToConvertProvincesOfReligion(muslimProvinces, date);
		UseFallbackNonMuslimFaithToRemoveIslam(muslimProvinces, muslimFaiths);

		// Log warning if there are still muslim provinces left.
		if (muslimProvinces.Count > 0) {
			Logger.Warn($"{muslimProvinces.Count} muslim provinces left after removing Islam: " +
			            $"{string.Join(", ", muslimProvinces.Select(p => p.Id))}");
		}
	}

	private void UseFallbackNonMuslimFaithToRemoveIslam(HashSet<Province> muslimProvinces, IdObjectCollection<string, Faith> muslimFaiths) {
		if (muslimProvinces.Count == 0) {
			return;
		}

		var fallbackFaith = Religions.Faiths.Except(muslimFaiths).FirstOrDefault();
		if (fallbackFaith is not null) {
			foreach (var province in muslimProvinces.ToArray()) {
				Logger.Debug($"Using fallback faith \"{fallbackFaith.Id}\" for province {province.Id}");
				province.SetFaithIdAndOverrideExistingEntries(fallbackFaith.Id);
				muslimProvinces.Remove(province);
			}
		}
	}

	private void UseClosestProvincesToConvertProvincesOfReligion(HashSet<Province> provincesOfReligion, Date date) {
		if (provincesOfReligion.Count == 0) {
			return;
		}

		var provincesWithValidFaith = Provinces
			.Where(p => !provincesOfReligion.Contains(p) && p.GetFaithId(date) is not null && !MapData.IsImpassable(p.Id))
			.ToArray();
		foreach (var province in provincesOfReligion.ToArray()) {
			Province? closestValidProvince = null;
			double shortestDistance = double.MaxValue;
			foreach (var candidateProvince in provincesWithValidFaith) {
				var distance = MapData.GetDistanceBetweenProvinces(province.Id, candidateProvince.Id);
				if (distance == 0 || distance >= shortestDistance) {
					continue;
				}

				shortestDistance = distance;
				closestValidProvince = candidateProvince;
			}
			if (closestValidProvince is null) {
				continue;
			}

			var faithId = closestValidProvince.GetFaithId(date)!;
			Logger.Debug($"Using faith \"{faithId}\" of closest province for province {province.Id}");
			province.SetFaithIdAndOverrideExistingEntries(faithId);
			provincesOfReligion.Remove(province);
		}
	}

	private void UseNeighborProvincesToConvertProvincesOfReligion(HashSet<Province> provincesOfReligion, Date date) {
		foreach (var province in provincesOfReligion.ToArray()) {
			var neighborIds = MapData.GetNeighborProvinceIds(province.Id);
			if (neighborIds.Count == 0) {
				continue;
			}

			string? neighborFaithId = null;
			foreach (var neighborId in neighborIds) {
				if (!Provinces.TryGetValue(neighborId, out var neighborProvince)) {
					continue;
				}

				if (provincesOfReligion.Contains(neighborProvince)) {
					continue;
				}

				neighborFaithId = neighborProvince.GetFaithId(date);
				if (neighborFaithId is not null) {
					break;
				}
			}
			if (neighborFaithId is null) {
				continue;
			}

			Logger.Debug($"Using neighbor's faith \"{neighborFaithId}\" for province {province.Id}.");
			province.SetFaithIdAndOverrideExistingEntries(neighborFaithId);
			provincesOfReligion.Remove(province);
		}
	}
	private void RemoveChristianity(Religion christianity, Date ck3BookmarkDate) {
		Logger.Info("Removing Christianity from the map...");

		var christianFaiths = christianity.Faiths;
		var christianProvinces = Provinces
			.Where(p => p.GetFaithId(ck3BookmarkDate) is string faithId && christianFaiths.ContainsKey(faithId))
			.ToHashSet();

		UseNeighborProvincesToConvertProvincesOfReligion(christianProvinces, ck3BookmarkDate);
		UseClosestProvincesToConvertProvincesOfReligion(christianProvinces, ck3BookmarkDate);

		// Log warning if there are still Christian provinces left.
		if (christianProvinces.Count > 0) {
			Logger.Warn($"{christianProvinces.Count} Christian provinces left after removing Christianity: " +
			            $"{string.Join(", ", christianProvinces.Select(p => p.Id))}");
		}
	}

	private void ReplaceMiaphysiteChristianityWithNiceneChristianity(Religion christianity, Date ck3BookmarkDate) {
		Logger.Info("Replacing Miaphysite Christianity with Nicene Christianity...");

		HashSet<string> miaphysiteFaithIds = ["coptic", "armenian_apostolic"];
		var miaphysiteProvinces = Provinces
			.Where(p => p.GetFaithId(ck3BookmarkDate) is string faithId && miaphysiteFaithIds.Contains(faithId))
			.ToArray();

		string[] replacementFaithIds = ["nicene", "chalcedonian", "orthodox", "catholic"];
		var bestReplacementFaithId = replacementFaithIds
			.Select(id => christianity.Faiths.TryGetValue(id, out var faith) ? faith : null)
			.FirstOrDefault(f => f is not null)?.Id ?? christianity.Faiths.First().Id;

		foreach (var province in miaphysiteProvinces) {
			province.SetFaithIdAndOverrideExistingEntries(bestReplacementFaithId);
		}
	}

	private void ReplaceNestorianChristianityWithNiceneChristianity(Religion christianity, Date ck3BookmarkDate) {
		Logger.Info("Replacing Nestorian Christianity with Nicene Christianity...");

		HashSet<string> nestorianFaithIds = ["nestorian", "indian_catholic"]; // indian_catholic is from RoA
		var nestorianProvinces = Provinces
			.Where(p => p.GetFaithId(ck3BookmarkDate) is string faithId && nestorianFaithIds.Contains(faithId))
			.ToArray();

		string[] replacementFaithIds = ["nicene", "chalcedonian", "orthodox", "catholic"];
		var bestReplacementFaithId = replacementFaithIds
			.Select(id => christianity.Faiths.TryGetValue(id, out var faith) ? faith : null)
			.FirstOrDefault(f => f is not null)?.Id ?? christianity.Faiths.First().Id;

		foreach (var province in nestorianProvinces) {
			province.SetFaithIdAndOverrideExistingEntries(bestReplacementFaithId);
		}
	}

	private void GenerateFillerHoldersForUnownedLands(Imperator.Provinces.ProvinceCollection irProvinces, CultureCollection cultures, Configuration config) {
		Logger.Info("Generating filler holders for unowned lands...");
		var date = config.CK3BookmarkDate;
		var unheldCounties = GetUnheldCounties(irProvinces, date);

		var duchyIdToHolderDict = new Dictionary<string, Character>();

		foreach (var county in unheldCounties) {
			if (TryReuseExistingFillerDuke(county, config, duchyIdToHolderDict, date)) {
				continue;
			}

			var candidateProvinces = GetCandidateProvincesForFillerHolder(county);
			var pseudoRandomSeed = GetFillerHolderSeed(county, candidateProvinces);
			var culture = DetermineFillerHolderCulture(county, candidateProvinces, cultures, date);
			var faithId = DetermineFillerHolderFaith(county, candidateProvinces, date);
			var holder = CreateFillerHolder(county, culture, faithId, date, pseudoRandomSeed);
			AssignFillerHolder(county, holder, config.FillerDukes, duchyIdToHolderDict, date);
		}
	}

	private List<Title> GetUnheldCounties(Imperator.Provinces.ProvinceCollection irProvinces, Date date) {
		List<Title> unheldCounties = [];
		foreach (var county in LandedTitles.Counties) {
			if (county.NobleFamily == true) {
				continue;
			}

			var irProvIds = county.CountyProvinceIds.SelectMany(id => provinceMapper.GetImperatorProvinceNumbers(id)).ToArray();
			if (irProvIds.Length > 0 && irProvIds.All(p => !irProvinces.TryGetValue(p, out var irProv) || irProv.OwnerCountry is null)) {
				Logger.Debug($"Adding {county.Id} to unheld counties because all its provinces are mapped to I:R wastelands.");
				unheldCounties.Add(county);
				continue;
			}

			var holderId = county.GetHolderId(date);
			if (holderId == "0") {
				unheldCounties.Add(county);
			} else if (Characters.TryGetValue(holderId, out var holder) && holder.DeathDate is not null && holder.DeathDate <= date) {
				Logger.Debug($"Adding {county.Id} to unheld counties because holder {holderId} is dead.");
				unheldCounties.Add(county);
			}
		}
		return unheldCounties;
	}

	private bool TryReuseExistingFillerDuke(Title county, Configuration config, Dictionary<string, Character> duchyIdToHolderDict, Date date) {
		if (!config.FillerDukes) {
			return false;
		}

		var duchy = county.DeJureLiege;
		if (duchy is null || duchy.Rank != TitleRank.duchy || !duchyIdToHolderDict.TryGetValue(duchy.Id, out var duchyHolder)) {
			return false;
		}

		county.SetHolder(duchyHolder, date);
		return true;
	}

	private OrderedSet<Province> GetCandidateProvincesForFillerHolder(Title county) {
		var candidateProvinces = new OrderedSet<Province>();
		if (county.CapitalBaronyProvinceId is not null && Provinces.TryGetValue(county.CapitalBaronyProvinceId.Value, out var capitalProvince)) {
			candidateProvinces.Add(capitalProvince);
		}

		var allCountyProvinces = county.CountyProvinceIds
			.Select(id => Provinces.TryGetValue(id, out var province) ? province : null)
			.Where(p => p is not null)
			.Select(p => p!);
		candidateProvinces.UnionWith(allCountyProvinces);
		return candidateProvinces;
	}

	private static int GetFillerHolderSeed(Title county, OrderedSet<Province> candidateProvinces) {
		if (candidateProvinces.Count != 0) {
			return (int)candidateProvinces.First().Id;
		}

		return county.Id.Aggregate(0, (current, c) => current + c);
	}

	private Culture DetermineFillerHolderCulture(Title county, OrderedSet<Province> candidateProvinces, CultureCollection cultures, Date date) {
		var culture = candidateProvinces.Select(p => p.GetCulture(date, cultures)).FirstOrDefault(c => c is not null);
		if (culture is not null) {
			return culture;
		}

		Logger.Debug($"Trying to use de jure duchy for culture of holder for {county.Id}...");
		var deJureDuchy = county.DeJureLiege;
		if (deJureDuchy is not null) {
			culture = Provinces.Where(p => deJureDuchy.DuchyContainsProvince(p.Id)).Select(p => p.GetCulture(date, cultures)).FirstOrDefault(c => c is not null);
		}
		if (culture is null && deJureDuchy?.DeJureLiege is not null) {
			Logger.Debug($"Trying to use de jure kingdom for culture of holder for {county.Id}...");
			var deJureKingdom = deJureDuchy.DeJureLiege;
			culture = Provinces.Where(p => deJureKingdom.KingdomContainsProvince(p.Id)).Select(p => p.GetCulture(date, cultures)).FirstOrDefault(c => c is not null);
		}
		if (culture is not null) {
			return culture;
		}

		Logger.Warn($"Found no fitting culture for generated holder of {county.Id}, using first culture from database!");
		return cultures.First();
	}

	private string DetermineFillerHolderFaith(Title county, OrderedSet<Province> candidateProvinces, Date date) {
		var faithId = candidateProvinces.Select(p => p.GetFaithId(date)).FirstOrDefault(f => f is not null);
		if (faithId is not null) {
			return faithId;
		}

		Logger.Debug($"Trying to use de jure duchy for faith of holder for {county.Id}...");
		var deJureDuchy = county.DeJureLiege;
		if (deJureDuchy is not null) {
			faithId = Provinces.Where(p => deJureDuchy.DuchyContainsProvince(p.Id)).Select(p => p.GetFaithId(date)).FirstOrDefault(f => f is not null);
		}
		if (faithId is null && deJureDuchy?.DeJureLiege is not null) {
			Logger.Debug($"Trying to use de jure kingdom for faith of holder for {county.Id}...");
			var deJureKingdom = deJureDuchy.DeJureLiege;
			faithId = Provinces.Where(p => deJureKingdom.KingdomContainsProvince(p.Id)).Select(p => p.GetFaithId(date)).FirstOrDefault(f => f is not null);
		}
		if (faithId is not null) {
			return faithId;
		}

		Logger.Warn($"Found no fitting faith for generated holder of {county.Id}, using first faith from database!");
		return Religions.Faiths.First().Id;
	}

	private Character CreateFillerHolder(Title county, Culture culture, string faithId, Date date, int pseudoRandomSeed) {
		var (female, name) = GetFillerHolderName(culture, pseudoRandomSeed);
		int age = 18 + (pseudoRandomSeed % 60);
		var holder = new Character($"IRToCK3_{county.Id}_holder", name, date, Characters) {
			FromImperator = true,
			Female = female,
			BirthDate = date.ChangeByYears(-age)
		};
		holder.SetFaithId(faithId, null);
		holder.SetCultureId(culture.Id, null);
		holder.History.AddFieldValue(holder.BirthDate, "effects", "effect", "{ set_variable = irtock3_uncolonized_filler }");
		Characters.AddOrReplace(holder);
		return holder;
	}

	private static (bool female, string name) GetFillerHolderName(Culture culture, int pseudoRandomSeed) {
		var maleNames = culture.MaleNames.ToImmutableList();
		if (maleNames.Count > 0) {
			return (false, maleNames[pseudoRandomSeed % maleNames.Count]);
		}

		var femaleNames = culture.FemaleNames.ToImmutableList();
		return (true, femaleNames[pseudoRandomSeed % femaleNames.Count]);
	}

	private void AssignFillerHolder(Title county, Character holder, bool fillerDukesEnabled, Dictionary<string, Character> duchyIdToHolderDict, Date date) {
		var government = GetGeneratedHolderGovernment(county, date);
		county.SetHolder(holder, date);
		if (fillerDukesEnabled) {
			var duchy = county.DeJureLiege;
			if (duchy is null || duchy.Rank != TitleRank.duchy) {
				return;
			}

			duchy.SetHolder(holder, date);
			duchy.SetGovernment(government, date);
			duchy.SetDeFactoLiege(newLiege: null, date);
			duchyIdToHolderDict[duchy.Id] = holder;
		} else {
			county.SetGovernment(government, date);
		}
		county.SetDeFactoLiege(newLiege: null, date);
	}

	private FrozenSet<string> GetGeneratedHolderGovernmentTypes(Title county, Date date) {
		return county.CountyProvinceIds
			.Select(id => Provinces.TryGetValue(id, out var province) ? province : null)
			.Where(p => p is not null)
			.Select(p => p!.GetHoldingType(date))
			.Where(t => t is not null)
			.Select(t => t!)
			.ToFrozenSet();
	}

	private string GetGeneratedHolderGovernment(Title county, Date date) {
		var countyHoldingTypes = GetGeneratedHolderGovernmentTypes(county, date);
		return countyHoldingTypes.Contains("castle_holding") ? "feudal_government" : "tribal_government";
	}

	private void DetermineCK3Dlcs(Configuration config) {
		var dlcFolderPath = Path.Join(config.CK3Path, "game/dlc");
		if (!Directory.Exists(dlcFolderPath)) {
			Logger.Warn($"CK3 DLC folder not found: {dlcFolderPath}");
			return;
		}

		System.Collections.Generic.OrderedDictionary<string, string> dlcFileToDlcFlagDict = new() {
			{"dlc001.dlc", "garments_of_hre"},
			{"dlc002.dlc", "fashion_of_abbasid_court"},
			{"dlc003.dlc", "northern_lords"},
			{"dlc004.dlc", "royal_court"},
			{"dlc005.dlc", "fate_of_iberia"},
			{"dlc006.dlc", "friends_and_foes"},
			{"dlc007.dlc", "tours_and_tournaments"},
			{"dlc008.dlc", "elegance_of_empire"},
			{"dlc009.dlc", "wards_and_wardens"},
			{"dlc010.dlc", "legacy_of_persia"},
			{"dlc011.dlc", "legends_of_the_dead"},
			{"dlc012.dlc", "north_african_attire"},
			{"dlc013.dlc", "couture_of_capets"},
			{"dlc014.dlc", "roads_to_power"},
			{"dlc015.dlc", "wandering_nobles"},
			{"dlc016.dlc", "west_slavic_attire"},
			{"dlc017.dlc", "medieval_monuments"},
			{"dlc018.dlc", "arctic_attire"},
			{"dlc019.dlc", "crowns_of_the_world"},
			{"dlc020.dlc", "khans_of_the_steppe"},
			{"dlc021.dlc", "coronations"},
			{"dlc022.dlc", "all_under_heaven"},
			{"dlc023.dlc", "high_medieval_warfare_attire"},
			{"dlc024.dlc", "holy_buildings"},
			{"dlc025.dlc", "north_pacific_attire"},
			{"dlc026.dlc", "east_asian_wonders"},
			{"dlc027.dlc", "celestial_court_attire"},
			{"dlc028.dlc", "symbols_of_authority"},
			{"dlc029.dlc", "songs_of_the_realm"},
		};
		
		var dlcFiles = Directory.GetFiles(dlcFolderPath, "*.dlc", SearchOption.AllDirectories);
		foreach (var dlcFile in dlcFiles) {
			var dlcFileName = Path.GetFileName(dlcFile);
			if (dlcFileToDlcFlagDict.TryGetValue(dlcFileName, out var dlcFlag)) {
				Logger.Info($"Found DLC: {dlcFlag}");
				enabledDlcFlags.Add(dlcFlag);
			} else {
				Logger.Warn($"Unknown DLC file: {dlcFileName}");
			}
		}
	}

	private readonly DeathReasonMapper deathReasonMapper = new();
	private readonly DefiniteFormMapper definiteFormMapper = new(Path.Combine("configurables", "definite_form_names.txt"));
	private readonly NicknameMapper nicknameMapper = new(Path.Combine("configurables", "nickname_map.txt"));
	private readonly ProvinceMapper provinceMapper = new();
	private readonly TagTitleMapper tagTitleMapper = new(
		tagTitleMappingsPath: Path.Combine("configurables", "title_map.txt"),
		governorshipTitleMappingsPath: Path.Combine("configurables", "governorMappings.txt"),
		rankMappingsPath: "configurables/country_rank_map.txt"
	);
	private readonly UnitTypeMapper unitTypeMapper = new("configurables/unit_types_map.txt");
	private readonly ImperatorRegionMapper imperatorRegionMapper;
	private readonly WarMapper warMapper = new("configurables/wargoal_mappings.txt");
}

