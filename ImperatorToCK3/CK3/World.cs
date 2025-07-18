﻿using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Diplomacy;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Legends;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Exceptions;
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
	internal CoaMapper CK3CoaMapper { get; private set; } = null!;
	private readonly List<string> enabledDlcFlags = [];

	/// <summary>
	/// Date based on I:R save date, but normalized for CK3 purposes.
	/// </summary>
	public Date CorrectedDate { get; }

	public World(Imperator.World impWorld, Configuration config, Thread? irCoaExtractThread) {
		Logger.Info("*** Hello CK3, let's get painting. ***");

		warMapper.DetectUnmappedWarGoals(impWorld.ModFS);
		
		DetermineCK3Dlcs(config);
		LoadAndDetectCK3Mods(config);

		// Initialize fields that depend on other fields.
		Religions = new ReligionCollection(LandedTitles);

		// Determine CK3 bookmark date.
		CorrectedDate = impWorld.EndDate.Year > 1 ? impWorld.EndDate : new Date(2, 1, 1);
		if (config.CK3BookmarkDate.Year == 0) { // bookmark date is not set
			config.CK3BookmarkDate = CorrectedDate;
			Logger.Info($"CK3 bookmark date set to: {config.CK3BookmarkDate}");
		} else if (CorrectedDate > config.CK3BookmarkDate) {
			Logger.Warn($"Corrected save can't be later than CK3 bookmark date, setting CK3 bookmark date to {CorrectedDate}!");
			config.CK3BookmarkDate = CorrectedDate;
		}
		
		// Recreate output mod folder.
		string outputModPath = Path.Join("output", config.OutputModName);
		WorldOutputter.ClearOutputModFolder(outputModPath);
		WorldOutputter.CreateModFolder(outputModPath);
		// This will also convert all Liquid templates into simple text files.
		WorldOutputter.CopyBlankModFilesToOutput(outputModPath, config.GetCK3ModFlags());
		
		// Include a fake mod pointing to blankMod in the output folder.
		LoadedMods.Add(new Mod("blankMod", outputModPath));
		ModFS = new ModFilesystem(Path.Combine(config.CK3Path, "game"), LoadedMods);

		var ck3Defines = new Defines();
		ck3Defines.LoadDefines(ModFS);
		
		ColorFactory ck3ColorFactory = new();
		// Now that we have the mod filesystem, we can initialize the localization database.
		Parallel.Invoke(
			() => LoadCorrectProvinceMappingsFile(impWorld, config), // Depends on loaded mods.
			() => {
				LocDB.LoadLocFromModFS(ModFS, config.GetActiveCK3ModFlags());
				Logger.IncrementProgress();
			},
			() => ScriptValues.LoadScriptValues(ModFS, ck3Defines),
			() => {
				NamedColors.LoadNamedColors("common/named_colors", ModFS);
				ck3ColorFactory.AddNamedColorDict(NamedColors);
			},
			() => {
				Logger.Info("Loading map data...");
				MapData = new MapData(ModFS);
			},
			() => CK3CoaMapper = new(ModFS),
			() => {
				// Modify some CK3 and mod files and put them in the output before we start outputting anything.
				FileTweaker.ModifyAndRemovePartsOfFiles(ModFS, outputModPath, config).Wait();
			}
		);
		
		System.Collections.Generic.OrderedDictionary<string, bool> ck3ModFlags = config.GetCK3ModFlags();
		
		Parallel.Invoke(
			() => { // depends on ck3ColorFactory and CulturalPillars
				// Load CK3 cultures from CK3 mod filesystem.
				Logger.Info("Loading cultural pillars...");
				CulturalPillars = new(ck3ColorFactory, ck3ModFlags);
				CulturalPillars.LoadPillars(ModFS, ck3ModFlags);
				Logger.Info("Loading converter cultural pillars...");
				CulturalPillars.LoadConverterPillars("configurables/cultural_pillars", ck3ModFlags);
				Cultures = new CultureCollection(ck3ColorFactory, CulturalPillars, ck3ModFlags);
				Cultures.LoadNameLists(ModFS);
				Cultures.LoadInnovationIds(ModFS);
				Cultures.LoadCultures(ModFS, config);
				Cultures.LoadConverterCultures("configurables/converter_cultures.txt", config);
				Cultures.WarnAboutCircularParents();
				Logger.IncrementProgress();
			},
			() => LoadMenAtArmsTypes(ModFS, ScriptValues), // depends on ScriptValues
			() => { // depends on LocDB and CK3CoaMapper
				// Load vanilla CK3 landed titles and their history
				LandedTitles.LoadTitles(ModFS, LocDB);
				
				if (config.StaticDeJure) {
					Logger.Info("Setting static de jure kingdoms and empires...");

					Title.LandedTitles overrideTitles = [];
					overrideTitles.LoadStaticTitles();
					LandedTitles.CarveTitles(overrideTitles);

					Logger.IncrementProgress();
				}
		
				LandedTitles.SetCoatsOfArms(CK3CoaMapper);
		
				LandedTitles.LoadHistory(config, ModFS);
				LandedTitles.LoadCulturalNamesFromConfigurables();
			}
		);
		
		// Load regions.
		ck3RegionMapper = new CK3RegionMapper(ModFS, LandedTitles);
		imperatorRegionMapper = impWorld.ImperatorRegionMapper;
		
		CultureMapper cultureMapper = null!;
		TraitMapper traitMapper = null!;
		DNAFactory dnaFactory = null!;
		Parallel.Invoke(
			() => { // depends on ck3ColorFactory and landed titles being loaded 
				// Load CK3 religions from game and blankMod.
				// Holy sites need to be loaded after landed titles.
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
				Religions.LoadConverterFaiths("configurables/converter_faiths.txt", ck3ColorFactory);
				Logger.Info("Loaded converter faiths.");
				Logger.IncrementProgress();
				Religions.RemoveChristianAndIslamicSyncretismFromAllFaiths();
				// Now that all the faiths are loaded, remove liege entries from the history of religious head titles.
				LandedTitles.RemoveLiegeEntriesFromReligiousHeadHistory(Religions);
				
				Religions.LoadReplaceableHolySites("configurables/replaceable_holy_sites.txt");
				Logger.Info("Loaded replaceable holy sites.");
			},
			
			() => cultureMapper = new CultureMapper(imperatorRegionMapper, ck3RegionMapper, Cultures),
			
			() => traitMapper = new("configurables/trait_map.txt", ModFS),
			
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
		
		var religionMapper = new ReligionMapper(Religions, imperatorRegionMapper, ck3RegionMapper);
		
		Parallel.Invoke(
			() => Cultures.ImportTechnology(impWorld.Countries, cultureMapper, provinceMapper, impWorld.InventionsDB, impWorld.LocDB, ck3ModFlags),
			
			() => { // depends on religionMapper
				// Check if all I:R religions have a base mapping.
				foreach (var irReligionId in impWorld.Religions.Select(r => r.Id)) {
					var baseMapping = religionMapper.Match(irReligionId, null, null, null, null, config);
					if (baseMapping is null) {
						string religionStr = "ID: " + irReligionId;
						var localizedName = impWorld.LocDB.GetLocBlockForKey(irReligionId)?["english"];
						if (localizedName is not null) {
							religionStr += $", name: {localizedName}";
						}
						Logger.Warn($"No base mapping found for I:R religion {religionStr}!");
					}
				}
			},
			() => { // depends on cultureMapper
				// Check if all I:R cultures have a base mapping.
				var irCultureIds = impWorld.CulturesDB.SelectMany(g => g.Select(c => c.Id));
				foreach (var irCultureId in irCultureIds) {
                	var baseMapping = cultureMapper.Match(irCultureId, null, null, null);
                	if (baseMapping is null) {
						string cultureStr = "ID: " + irCultureId;
						var localizedName = impWorld.LocDB.GetLocBlockForKey(irCultureId)?["english"];
						if (localizedName is not null) {
							cultureStr += $", name: {localizedName}";
						}
                		Logger.Warn($"No base mapping found for I:R culture {cultureStr}!");
                	}
                }
			},
			() => { // depends on TraitMapper and CK3 characters being loaded
				Characters.RemoveUndefinedTraits(traitMapper);
			}
		);
		
		Characters.ImportImperatorCharacters(
			impWorld,
			religionMapper,
			cultureMapper,
			Cultures,
			traitMapper,
			nicknameMapper,
			provinceMapper,
			deathReasonMapper,
			dnaFactory,
			LocDB,
			impWorld.EndDate,
			config
		);
		// Now that we have loaded all characters, we can mark some of them as non-removable.
		Characters.LoadCharacterIDsToPreserve(config.CK3BookmarkDate);
		ClearFeaturedCharactersDescriptions(config.CK3BookmarkDate);

		Dynasties.LoadCK3Dynasties(ModFS);
		// Now that we have loaded all dynasties from CK3, we can remove invalid dynasty IDs from character history.
		Characters.RemoveInvalidDynastiesFromHistory(Dynasties);
		Dynasties.ImportImperatorFamilies(impWorld, cultureMapper, impWorld.LocDB, LocDB, CorrectedDate);
		DynastyHouses.LoadCK3Houses(ModFS);
		
		// Load existing CK3 government IDs.
		Logger.Info("Loading CK3 government IDs...");
		var ck3GovernmentIds = new HashSet<string>();
		var governmentsParser = new Parser();
		governmentsParser.RegisterRegex(CommonRegexes.String, (reader, governmentId) => {
			ck3GovernmentIds.Add(governmentId);
			ParserHelpers.IgnoreItem(reader);
		});
		governmentsParser.ParseGameFolder("common/governments", ModFS, "txt", recursive: false, logFilePaths: true);
		Logger.IncrementProgress();
		GovernmentMapper governmentMapper = new(ck3GovernmentIds);
		Logger.IncrementProgress();
		
		// Before we can import Imperator countries and governorships, the I:R CoA extraction thread needs to finish.
		irCoaExtractThread?.Join();

		SuccessionLawMapper successionLawMapper = new("configurables/succession_law_map.liquid", ck3ModFlags);
		List<KeyValuePair<Country, Dependency?>> countyLevelCountries = [];
		LandedTitles.ImportImperatorCountries(
			impWorld.Countries,
			impWorld.Dependencies,
			tagTitleMapper,
			impWorld.LocDB,
			LocDB,
			provinceMapper,
			impWorld.CoaMapper,
			governmentMapper,
			successionLawMapper,
			definiteFormMapper,
			religionMapper,
			cultureMapper,
			nicknameMapper,
			Characters,
			CorrectedDate,
			config,
			countyLevelCountries,
			enabledDlcFlags
		);

		// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
		// Some of it will be overwritten, but not all.
		Provinces.ImportVanillaProvinces(ModFS, Religions, Cultures);

		// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
		Provinces.ImportImperatorProvinces(impWorld, MapData, LandedTitles, cultureMapper, religionMapper, provinceMapper, CorrectedDate, config);
		Provinces.LoadPrehistory();

		var countyLevelGovernorships = new List<Governorship>();
		LandedTitles.ImportImperatorGovernorships(
			impWorld,
			Provinces,
			tagTitleMapper,
			impWorld.LocDB,
			LocDB,
			config,
			provinceMapper,
			definiteFormMapper,
			imperatorRegionMapper,
			impWorld.CoaMapper,
			countyLevelGovernorships
		);
		
		// Give counties to rulers and governors.
		OverwriteCountiesHistory(impWorld.Countries, impWorld.JobsDB.Governorships, countyLevelCountries, countyLevelGovernorships, impWorld.Characters, impWorld.Provinces, CorrectedDate);
		// Import holding owners as barons and counts.
		LandedTitles.ImportImperatorHoldings(Provinces, impWorld.Characters, impWorld.EndDate);
		
		LandedTitles.ImportDevelopmentFromImperator(Provinces, CorrectedDate, config.ImperatorCivilizationWorth);
		LandedTitles.RemoveInvalidLandlessTitles(config.CK3BookmarkDate);
		
		// Apply region-specific tweaks.
		HandleIcelandAndFaroeIslands(impWorld, config);
		
		// Check if any muslim religion exists in Imperator. Otherwise, remove Islam from the entire CK3 map.
		var possibleMuslimReligionNames = new List<string> { "muslim", "islam", "sunni", "shiite" };
		var muslimReligionExists = impWorld.Religions
			.Any(r => possibleMuslimReligionNames.Contains(r.Id.ToLowerInvariant()));
		if (muslimReligionExists) {
			Logger.Info("Found muslim religion in Imperator save, keeping Islam in CK3.");
		} else {
			RemoveIslam(config);
		}
		Logger.IncrementProgress();
		
		// Now that Islam has been handled, we can generate filler holders without the risk of making them Muslim.
		GenerateFillerHoldersForUnownedLands(Cultures, config);
		Logger.IncrementProgress();
		if (!config.StaticDeJure) {
			LandedTitles.SetDeJureKingdomsAndEmpires(config.CK3BookmarkDate, Cultures, Characters, MapData, LocDB);
		}
		
		Dynasties.SetCoasForRulingDynasties(LandedTitles, config.CK3BookmarkDate);
		
		Characters.RemoveEmployerIdFromLandedCharacters(LandedTitles, CorrectedDate);
		Characters.PurgeUnneededCharacters(LandedTitles, Dynasties, DynastyHouses, config.CK3BookmarkDate);
		// We could convert Imperator character DNA while importing the characters.
		// But that'd be wasteful, because some of them are purged. So, we do it now.
		Characters.ConvertImperatorCharacterDNA(dnaFactory);
		
		// If there's a gap between the I:R save date and the CK3 bookmark date,
		// generate successors for old I:R characters instead of making them live for centuries.
		if (config.CK3BookmarkDate.DiffInYears(impWorld.EndDate) > 1) {
			Characters.GenerateSuccessorsForOldCharacters(LandedTitles, Cultures, impWorld.EndDate, config.CK3BookmarkDate, impWorld.RandomSeed);
		}

		// Gold needs to be distributed after characters' successors are generated.
		Characters.DistributeCountriesGold(LandedTitles, config);
		Characters.ImportLegions(LandedTitles, impWorld.Units, impWorld.Characters, CorrectedDate, unitTypeMapper, MenAtArmsTypes, provinceMapper, LocDB, config);
		
		// After the purging of unneeded characters, we should clean up the title history.
		LandedTitles.CleanUpHistory(Characters, config.CK3BookmarkDate);
		
		// Now that the title history is basically done, convert officials as council members and courtiers.
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

			() => {
				Diplomacy.ImportImperatorLeagues(impWorld.DefensiveLeagues, impWorld.Countries);
			}
		);
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
		modLoader.LoadMods(Directory.GetParent(config.CK3ModsPath)!.FullName, incomingCK3Mods);
		
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
		bool irHasTI = irWorld.TerraIndomitaDetected;
		
		bool ck3HasRajasOfAsia = config.RajasOfAsiaEnabled;
		bool ck3HasAEP = config.AsiaExpansionProjectEnabled;

		string mappingsToUse;
		if (irHasTI && ck3HasRajasOfAsia) {
			mappingsToUse = "terra_indomita_to_rajas_of_asia";
		} else if (irHasTI && ck3HasAEP) {
			mappingsToUse = "terra_indomita_to_aep";
		} else if (irWorld.InvictusDetected) {
			mappingsToUse = "imperator_invictus";
		} else {
			mappingsToUse = "imperator_vanilla";
			Logger.Warn("Support for non-Invictus Imperator saves is deprecated.");
		}
		
		Logger.Info($"Using province mappings: {mappingsToUse}");
		var mappingsPath = Path.Combine("configurables/province_mappings", mappingsToUse + ".txt");
		
		provinceMapper.LoadMappings(mappingsPath);
	}

	private void LoadMenAtArmsTypes(ModFilesystem ck3ModFS, ScriptValueCollection scriptValues) {
		Logger.Info("Loading men-at-arms types...");

		const string maaPath = "common/men_at_arms_types";
		var parser = new Parser();
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
		FrozenSet<Governorship> governorshipsSet = governorships.ToFrozenSet();
		FrozenSet<Governorship> countyLevelGovernorshipsSet = countyLevelGovernorships.ToFrozenSet();
		
		foreach (var county in LandedTitles.Where(t => t.Rank == TitleRank.county)) {
			if (county.CapitalBaronyProvinceId is null) {
				Logger.Warn($"County {county} has no capital barony province!");
				continue;
			}
			ulong capitalBaronyProvinceId = (ulong)county.CapitalBaronyProvinceId;
			if (capitalBaronyProvinceId == 0) {
				// title's capital province has an invalid ID (0 is not a valid province in CK3)
				Logger.Warn($"County {county} has invalid capital barony province!");
				continue;
			}

			if (!Provinces.ContainsKey(capitalBaronyProvinceId)) {
				Logger.Warn($"Capital barony province not found: {capitalBaronyProvinceId}");
				continue;
			}

			var ck3CapitalBaronyProvince = Provinces[capitalBaronyProvinceId];
			var irProvince = ck3CapitalBaronyProvince.PrimaryImperatorProvince;
			if (irProvince is null) { // probably outside of Imperator map
				continue;
			}

			var irCountry = irProvince.OwnerCountry;

			if (irCountry is null || irCountry.CountryType == CountryType.rebels) { // e.g. uncolonized Imperator province
				county.SetHolder(null, conversionDate);
				county.SetDeFactoLiege(null, conversionDate);
				RevokeBaroniesFromCountyGivenToImperatorCharacter(county);
			} else {
				bool given = TryGiveCountyToCountyLevelRuler(county, irCountry, countyLevelCountries, irCountries);
				if (!given) {
					given = TryGiveCountyToGovernor(county, irProvince, irCountry, governorshipsSet, irProvinces, countyLevelGovernorshipsSet, impCharacters);
				}
				if (!given) {
					given = TryGiveCountyToMonarch(county, irCountry);
				}
				if (!given) {
					Logger.Warn($"County {county} was not given to anyone!");
				}
			}
		}
		Logger.IncrementProgress();
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

	private bool TryGiveCountyToGovernor(Title county,
		Imperator.Provinces.Province irProvince,
		Country irCountry,
		FrozenSet<Governorship> governorshipsSet,
		Imperator.Provinces.ProvinceCollection irProvinces,
		FrozenSet<Governorship> countyLevelGovernorshipsSet,
		Imperator.Characters.CharacterCollection irCharacters) {
		var ck3Country = irCountry.CK3Title;
		if (ck3Country is null) {
			Logger.Warn($"{irCountry.Name} has no CK3 title!"); // should not happen
			return false;
		}
		var matchingGovernorships = new List<Governorship>(governorshipsSet.Where(g =>
			g.Country.Id == irCountry.Id &&
			g.Region.Id == imperatorRegionMapper.GetParentRegionName(irProvince.Id)
		));

		var ck3CapitalCounty = ck3Country.CapitalCounty;
		if (ck3CapitalCounty is null) {
			var logLevel = ck3Country.ImperatorCountry?.PlayerCountry == true ? Level.Warn : Level.Debug;
			Logger.Log(logLevel, $"{ck3Country} has no capital county!");
			return false;
		}
		// if title belongs to country ruler's capital's de jure duchy, it needs to be directly held by the ruler
		var countryCapitalDuchy = ck3CapitalCounty.DeJureLiege;
		var deJureDuchyOfCounty = county.DeJureLiege;
		if (countryCapitalDuchy is not null && deJureDuchyOfCounty is not null && countryCapitalDuchy.Id == deJureDuchyOfCounty.Id) {
			return false;
		}

		if (matchingGovernorships.Count == 0) {
			// we have no matching governorship
			return false;
		}

		// give county to governor
		var governorship = matchingGovernorships[0];
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
		List<KeyValuePair<Country, Dependency?>> countyLevelCountries,
		CountryCollection irCountries) {
		var matchingCountyLevelRulers = countyLevelCountries.Where(c => c.Key.Id == irCountry.Id).ToArray();
		if (matchingCountyLevelRulers.Length == 0) {
			return false;
		}
		var dependency = matchingCountyLevelRulers[0].Value;

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
		
		OrderedSet<string> titleIdsToHandle;
		if (config.FallenEagleEnabled) {
			// Iceland doesn't exist on TFE map.
			titleIdsToHandle = ["c_faereyar"];
		} else if (irWorld.TerraIndomitaDetected) {
			// The Faroe Islands are on the map in TI, so it should be handled normally instead of being given an Eremitic holder.
			titleIdsToHandle = ["d_iceland"];
		} else {
			titleIdsToHandle = ["d_iceland", "c_faereyar"];
		}

		bool generateHermits = true;
		IEnumerable<string> faithCandidates = new OrderedSet<string>();
		Queue<string> namePool = new();
		const string defaultCultureId = "irish";
		string cultureId = defaultCultureId;

		switch (year) {
			case <= 300:
				UsePaganRulers();
				break;
			case < 874:
				faithCandidates = new OrderedSet<string> { "insular_celtic", "catholic", "orthodox", "chalcedonian", "nicene" };
				var christianFaiths = Religions.TryGetValue("christianity_religion", out var christianityReligion) ? christianityReligion.Faiths : [];

				// If there is at least one Irish Christian county, give it to the Irish Papar.
				// If there is at least one Christian county of another Gaelic culture, give it to a character of this Gaelic culture.
				var cultureCandidates = new[] { "irish", "gaelic" };
				bool provinceFound = false;
				foreach (var potentialCultureId in cultureCandidates) {
					var cultureProvinces = Provinces.Where(p =>
						p.GetCultureId(bookmarkDate) == potentialCultureId);
					foreach (var cultureProvince in cultureProvinces) {
						var faithId = cultureProvince.GetFaithId(bookmarkDate);
						if (faithId is null || !christianFaiths.ContainsKey(faithId)) {
							continue;
						}
						provinceFound = true;
						cultureId = potentialCultureId;
						faithCandidates = faithCandidates.Prepend(faithId);
						break;
					}
					if (provinceFound) {
						break;
					}
				}
				if (!provinceFound) {
					// If all the Gaels are pagan but at least one province in Ireland or Scotland is Christian,
					// give the handled titles to a generated ruler of the same culture as that Christian province.
					var potentialSourceProvinces = Provinces.Where(p =>
						ck3RegionMapper.ProvinceIsInRegion(p.Id, "custom_ireland") || ck3RegionMapper.ProvinceIsInRegion(p.Id, "custom_scotland"));
					foreach (var potentialSourceProvince in potentialSourceProvinces) {
						var faithId = potentialSourceProvince.GetFaithId(bookmarkDate);
						if (faithId is null || !christianFaiths.ContainsKey(faithId)) {
							continue;
						}
						provinceFound = true;
						cultureId = potentialSourceProvince.GetCultureId(bookmarkDate) ?? defaultCultureId;
						faithCandidates = faithCandidates.Prepend(faithId);
						break;
					}
				}
				if (!provinceFound) {
					// Give up and create a pagan ruler.
					UsePaganRulers();
				} else {
					Logger.Info("Giving Iceland and Faroe Islands to Papar...");
					namePool = new Queue<string>(["Canann", "Petair", "Fergus"]);
				}
				break;
			default:
				Logger.Info("Keeping Iceland and Faroe Islands as is in history...");
				// Let CK3 use rulers from its history.
				generateHermits = false;
				break;
		}

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

		void UsePaganRulers() {
			Logger.Info("Giving Iceland and Faroe Islands to pagan Gaels...");
			faithCandidates = new OrderedSet<string> { "gaelic_paganism", "celtic_pagan", "briton_paganism", "pagan" };
			cultureId = "gaelic";
			// ReSharper disable once StringLiteralTypo
			namePool = new Queue<string>(["A_engus", "Domnall", "Rechtabra"]);
		}
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

		foreach (var (regionId, faithId) in regionToNewFaithMap) {
			var regionProvinces = muslimProvinces
				.Where(p => ck3RegionMapper.ProvinceIsInRegion(p.Id, regionId));
			foreach (var province in regionProvinces) {
				province.SetFaithIdAndOverrideExistingEntries(faithId);
				muslimProvinces.Remove(province);
			}
		}
		
		UseNeighborProvincesToRemoveIslam(muslimProvinces, date);
		UseClosestProvincesToRemoveIslam(muslimProvinces, date);
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

	private void UseClosestProvincesToRemoveIslam(HashSet<Province> muslimProvinces, Date date) {
		if (muslimProvinces.Count == 0) {
			return;
		}

		var provincesWithValidFaith = Provinces
			.Except(muslimProvinces)
			.Where(p => p.GetFaithId(date) is not null)
			.ToFrozenSet();
		foreach (var province in muslimProvinces) {
			var closestValidProvince = provincesWithValidFaith
				.Except(muslimProvinces)
				.Select(p => new {
					Province = p,
					Distance = MapData.GetDistanceBetweenProvinces(province.Id, p.Id),
				})
				.Where(x => x.Distance > 0)
				.MinBy(x => x.Distance)?.Province;
			if (closestValidProvince is null) {
				continue;
			}
				
			var faithId = closestValidProvince.GetFaithId(date)!;
			Logger.Debug($"Using faith \"{faithId}\" of closest province for province {province.Id}");
			province.SetFaithIdAndOverrideExistingEntries(faithId);
			muslimProvinces.Remove(province);
		}
	}

	private void UseNeighborProvincesToRemoveIslam(HashSet<Province> muslimProvinces, Date date) {
		foreach (var province in muslimProvinces) {
			var neighborIds = MapData.GetNeighborProvinceIds(province.Id);
			if (neighborIds.Count == 0) {
				continue;
			}

			var neighborFaithId = Provinces
				.Except(muslimProvinces)
				.Where(p => neighborIds.Contains(p.Id))
				.Select(p => p.GetFaithId(date))
				.FirstOrDefault(f => f is not null);
			if (neighborFaithId is null) {
				continue;
			}
			
			Logger.Debug($"Using neighbor's faith \"{neighborFaithId}\" for province {province.Id}.");
			province.SetFaithIdAndOverrideExistingEntries(neighborFaithId);
			muslimProvinces.Remove(province);
		}
	}

	private void GenerateFillerHoldersForUnownedLands(CultureCollection cultures, Configuration config) {
		Logger.Info("Generating filler holders for unowned lands...");
		var date = config.CK3BookmarkDate;
		List<Title> unheldCounties = [];
		foreach (var county in LandedTitles.Counties) {
			var holderId = county.GetHolderId(date);
			if (holderId == "0") {
				unheldCounties.Add(county);
			} else if (Characters.TryGetValue(holderId, out var holder)) {
				if (holder.DeathDate is not null && holder.DeathDate <= date) {
					Logger.Debug($"Adding {county.Id} to unheld counties because holder {holderId} is dead.");
					unheldCounties.Add(county);
				}
			}
		}

		var duchyIdToHolderDict = new Dictionary<string, Character>();

		foreach (var county in unheldCounties) {
			if (config.FillerDukes) {
				var duchy = county.DeJureLiege;
				if (duchy is not null && duchy.Rank == TitleRank.duchy) {
					if (duchyIdToHolderDict.TryGetValue(duchy.Id, out var duchyHolder)) {
						county.SetHolder(duchyHolder, date);
						continue;
					}
				}
			}

			var candidateProvinces = new OrderedSet<Province>();
			if (county.CapitalBaronyProvinceId is not null) {
				// Give priority to capital province.
				if (Provinces.TryGetValue(county.CapitalBaronyProvinceId.Value, out var capitalProvince)) {
					candidateProvinces.Add(capitalProvince);
				}
			}

			var allCountyProvinces = county.CountyProvinceIds
				.Select(id => Provinces.TryGetValue(id, out var province) ? province : null)
				.Where(p => p is not null)
				.Select(p => p!);
			candidateProvinces.UnionWith(allCountyProvinces);

			int pseudoRandomSeed;
			if (candidateProvinces.Count != 0) {
				pseudoRandomSeed = (int)candidateProvinces.First().Id;
			} else {
				// Use county ID for seed if no province is available.
				pseudoRandomSeed = county.Id.Aggregate(0, (current, c) => current + c);
			}
			
			// Determine culture of the holder.
			var culture = candidateProvinces
				.Select(p => p.GetCulture(date, cultures))
				.FirstOrDefault(c => c is not null);
			if (culture is null) {
				Logger.Debug($"Trying to use de jure duchy for culture of holder for {county.Id}...");
				var deJureDuchy = county.DeJureLiege;
				if (deJureDuchy is not null) {
					culture = Provinces
						.Where(p => deJureDuchy.DuchyContainsProvince(p.Id))
						.Select(p => p.GetCulture(date, cultures))
						.FirstOrDefault(c => c is not null);
				}
				if (culture is null && deJureDuchy?.DeJureLiege is not null) {
					Logger.Debug($"Trying to use de jure kingdom for culture of holder for {county.Id}...");
					var deJureKingdom = deJureDuchy.DeJureLiege;
					culture = Provinces
						.Where(p => deJureKingdom.KingdomContainsProvince(p.Id))
						.Select(p => p.GetCulture(date, cultures))
						.FirstOrDefault(c => c is not null);
				}
				if (culture is null) {
					Logger.Warn($"Found no fitting culture for generated holder of {county.Id}, " +
					            "using first culture from database!");
					culture = cultures.First();
				}
			}
			
			// Determine faith of the holder.
			var faithId = candidateProvinces
				.Select(p => p.GetFaithId(date))
				.FirstOrDefault(f => f is not null);
			if (faithId is null) {
				Logger.Debug($"Trying to use de jure duchy for faith of holder for {county.Id}...");
				var deJureDuchy = county.DeJureLiege;
				if (deJureDuchy is not null) {
					faithId = Provinces
						.Where(p => deJureDuchy.DuchyContainsProvince(p.Id))
						.Select(p => p.GetFaithId(date))
						.FirstOrDefault(f => f is not null);
				}
				if (faithId is null && deJureDuchy?.DeJureLiege is not null) {
					Logger.Debug($"Trying to use de jure kingdom for faith of holder for {county.Id}...");
					var deJureKingdom = deJureDuchy.DeJureLiege;
					faithId = Provinces
						.Where(p => deJureKingdom.KingdomContainsProvince(p.Id))
						.Select(p => p.GetFaithId(date))
						.FirstOrDefault(f => f is not null);
				}
				if (faithId is null) {
					Logger.Warn($"Found no fitting faith for generated holder of {county.Id}, " +
					            "using first faith from database!");
					faithId = Religions.Faiths.First().Id;
				}
			}

			bool female = false;
			string name;
			var maleNames = culture.MaleNames.ToImmutableList();
			if (maleNames.Count > 0) {
				name = maleNames[pseudoRandomSeed % maleNames.Count];
			} else { // Generate a female if no male name is available.
				female = true;
				var femaleNames = culture.FemaleNames.ToImmutableList();
				name = femaleNames[pseudoRandomSeed % femaleNames.Count];
			}
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

			var countyHoldingTypes = county.CountyProvinceIds
				.Select(id => Provinces.TryGetValue(id, out var province) ? province : null)
				.Where(p => p is not null)
				.Select(p => p!.GetHoldingType(date))
				.Where(t => t is not null)
				.Select(t => t!)
				.ToFrozenSet();
			string government = countyHoldingTypes.Contains("castle_holding")
				? "feudal_government"
				: "tribal_government";

			county.SetHolder(holder, date);
			if (config.FillerDukes) {
				var duchy = county.DeJureLiege;
				if (duchy is null || duchy.Rank != TitleRank.duchy) {
					continue;
				}

				duchy.SetHolder(holder, date);
				duchy.SetGovernment(government, date);
				duchyIdToHolderDict[duchy.Id] = holder;
			} else {
				county.SetGovernment(government, date);
			}
		}
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
	private readonly CK3RegionMapper ck3RegionMapper;
	private readonly ImperatorRegionMapper imperatorRegionMapper;
	private readonly WarMapper warMapper = new("configurables/wargoal_mappings.txt");
}