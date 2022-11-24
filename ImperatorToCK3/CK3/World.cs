using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Map;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
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
using ImperatorToCK3.Mappers.UnitType;
using Open.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3 {
	public class World {
		public OrderedSet<Mod> LoadedMods { get; }
		public ModFilesystem ModFS { get; private set; }
		private ScriptValueCollection ScriptValues { get; } = new();
		public NamedColorCollection NamedColors { get; } = new();
		public CharacterCollection Characters { get; } = new();
		public DynastyCollection Dynasties { get; } = new();
		public ProvinceCollection Provinces { get; } = new();
		public Title.LandedTitles LandedTitles { get; } = new();
		public ReligionCollection Religions { get; }
		public IdObjectCollection<string, MenAtArmsType> MenAtArmsTypes { get; }= new();
		public MapData MapData { get; }
		public Date CorrectedDate { get; }

		public World(Imperator.World impWorld, Configuration config) {
			Logger.Info("*** Hello CK3, let's get painting. ***");
			
			// Initialize fields that depend on other fields.
			Religions = new ReligionCollection(LandedTitles);
			
			// Determine CK3 bookmark date.
			CorrectedDate = impWorld.EndDate.Year > 1 ? impWorld.EndDate : new Date(2, 1, 1);
			if (config.CK3BookmarkDate.Year == 0) { // bookmark date is not set
				config.CK3BookmarkDate = CorrectedDate;
				Logger.Info($"CK3 bookmark date set to: {config.CK3BookmarkDate}");
			} else if (CorrectedDate > config.CK3BookmarkDate) {
				Logger.Error("Corrected save date is later than CK3 bookmark date, proceeding at your own risk!");
			}

			LoadCorrectProvinceMappingsVersion(impWorld);
			
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
			LoadedMods = modLoader.UsableMods.ToOrderedSet();
			// Include a fake mod pointing to blankMod.
			LoadedMods.Add(new Mod("blankMod", "blankMod/output"));
			ModFS = new ModFilesystem(Path.Combine(config.CK3Path, "game"), LoadedMods);
			Logger.IncrementProgress();
			
			ScriptValues.LoadScriptValues(ModFS);
			
			NamedColors.LoadNamedColors("common/named_colors", ModFS);
			Faith.ColorFactory.AddNamedColorDict(NamedColors);

			LoadMenAtArmsTypes(ModFS, ScriptValues);

			Logger.Info("Loading map data...");
			MapData = new MapData(ModFS);

			// Load Imperator CoAs to use them for generated CK3 titles
			coaMapper = new CoaMapper(impWorld.ModFS);

			// Load vanilla CK3 landed titles and their history
			LandedTitles.LoadTitles(ModFS);
			LandedTitles.LoadHistory(config, ModFS);
			LandedTitles.LoadCulturalNamesFromConfigurables();
			
			// Load CK3 religions from game and blankMod.
			// Holy sites need to be loaded after landed titles.
			Religions.LoadHolySites(ModFS);
			Religions.LoadReligions(ModFS);
			Religions.LoadReplaceableHolySites("configurables/replaceable_holy_sites.txt");

			// Load regions.
			ck3RegionMapper = new CK3RegionMapper(ModFS, LandedTitles);
			imperatorRegionMapper = new ImperatorRegionMapper(impWorld.ModFS);
			// Use the region mappers in other mappers
			var religionMapper = new ReligionMapper(Religions, imperatorRegionMapper, ck3RegionMapper);
			var cultureMapper = new CultureMapper(imperatorRegionMapper, ck3RegionMapper);

			var traitMapper = new TraitMapper(Path.Combine("configurables", "trait_map.txt"), ModFS);

			Characters.ImportImperatorCharacters(
				impWorld,
				religionMapper,
				cultureMapper,
				traitMapper,
				nicknameMapper,
				impWorld.LocDB,
				provinceMapper,
				deathReasonMapper,
				CorrectedDate,
				config
			);
			ClearFeaturedCharactersDescriptions(config.CK3BookmarkDate);

			Dynasties.ImportImperatorFamilies(impWorld, impWorld.LocDB);

			LandedTitles.ImportImperatorCountries(
				impWorld.Countries,
				tagTitleMapper,
				impWorld.LocDB,
				provinceMapper,
				coaMapper,
				governmentMapper,
				successionLawMapper,
				definiteFormMapper,
				religionMapper,
				cultureMapper,
				nicknameMapper,
				Characters,
				CorrectedDate,
				config
			);

			// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
			// Some of it will be overwritten, but not all.
			Provinces.ImportVanillaProvinces(ModFS);

			// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
			Provinces.ImportImperatorProvinces(impWorld, LandedTitles, cultureMapper, religionMapper, provinceMapper, config);
			Provinces.LoadPrehistory();
			
			var countyLevelGovernorships = new List<Governorship>();
			LandedTitles.ImportImperatorGovernorships(
				impWorld,
				Provinces,
				tagTitleMapper,
				impWorld.LocDB,
				provinceMapper,
				definiteFormMapper,
				imperatorRegionMapper,
				coaMapper,
				countyLevelGovernorships
			);

			OverwriteCountiesHistory(impWorld.Jobs.Governorships, countyLevelGovernorships, impWorld.Characters, CorrectedDate);
			LandedTitles.ImportDevelopmentFromImperator(Provinces, CorrectedDate, config.ImperatorCivilizationWorth);
			LandedTitles.RemoveInvalidLandlessTitles(config.CK3BookmarkDate);
			LandedTitles.SetDeJureKingdomsAndEmpires(config.CK3BookmarkDate);
			Dynasties.SetCoasForRulingDynasties(LandedTitles);

			Characters.DistributeCountriesGold(LandedTitles, config);
			Characters.ImportLegions(LandedTitles, impWorld.Units, impWorld.Characters, CorrectedDate, unitTypeMapper, MenAtArmsTypes, provinceMapper, config);

			Characters.RemoveEmployerIdFromLandedCharacters(LandedTitles, CorrectedDate);
			Characters.PurgeUnneededCharacters(LandedTitles);
			
			// Apply region-specific tweaks.
			HandleIcelandAndFaroeIslands(config);
			RemoveIslamFromAfrica(config);

			var holySiteEffectMapper = new HolySiteEffectMapper("configurables/holy_site_effect_mappings.txt");
			Religions.DetermineHolySites(Provinces, impWorld.Religions, holySiteEffectMapper, config.CK3BookmarkDate);
		}

		private void LoadCorrectProvinceMappingsVersion(Imperator.World imperatorWorld) {
			var mappingsVersion = "imperator_invictus";
			if (!imperatorWorld.GlobalFlags.Contains("is_playing_invictus")) {
				Logger.Warn("Support for non-Invictus Imperator saves is deprecated.");
				mappingsVersion = "imperator_vanilla";
			}
			Logger.Debug($"Using mappings version: {mappingsVersion}");
			provinceMapper.LoadMappings("configurables/province_mappings.txt", mappingsVersion);
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
					title.Localizations.AddLocBlock($"{holder.Name}_desc");
				}
			}
		}

		private void OverwriteCountiesHistory(IEnumerable<Governorship> governorships, IEnumerable<Governorship> countyLevelGovernorships, Imperator.Characters.CharacterCollection impCharacters, Date conversionDate) {
			Logger.Info("Overwriting counties' history...");
			var governorshipsSet = governorships.ToHashSet();
			var countyLevelGovernorshipsSet = countyLevelGovernorships.ToHashSet();
			
			foreach (var county in LandedTitles.Where(t => t.Rank == TitleRank.county)) {
				if (county.CapitalBaronyProvince is null) {
					Logger.Warn($"County {county} has no capital barony province!");
					continue;
				}
				ulong capitalBaronyProvinceId = (ulong)county.CapitalBaronyProvince;
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
				var impProvince = ck3CapitalBaronyProvince.PrimaryImperatorProvince;
				if (impProvince is null) { // probably outside of Imperator map
					continue;
				}

				var impCountry = impProvince.OwnerCountry;

				if (impCountry is null || impCountry.CountryType == CountryType.rebels) { // e.g. uncolonized Imperator province
					county.SetHolder(null, conversionDate);
					county.SetDeFactoLiege(null, conversionDate);
				} else {
					bool given = TryGiveCountyToGovernor(county, impProvince, impCountry);
					if (!given) {
						given = TryGiveCountyToMonarch(county, impCountry);
					}
					if (!given) {
						Logger.Warn($"County {county} was not given to anyone!");
					}
				}
			}
			Logger.IncrementProgress();

			bool TryGiveCountyToMonarch(Title county, Country impCountry) {
				var ck3Country = impCountry.CK3Title;
				if (ck3Country is null) {
					Logger.Warn($"{impCountry.Name} has no CK3 title!"); // should not happen
					return false;
				}

				GiveCountyToMonarch(county, ck3Country);
				return true;
			}

			void GiveCountyToMonarch(Title county, Title ck3Country) {
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

			bool TryGiveCountyToGovernor(Title county, Imperator.Provinces.Province impProvince, Country impCountry) {
				var ck3Country = impCountry.CK3Title;
				if (ck3Country is null) {
					Logger.Warn($"{impCountry.Name} has no CK3 title!"); // should not happen
					return false;
				}
				var matchingGovernorships = new List<Governorship>(governorshipsSet.Where(g =>
					g.CountryId == impCountry.Id &&
					g.RegionName == imperatorRegionMapper.GetParentRegionName(impProvince.Id)
				));

				var ck3CapitalCounty = ck3Country.CapitalCounty;
				if (ck3CapitalCounty is null) {
					Logger.Warn($"{ck3Country} has no capital county!");
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
				var ck3GovernorshipId = tagTitleMapper.GetTitleForGovernorship(governorship, impCountry, LandedTitles, Provinces, imperatorRegionMapper);
				if (ck3GovernorshipId is null) {
					Logger.Warn($"{nameof(ck3GovernorshipId)} is null for {ck3Country} {governorship.RegionName}!");
					return false;
				}

				if (countyLevelGovernorshipsSet.Contains(governorship)) {
					GiveCountyToCountyLevelGovernor(county, governorship, ck3Country);
				} else {
					GiveCountyToGovernor(county, ck3GovernorshipId);
				}
				return true;
			}

			void GiveCountyToGovernor(Title county, string ck3GovernorshipId) {
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

			void GiveCountyToCountyLevelGovernor(Title county, Governorship governorship, Title ck3Country) {
				var holderChangeDate = governorship.StartDate;
				var impGovernor = impCharacters[governorship.CharacterId];
				var governor = impGovernor.CK3Character;

				county.ClearHolderSpecificHistory();
				county.SetHolder(governor, holderChangeDate);
				county.SetDeFactoLiege(ck3Country, holderChangeDate);
			}
		}

		private void HandleIcelandAndFaroeIslands(Configuration config) {
			Logger.Info("Handling Iceland and Faroe Islands...");
			Date bookmarkDate = config.CK3BookmarkDate;
			var year = bookmarkDate.Year;

			var faiths = Religions.Faiths.ToList();
			var titleIdsToHandle = new OrderedSet<string> {"d_iceland", "c_faereyar"};

			bool generateHermits = true;
			IEnumerable<string> faithCandidates = new OrderedSet<string>();
			Queue<string> namePool = new();
			const string defaultCultureId = "irish";
			string cultureId = defaultCultureId;

			switch (year) {
				case <= 300:
					UsePaganRulers();
					break;
				case > 300 and < 874:
					faithCandidates = new OrderedSet<string> {"insular_celtic", "catholic", "orthodox"};
					var christianFaiths = Religions["christianity_religion"].Faiths;
					
					// If there is at least an Irish Christian county, give it to the Irish Papar.
					// If there is at least a Christian county of another Gaelic culture, give it to a character of this Gaelic culture.
					var cultureCandidates = new[] {"irish", "gaelic"};
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
						// If all the Gaels are pagan but at least one province in Ireland is Christian,
						// give the handled titles to a generated ruler of the same culture of that Christian county in Ireland.
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
						namePool = new Queue<string>(new[] {"Canann", "Petair", "Fergus"});
					}
					break;
				default:
					Logger.Info("Keeping Iceland and Faroe Islands as is in history...");
					// Let CK3 use rulers from its history.
					generateHermits = false;
					break;
			}

			if (generateHermits) {
				faithCandidates = faithCandidates.ToList(); // prevent multiple enumeration
				foreach (var titleId in titleIdsToHandle) {
					if (!LandedTitles.TryGetValue(titleId, out var title)) {
						Logger.Warn($"Title {titleId} not found!");
						continue;
					}
					Logger.Debug($"Generating hermit for {titleId}...");
				
					var hermit = new Character($"IRToCK3_{titleId}_hermit", namePool.Dequeue(), bookmarkDate.ChangeByYears(-50));
					var faithId = faithCandidates.First(c => faiths.Any(f => f.Id == c));
					hermit.FaithId = faithId;
					hermit.CultureId = cultureId;
					hermit.History.AddFieldValue(null, "traits", "trait", "chaste");
					hermit.History.AddFieldValue(null, "traits", "trait", "celibate");
					hermit.History.AddFieldValue(null, "traits", "trait", "devoted");
					var eremiteEffect = new StringOfItem("{ set_variable = IRToCK3_eremite_flag }");
					hermit.History.AddFieldValue(config.CK3BookmarkDate, "effects", "effect", eremiteEffect);
					Characters.Add(hermit);
			
					title.SetHolder(hermit, bookmarkDate);
					title.SetGovernment("eremitic_government", bookmarkDate);
					foreach (var county in title.GetDeJureVassalsAndBelow(rankFilter: "c").Values) {
						county.SetHolder(hermit, bookmarkDate);
						county.SetDevelopmentLevel(0, bookmarkDate);
						foreach (var provinceId in county.CountyProvinces) {
							var province = Provinces[provinceId];
							province.History.RemoveHistoryPastDate("1.1.1");
							province.SetFaithId(faithId, date: null);
							province.SetCultureId(cultureId, date: null);
							province.SetBuildings(new List<string>(), date: null);
							province.History.Fields["holding"].RemoveAllEntries();
						}
					}
				}
			}

			Logger.IncrementProgress();

			void UsePaganRulers() {
				Logger.Info("Giving Iceland and Faroe Islands to pagan Gaels...");
				faithCandidates = new OrderedSet<string> {"gaelic_paganism", "celtic_pagan", "briton_paganism", "pagan"};
				cultureId = "gaelic";
				// ReSharper disable once StringLiteralTypo
				namePool = new Queue<string>(new[]{"A_engus", "Domnall", "Rechtabra"});
			}
		}
		
		/// <summary>
		/// It makes no sense to have Islam in Africa before the rise of Islam.
		/// This method removes it.
		/// </summary>
		private void RemoveIslamFromAfrica(Configuration config) {
			Logger.Info("Removing Islam from Africa...");
			var date = config.CK3BookmarkDate;
			
			if (!Religions.TryGetValue("islam_religion", out var islam)) {
				Logger.Debug("islam_religion not found in religions.");
				return;
			}
			
			var muslimFaiths = islam.Faiths;
			
			var muslimAfricanProvinces = Provinces
				.Where(p => ck3RegionMapper.ProvinceIsInRegion(p.Id, "world_africa"))
				.Where(p => p.GetFaithId(date) is string faithId && muslimFaiths.ContainsKey(faithId))
				.ToHashSet();

			var regionToNewFaithDict = new OrderedDictionary<string, string> {
				{"world_africa_north", "berber_pagan"},
				{"world_africa_west", "berber_pagan"},
				{"world_africa_east", "waaqism_pagan"},
				{"world_africa_sahara", "berber_pagan"},
				{"world_africa", "pagan"} // fallback
			};

			foreach (var (regionId, faithId) in regionToNewFaithDict) {
				if (Religions.GetFaith(faithId) is null) {
					Logger.Warn($"Faith {faithId} not found.");
					continue;
				}
				
				var regionProvinces = muslimAfricanProvinces
					.Where(p => ck3RegionMapper.ProvinceIsInRegion(p.Id, regionId));
				foreach (var province in regionProvinces) {
					var faithHistoryField = province.History.Fields["faith"];
					faithHistoryField.RemoveAllEntries();
					faithHistoryField.AddEntryToHistory(null, "faith", faithId);

					muslimAfricanProvinces.Remove(province);
				}
			}
			
			Logger.IncrementProgress();
		}

		private readonly CoaMapper coaMapper;
		private readonly DeathReasonMapper deathReasonMapper = new();
		private readonly DefiniteFormMapper definiteFormMapper = new(Path.Combine("configurables", "definite_form_names.txt"));
		private readonly GovernmentMapper governmentMapper = new();
		private readonly NicknameMapper nicknameMapper = new(Path.Combine("configurables", "nickname_map.txt"));
		private readonly ProvinceMapper provinceMapper = new();
		private readonly SuccessionLawMapper successionLawMapper = new(Path.Combine("configurables", "succession_law_map.txt"));
		private readonly TagTitleMapper tagTitleMapper = new(
			tagTitleMappingsPath: Path.Combine("configurables", "title_map.txt"),
			governorshipTitleMappingsPath: Path.Combine("configurables", "governorMappings.txt")
		);
		private readonly UnitTypeMapper unitTypeMapper = new("configurables/unit_types_map.txt");
		private readonly CK3RegionMapper ck3RegionMapper;
		private readonly ImperatorRegionMapper imperatorRegionMapper;
	}
}
