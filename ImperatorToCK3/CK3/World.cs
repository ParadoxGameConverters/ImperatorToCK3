﻿using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.Trait;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3 {
	public class World {
		public Characters.Characters Characters { get; } = new();
		public Dictionary<string, Dynasty> Dynasties { get; } = new();
		public Dictionary<ulong, Province> Provinces { get; } = new();
		public LandedTitles LandedTitles { get; } = new();
		public Map.MapData MapData { get; }

		public World(Imperator.World impWorld, Configuration theConfiguration) {
			Logger.Info("*** Hello CK3, let's get painting. ***");

			Logger.Info("Loading map data...");
			MapData = new Map.MapData(theConfiguration.Ck3Path);

			// Scraping localizations from Imperator so we may know proper names for our countries.
			localizationMapper.ScrapeLocalizations(theConfiguration, impWorld.Mods);

			// Loading Imperator CoAs to use them for generated CK3 titles
			coaMapper = new CoaMapper(theConfiguration);

			// Load vanilla titles history
			var titlesHistoryPath = Path.Combine(theConfiguration.Ck3Path, "game/history/titles");
			titlesHistory = new TitlesHistory(titlesHistoryPath, theConfiguration.Ck3BookmarkDate);

			// Load vanilla CK3 landed titles
			var landedTitlesPath = Path.Combine(theConfiguration.Ck3Path, "game/common/landed_titles/00_landed_titles.txt");

			LandedTitles.LoadTitles(landedTitlesPath);
			AddHistoryToVanillaTitles(theConfiguration.Ck3BookmarkDate);

			// Loading regions
			ck3RegionMapper = new CK3RegionMapper(theConfiguration.Ck3Path, LandedTitles);
			imperatorRegionMapper = new ImperatorRegionMapper(theConfiguration.ImperatorPath);
			// Use the region mappers in other mappers
			religionMapper.LoadRegionMappers(imperatorRegionMapper, ck3RegionMapper);
			cultureMapper.LoadRegionMappers(imperatorRegionMapper, ck3RegionMapper);

			ImportImperatorCountries(impWorld.Countries);
			ImportImperatorGovernorships(impWorld);

			// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
			// Some of it will be overwritten, but not all.
			ImportVanillaProvinces(theConfiguration.Ck3Path, theConfiguration.Ck3BookmarkDate);

			// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
			ImportImperatorProvinces(impWorld);

			Characters.ImportImperatorCharacters(
				impWorld,
				religionMapper,
				cultureMapper,
				traitMapper,
				nicknameMapper,
				localizationMapper,
				provinceMapper,
				deathReasonMapper,
				impWorld.EndDate,
				theConfiguration.Ck3BookmarkDate
			);
			ClearFeaturedCharactersDescriptions(theConfiguration.Ck3BookmarkDate);

			ImportImperatorFamilies(impWorld);

			OverWriteCountiesHistory(impWorld.Jobs.Governorships, theConfiguration.Ck3BookmarkDate);
			RemoveInvalidLandlessTitles(theConfiguration.Ck3BookmarkDate);

			Characters.PurgeLandlessVanillaCharacters(LandedTitles, theConfiguration.Ck3BookmarkDate);
			Characters.RemoveEmployerIdFromLandedCharacters(LandedTitles, impWorld.EndDate);
		}

		private void ClearFeaturedCharactersDescriptions(Date ck3BookmarkDate) {
			Logger.Info("Clearing featured characters' descriptions.");
			foreach (var title in LandedTitles.Values) {
				if (!title.PlayerCountry) {
					continue;
				}
				var holderId = title.GetHolderId(ck3BookmarkDate);
				if (holderId != "0" && Characters.TryGetValue(holderId, out var holder)) {
					title.Localizations.Add($"{holder.Name}_desc", new LocBlock());
				}
			}
		}

		private void ImportImperatorCountries(Dictionary<ulong, Country> imperatorCountries) {
			Logger.Info("Importing Imperator Countries.");

			// landedTitles holds all titles imported from CK3. We'll now overwrite some and
			// add new ones from Imperator tags.
			var counter = 0;
			// We don't need pirates, barbarians etc.
			foreach (var country in imperatorCountries.Values.Where(c => c.CountryType == CountryType.real)) {
				ImportImperatorCountry(country, imperatorCountries);
				++counter;
			}
			Logger.Info($"Imported {counter} countries from I:R.");
		}

		private void ImportImperatorCountry(
			Country country,
			Dictionary<ulong, Country> imperatorCountries
		) {
			// Create a new title or update existing title
			var name = Title.DetermineName(country, imperatorCountries, tagTitleMapper, localizationMapper);

			if (LandedTitles.TryGetValue(name, out var existingTitle)) {
				existingTitle.InitializeFromTag(
					country,
					imperatorCountries,
					localizationMapper,
					LandedTitles,
					provinceMapper,
					coaMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					Characters
				);
			} else {
				var newTitle = new Title(
					country,
					imperatorCountries,
					localizationMapper,
					LandedTitles,
					provinceMapper,
					coaMapper,
					tagTitleMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					Characters
				);
				LandedTitles.Add(newTitle);
			}
		}

		private void ImportImperatorGovernorships(Imperator.World impWorld) {
			Logger.Info("Importing Imperator Governorships.");

			var governorships = impWorld.Jobs.Governorships;
			var imperatorCountries = impWorld.Countries;

			var governorshipsPerRegion = governorships.GroupBy(g => g.RegionName)
				.ToDictionary(g => g.Key, g => g.Count());

			// landedTitles holds all titles imported from CK3. We'll now overwrite some and
			// add new ones from Imperator governorships.
			var counter = 0;
			foreach (var governorship in governorships) {
				ImportImperatorGovernorship(
					governorship,
					imperatorCountries,
					impWorld.Characters,
					governorshipsPerRegion[governorship.RegionName] > 1
				);
				++counter;
			}
			Logger.Info($"Imported {counter} governorships from I:R.");
		}
		private void ImportImperatorGovernorship(
			Governorship governorship,
			Dictionary<ulong, Country> imperatorCountries,
			Imperator.Characters.Characters imperatorCharacters,
			bool regionHasMultipleGovernorships
		) {
			var country = imperatorCountries[governorship.CountryId];
			// Create a new title or update existing title
			var name = Title.DetermineName(governorship, country, tagTitleMapper);

			if (LandedTitles.TryGetValue(name, out var existingTitle)) {
				existingTitle.InitializeFromGovernorship(
					governorship,
					country,
					imperatorCharacters,
					regionHasMultipleGovernorships,
					localizationMapper,
					LandedTitles,
					provinceMapper,
					definiteFormMapper,
					imperatorRegionMapper
				);
			} else {
				var newTitle = new Title(
					governorship,
					country,
					imperatorCharacters,
					regionHasMultipleGovernorships,
					localizationMapper,
					LandedTitles,
					provinceMapper,
					coaMapper,
					tagTitleMapper,
					definiteFormMapper,
					imperatorRegionMapper
				);
				LandedTitles.Add(newTitle);
			}
		}

		private void ImportVanillaProvinces(string ck3Path, Date ck3BookmarkDate) {
			Logger.Info("Importing Vanilla Provinces.");
			// ---- Loading history/provinces
			var path = Path.Combine(ck3Path, "game/history/provinces");
			var fileNames = SystemUtils.GetAllFilesInFolderRecursive(path);
			foreach (var fileName in fileNames) {
				if (!fileName.EndsWith(".txt")) {
					continue;
				}
				var provincesPath = Path.Combine(ck3Path, "game/history/provinces", fileName);
				try {
					var newProvinces = new Provinces.Provinces(provincesPath, ck3BookmarkDate);
					foreach (var (newProvinceId, newProvince) in newProvinces) {
						if (Provinces.ContainsKey(newProvinceId)) {
							Logger.Warn($"Vanilla province duplication - {newProvinceId} already loaded! Overwriting.");
						}
						Provinces[newProvinceId] = newProvince;
					}
				} catch (Exception e) {
					Logger.Warn($"Invalid province filename: {provincesPath} ({e})");
				}
			}

			// now load the provinces that don't have unique entries in history/provinces
			// they instead use history/province_mapping
			path = Path.Combine(ck3Path, "game/history/province_mapping");
			fileNames = SystemUtils.GetAllFilesInFolderRecursive(path);
			foreach (var fileName in fileNames) {
				if (!fileName.EndsWith(".txt")) {
					continue;
				}

				var provinceMappingsPath = Path.Combine(ck3Path, "game/history/province_mapping", fileName);
				try {
					var newMappings = new ProvinceMappings(provinceMappingsPath);
					foreach (var (newProvinceId, baseProvinceId) in newMappings) {
						if (!Provinces.ContainsKey(baseProvinceId)) {
							Logger.Warn($"Base province {baseProvinceId} not found for province {newProvinceId}.");
							continue;
						}
						if (Provinces.ContainsKey(newProvinceId)) {
							Logger.Info($"Vanilla province duplication - {newProvinceId} already loaded! Preferring unique entry over mapping.");
						} else {
							var newProvince = new Province(newProvinceId, Provinces[baseProvinceId]);
							Provinces.Add(newProvinceId, newProvince);
						}
					}
				} catch (Exception e) {
					Logger.Warn($"Invalid province filename: {provinceMappingsPath}: ({e})");
				}
			}

			Logger.Info($"Loaded {Provinces.Count} province definitions.");
		}

		private void ImportImperatorProvinces(Imperator.World impWorld) {
			Logger.Info("Importing Imperator Provinces.");
			var counter = 0;
			// Imperator provinces map to a subset of CK3 provinces. We'll only rewrite those we are responsible for.
			foreach (var (provinceId, province) in Provinces) {
				var impProvinces = provinceMapper.GetImperatorProvinceNumbers(provinceId);
				// Provinces we're not affecting will not be in this list.
				if (impProvinces.Count == 0) {
					continue;
				}
				// Next, we find what province to use as its initializing source.
				var sourceProvince = DetermineProvinceSource(impProvinces, impWorld);
				if (sourceProvince is null) {
					Logger.Warn($"Could not determine source province for CK3 province {provinceId}!");
					continue; // MISMAP, or simply have mod provinces loaded we're not using.
				}
				province.InitializeFromImperator(sourceProvince.Value.Value, LandedTitles, cultureMapper, religionMapper);
				// And finally, initialize it.
				++counter;
			}
			Logger.Info($"{impWorld.Provinces.Count} Imperator provinces imported into {counter} CK3 provinces.");
		}

		private static KeyValuePair<ulong, Imperator.Provinces.Province>? DetermineProvinceSource(
			List<ulong> impProvinceNumbers,
			Imperator.World impWorld
		) {
			// determine ownership by province development.
			var theClaims = new Dictionary<ulong, List<Imperator.Provinces.Province>>(); // owner, offered province sources
			var theShares = new Dictionary<ulong, int>(); // owner, development                                               
			ulong? winner = null;
			long maxDev = -1;

			foreach (var imperatorProvinceId in impProvinceNumbers) {
				if (!impWorld.Provinces.TryGetValue(imperatorProvinceId, out var impProvince)) {
					Logger.Warn($"Source province {imperatorProvinceId} is not on the list of known provinces!");
					continue; // Broken mapping, or loaded a mod changing provinces without using it.
				}

				var ownerId = impProvince.OwnerCountry?.Id ?? 0;
				if (!theClaims.ContainsKey(ownerId)) {
					theClaims[ownerId] = new();
				}

				theClaims[ownerId].Add(impProvince);

				var devValue = (int)impProvince.BuildingCount + impProvince.GetPopCount();
				theShares[ownerId] = devValue;
			}
			// Let's see who the lucky winner is.
			foreach (var (owner, development) in theShares) {
				if (development > maxDev) {
					winner = owner;
					maxDev = development;
				}
			}
			if (winner is null) {
				return null;
			}

			// Now that we have a winning owner, let's find its largest province to use as a source.
			maxDev = -1; // We can have winning provinces with weight = 0;

			var toReturn = new KeyValuePair<ulong, Imperator.Provinces.Province>();
			foreach (var province in theClaims[(ulong)winner]) {
				long provinceWeight = province.BuildingCount + province.GetPopCount();

				if (provinceWeight > maxDev) {
					toReturn = new(province.Id, province);
					maxDev = provinceWeight;
				}
			}
			if (toReturn.Key == 0 || toReturn.Value is null) {
				return null;
			}
			return toReturn;
		}

		private void AddHistoryToVanillaTitles(Date ck3BookmarkDate) {
			foreach (var (name, title) in LandedTitles) {
				var historyOpt = titlesHistory.PopTitleHistory(name);
				if (historyOpt is not null) {
					title.AddHistory(LandedTitles, historyOpt);
				}
			}
			// add vanilla development to counties
			// for counties that inherit development level from de jure lieges, assign it to them directly for better reliability
			foreach (Title title in LandedTitles.Values.Where(title => title.Rank == TitleRank.county && title.DevelopmentLevel is null)) {
				title.DevelopmentLevel = title.OwnOrInheritedDevelopmentLevel;
			}

			// remove history entries past the bookmark date
			foreach (var title in LandedTitles.Values) {
				title.RemoveHistoryPastBookmarkDate(ck3BookmarkDate);
			}
		}

		private void OverWriteCountiesHistory(IReadOnlyCollection<Governorship> governorships, Date ck3BookmarkDate) {
			Logger.Info("Overwriting counties' history.");
			foreach (var title in LandedTitles.Values) {
				if (title.Rank != TitleRank.county) {
					continue;
				}
				ulong capitalBaronyProvinceId = (ulong)title.CapitalBaronyProvince!;
				if (capitalBaronyProvinceId == 0) {
					// title's capital province has an invalid ID (0 is not a valid province in CK3)
					continue;
				}

				if (!Provinces.ContainsKey(capitalBaronyProvinceId)) {
					Logger.Warn($"Capital barony province not found {title.CapitalBaronyProvince}");
					continue;
				}

				var ck3CapitalBaronyProvince = Provinces[capitalBaronyProvinceId];
				var impProvince = ck3CapitalBaronyProvince.ImperatorProvince;
				if (impProvince is null) {
					continue;
				}

				var impCountry = impProvince.OwnerCountry;

				if (impCountry is null || impCountry.CountryType == CountryType.rebels) { // e.g. uncolonized Imperator province
					title.SetHolderId("0", ck3BookmarkDate);
					title.DeFactoLiege = null;
				} else {
					var ck3Country = impCountry.CK3Title;
					if (ck3Country is null) {
						Logger.Warn($"{impCountry.Name} has no CK3 title!"); // should not happen
						continue;
					}
					var ck3CapitalCounty = ck3Country.CapitalCounty;
					var impMonarch = impCountry.Monarch;
					var matchingGovernorships = new List<Governorship>(governorships.Where(g =>
						g.CountryId == impCountry.Id &&
						g.RegionName == imperatorRegionMapper.GetParentRegionName(impProvince.Id)
					));

					if (ck3CapitalCounty is null) {
						if (impMonarch is not null) {
							GiveCountyToMonarch(title, ck3Country, impMonarch.Id);
						} else {
							Logger.Warn($"Imperator ruler doesn't exist for {impCountry.Name} owning {title.Name}!");
						}
						continue;
					}
					// if title belongs to country ruler's capital's de jure duchy, make it directly held by the ruler
					var countryCapitalDuchy = ck3CapitalCounty.DeJureLiege;
					var titleLiegeDuchy = title.DeJureLiege;
					if (countryCapitalDuchy is not null && titleLiegeDuchy is not null && countryCapitalDuchy.Name == titleLiegeDuchy.Name) {
						if (impMonarch is not null) {
							GiveCountyToMonarch(title, ck3Country, impMonarch.Id);
						}
					} else if (matchingGovernorships.Count > 0) {
						// give county to governor
						var governorship = matchingGovernorships[0];
						var ck3GovernorshipName = tagTitleMapper.GetTitleForGovernorship(governorship.RegionName, impCountry.Tag, ck3Country.Name);
						if (ck3GovernorshipName is null) {
							Logger.Warn($"{nameof(ck3GovernorshipName)} is null for {ck3Country.Name} {governorship.RegionName}!");
							continue;
						}
						GiveCountyToGovernor(title, ck3GovernorshipName);
					} else if (impMonarch is not null) {
						GiveCountyToMonarch(title, ck3Country, impMonarch.Id);
					}
				}
			}

			void GiveCountyToMonarch(Title title, Title ck3Country, ulong impMonarchId) {
				var holderId = "imperator" + impMonarchId;
				if (Characters.TryGetValue(holderId, out var holder)) {
					title.ClearHolderSpecificHistory();
					title.SetHolderId(holder.Id, ck3Country.GetDateOfLastHolderChange());
				} else {
					Logger.Warn($"Holder {holderId} of county {title.Name} doesn't exist!");
				}
				title.DeFactoLiege = null;
			}

			void GiveCountyToGovernor(Title title, string ck3GovernorshipName) {
				var ck3Governorship = LandedTitles[ck3GovernorshipName];
				var holderChangeDate = ck3Governorship.GetDateOfLastHolderChange();
				var holderId = ck3Governorship.GetHolderId(holderChangeDate);
				if (Characters.TryGetValue(holderId, out var governor)) {
					title.ClearHolderSpecificHistory();
					title.SetHolderId(governor.Id, holderChangeDate);
				} else {
					Logger.Warn($"Holder {holderId} of county {title.Name} doesn't exist!");
				}
				title.DeFactoLiege = null;
			}
		}

		private void RemoveInvalidLandlessTitles(Date ck3BookmarkDate) {
			Logger.Info("Removing invalid landless titles.");
			var removedGeneratedTitles = new HashSet<string>();
			var revokedVanillaTitles = new HashSet<string>();

			HashSet<string> countyHoldersCache = GetCountyHolderIds(ck3BookmarkDate);

			foreach (var (name, title) in LandedTitles) {
				// if duchy/kingdom/empire title holder holds no county (is landless), remove the title
				// this also removes landless titles initialized from Imperator
				if (title.Rank != TitleRank.county && title.Rank != TitleRank.barony && !countyHoldersCache.Contains(title.GetHolderId(ck3BookmarkDate))) {
					if (!LandedTitles[name].Landless) { // does not have landless attribute set to true
						if (title.IsImportedOrUpdatedFromImperator && name.Contains("IMPTOCK3")) {
							removedGeneratedTitles.Add(name);
							LandedTitles.Remove(name);
						} else {
							revokedVanillaTitles.Add(name);
							title.ClearHolderSpecificHistory();
							title.DeFactoLiege = null;
						}
					}
				}
			}
			if (removedGeneratedTitles.Count > 0) {
				Logger.Debug("Found landless generated titles that can't be landless: " + string.Join(", ", removedGeneratedTitles));
			}
			if (revokedVanillaTitles.Count > 0) {
				Logger.Debug("Found landless vanilla titles that can't be landless: " + string.Join(", ", revokedVanillaTitles));
			}
		}

		private HashSet<string> GetCountyHolderIds(Date date) {
			var countyHoldersCache = new HashSet<string>();
			foreach (var county in LandedTitles.Values.Where(t => t.Rank == TitleRank.county)) {
				var holderId = county.GetHolderId(date);
				if (holderId != "0") {
					countyHoldersCache.Add(holderId);
				}
			}

			return countyHoldersCache;
		}

		private void ImportImperatorFamilies(Imperator.World impWorld) {
			Logger.Info("Importing Imperator Families.");

			// dynasties only holds dynasties converted from Imperator families, as vanilla ones aren't modified
			foreach (var family in impWorld.Families.Values) {
				if (family.Minor) {
					continue;
				}

				var newDynasty = new Dynasty(family, localizationMapper);
				Dynasties.Add(newDynasty.Id, newDynasty);
			}
			Logger.Info($"{Dynasties.Count} total families imported.");
		}

		private readonly CoaMapper coaMapper;
		private readonly CultureMapper cultureMapper = new();
		private readonly DeathReasonMapper deathReasonMapper = new();
		private readonly DefiniteFormMapper definiteFormMapper = new("configurables/definite_form_names.txt");
		private readonly GovernmentMapper governmentMapper = new();
		private readonly LocalizationMapper localizationMapper = new();
		private readonly NicknameMapper nicknameMapper = new("configurables/nickname_map.txt");
		private readonly ProvinceMapper provinceMapper = new();
		private readonly ReligionMapper religionMapper = new();
		private readonly SuccessionLawMapper successionLawMapper = new("configurables/succession_law_map.txt");
		private readonly TagTitleMapper tagTitleMapper = new("configurables/title_map.txt", "configurables/governorMappings.txt");
		private readonly TraitMapper traitMapper = new("configurables/trait_map.txt");
		private readonly CK3RegionMapper ck3RegionMapper;
		private readonly ImperatorRegionMapper imperatorRegionMapper;
		private readonly TitlesHistory titlesHistory;
	}
}
