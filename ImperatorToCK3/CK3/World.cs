﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CK3.Provinces;
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
using commonItems;
using ImperatorToCK3.Imperator.Countries;

namespace ImperatorToCK3.CK3 {
	public class World {
		public Dictionary<string, Character> Characters { get; } = new();
		public Dictionary<string, Dynasty> Dynasties { get; } = new();
		public Dictionary<ulong, Province> Provinces { get; } = new();
		private readonly LandedTitles landedTitles = new();
		public Dictionary<string, Title> LandedTitles {
			get {
				return landedTitles.StoredTitles;
			}
		}

		public World(Imperator.World impWorld, Configuration theConfiguration) {
			Logger.Info("*** Hello CK3, let's get painting. ***");
			// Scraping localizations from Imperator so we may know proper names for our countries.
			localizationMapper.ScrapeLocalizations(theConfiguration, impWorld.Mods);

			// Loading Imperator CoAs to use them for generated CK3 titles
			coaMapper = new CoaMapper(theConfiguration);

			// Load vanilla titles history
			var titlesHistoryPath = Path.Combine(theConfiguration.Ck3Path, "game/history/titles");
			titlesHistory = new TitlesHistory(titlesHistoryPath);

			// Loading vanilla CK3 landed titles
			var landedTitlesPath = Path.Combine(theConfiguration.Ck3Path, "game/common/landed_titles/00_landed_titles.txt");
			landedTitles.LoadTitles(landedTitlesPath);
			AddHistoryToVanillaTitles();

			// Loading regions
			ck3RegionMapper = new CK3RegionMapper(theConfiguration.Ck3Path, landedTitles);
			imperatorRegionMapper = new ImperatorRegionMapper(theConfiguration.ImperatorPath);
			// Use the region mappers in other mappers
			religionMapper.LoadRegionMappers(imperatorRegionMapper, ck3RegionMapper);
			cultureMapper.LoadRegionMappers(imperatorRegionMapper, ck3RegionMapper);

			ImportImperatorCountries(impWorld.Countries.StoredCountries);

			// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
			// Some of it will be overwritten, but not all.
			ImportVanillaProvinces(theConfiguration.Ck3Path);

			// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
			ImportImperatorProvinces(impWorld);

			ImportImperatorCharacters(impWorld, theConfiguration.ConvertBirthAndDeathDates, impWorld.EndDate);
			LinkSpouses();
			LinkMothersAndFathers();

			ImportImperatorFamilies(impWorld);

			OverWriteCountiesHistory();
			RemoveInvalidLandlessTitles();

			PurgeLandlessVanillaCharacters();
		}
		private void ImportImperatorCharacters(Imperator.World impWorld, bool convertBirthAndDeathDates, Date endDate) {
			Logger.Info("Importing Imperator Characters.");

			foreach (var character in impWorld.Characters.StoredCharacters.Values) {
				ImportImperatorCharacter(character, convertBirthAndDeathDates, endDate);
			}
			Logger.Info($"{Characters.Count} total characters recognized.");
		}

		private void ImportImperatorCharacter(
			Imperator.Characters.Character character,
			bool convertBirthAndDeathDates,
			Date endDate
		) {
			// Create a new CK3 character
			var newCharacter = new Character();
			newCharacter.InitializeFromImperator(character,
				religionMapper,
				cultureMapper,
				traitMapper,
				nicknameMapper,
				localizationMapper,
				provinceMapper,
				deathReasonMapper,
				convertBirthAndDeathDates,
				endDate
			);
			character.CK3Character = newCharacter;
			Characters.Add(newCharacter.ID, newCharacter);
		}

		private void ImportImperatorCountries(Dictionary<ulong, Country> imperatorCountries) {
			Logger.Info("Importing Imperator Countries.");

			// landedTitles holds all titles imported from CK3. We'll now overwrite some and
			// add new ones from Imperator tags.
			foreach (var title in imperatorCountries) {
				ImportImperatorCountry(title, imperatorCountries);
			}
			Logger.Info($"{LandedTitles.Count} total countries recognized.");
		}

		private void ImportImperatorCountry(
					KeyValuePair<ulong, Country> country,
					Dictionary<ulong, Country> imperatorCountries
		) {
			// Create a new title
			var newTitle = new Title();
			newTitle.InitializeFromTag(
				country.Value,
				imperatorCountries,
				localizationMapper,
				landedTitles,
				provinceMapper,
				coaMapper,
				tagTitleMapper,
				governmentMapper,
				successionLawMapper
			);

			var name = newTitle.Name;
			if (LandedTitles.TryGetValue(name, out var title)) {
				var vanillaTitle = title;
				vanillaTitle.UpdateFromTitle(newTitle);
				country.Value.CK3Title = vanillaTitle;
			} else {
				landedTitles.InsertTitle(newTitle);
				country.Value.CK3Title = newTitle;
			}
		}

		private void ImportVanillaProvinces(string ck3Path) {
			Logger.Info("Importing Vanilla Provinces.");
			// ---- Loading history/provinces
			var path = Path.Combine(ck3Path, "game/history/provinces");
			var fileNames = SystemUtils.GetAllFilesInFolderRecursive(path);
			foreach (var fileName in fileNames) {
				if (!fileName.EndsWith(".txt"))
					continue;
				var provincesPath = Path.Combine(ck3Path, "game/history/provinces", fileName);
				try {
					var newProvinces = new Provinces.Provinces(provincesPath);
					foreach (var (newProvinceID, newProvince) in newProvinces.StoredProvinces) {
						if (Provinces.ContainsKey(newProvinceID)) {
							Logger.Warn($"Vanilla province duplication - {newProvinceID} already loaded! Overwriting.");
						}
						Provinces[newProvinceID] = newProvince;
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
				if (!fileName.EndsWith(".txt"))
					continue;
				var provinceMappingsPath = Path.Combine(ck3Path, "game/history/province_mapping", fileName);
				try {
					var newProvinces = new ProvinceMappings(provinceMappingsPath);
					foreach (var (newProvinceID, baseProvinceID) in newProvinces.Mappings) {
						if (!Provinces.ContainsKey(baseProvinceID)) {
							Logger.Warn($"Base province {baseProvinceID} not found for province {newProvinceID}.");
							continue;
						}
						if (Provinces.ContainsKey(newProvinceID)) {
							Logger.Info($"Vanilla province duplication - {newProvinceID} already loaded! Preferring unique entry over mapping.");
						} else {
							var newProvince = new Province(newProvinceID, Provinces[baseProvinceID]);
							Provinces.Add(newProvinceID, newProvince);
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
			foreach (var (provinceID, province) in Provinces) {
				var impProvinces = provinceMapper.GetImperatorProvinceNumbers(provinceID);
				// Provinces we're not affecting will not be in this list.
				if (impProvinces.Count == 0)
					continue;
				// Next, we find what province to use as its initializing source.
				var sourceProvince = DetermineProvinceSource(impProvinces, impWorld);
				if (sourceProvince is null) {
					Logger.Warn($"Could not determine source province for CK3 province {provinceID}!");
					continue; // MISMAP, or simply have mod provinces loaded we're not using.
				} else {
					province.InitializeFromImperator(sourceProvince.Value.Value, cultureMapper, religionMapper);
				}
				// And finally, initialize it.
				++counter;
			}
			Logger.Info($"{impWorld.Provinces.StoredProvinces.Count} Imperator provinces imported into {counter} CK3 provinces.");
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

			foreach (var imperatorProvinceID in impProvinceNumbers) {
				if (impWorld.Provinces.StoredProvinces.TryGetValue(imperatorProvinceID, out var impProvince)) {
					var ownerID = impProvince.OwnerCountry.Key;
					if (!theClaims.ContainsKey(ownerID)) {
						theClaims[ownerID] = new();
					}
					theClaims[ownerID].Add(impProvince);

					var devValue = (int)impProvince.BuildingCount + impProvince.GetPopCount();
					theShares[ownerID] = devValue;
				} else {
					Logger.Warn($"Source province {imperatorProvinceID} is not on the list of known provinces!");
					continue; // Broken mapping, or loaded a mod changing provinces without using it.
				}
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
					toReturn = new(province.ID, province);
					maxDev = provinceWeight;
				}
			}
			if (toReturn.Key == 0 || toReturn.Value is null) {
				return null;
			}
			return toReturn;
		}

		private void AddHistoryToVanillaTitles() {
			foreach (var (name, title) in LandedTitles) {
				var historyOpt = titlesHistory.PopTitleHistory(name);
				if (historyOpt is not null)
					title.AddHistory(landedTitles, historyOpt);
			}
			// add vanilla development to counties
			// for counties that inherit development level from de jure lieges, assign it to them directly for better reliability
			foreach (var title in LandedTitles.Values) {
				if (title.Rank == TitleRank.county && title.DevelopmentLevel is null) {
					title.DevelopmentLevel = title.OwnOrInheritedDevelopmentLevel;
				}
			}
		}

		private void OverWriteCountiesHistory() {
			Logger.Info("Overwriting counties' history.");
			foreach (var title in LandedTitles.Values) {
				if (title.Rank == TitleRank.county && title.CapitalBaronyProvince > 0) { // title is a county and its capital province has a valid ID (0 is not a valid province in CK3)
					if (!Provinces.ContainsKey(title.CapitalBaronyProvince)) {
						Logger.Warn($"Capital barony province not found {title.CapitalBaronyProvince}");
					} else {
						var ck3CapitalBaronyProvince = Provinces[title.CapitalBaronyProvince];
						var impProvince = ck3CapitalBaronyProvince.ImperatorProvince;
						if (impProvince is not null) {
							var impCountry = impProvince.OwnerCountry.Value;
							if (impCountry is not null && impCountry.CountryType != CountryType.rebels) {
								var impMonarch = impCountry.Monarch;
								if (impMonarch is not null) {
									if (Characters.TryGetValue("imperator" + impMonarch.ToString(), out var holder)) {
										title.Holder = holder;
									}
									title.DeFactoLiege = null;
									countyHoldersCache.Add(title.HolderID);
								}
							} else { // e.g. uncolonised Imperator province
								title.Holder = null;
								title.DeFactoLiege = null;
							}
						} else { // county is probably outside of Imperator map
							if (!string.IsNullOrEmpty(title.HolderID) && title.HolderID != "0") {
								countyHoldersCache.Add(title.HolderID);
							}
						}
					}
				}
			}
		}

		private void RemoveInvalidLandlessTitles() {
			Logger.Info("Removing invalid landless titles.");
			var removedGeneratedTitles = new HashSet<string>();
			var revokedVanillaTitles = new HashSet<string>();

			foreach (var (name, title) in LandedTitles) {
				//important check: if duchy/kingdom/empire title holder holds no county (is landless), remove the title
				// this also removes landless titles initialized from Imperator
				if (title.Rank != TitleRank.county && title.Rank != TitleRank.barony && !countyHoldersCache.Contains(title.HolderID)) {
					if (!LandedTitles[name].Landless) { // does not have landless attribute set to true
						if (title.IsImportedOrUpdatedFromImperator && name.IndexOf("IMPTOCK3") != -1) {
							removedGeneratedTitles.Add(name);
							landedTitles.EraseTitle(name);
						} else {
							revokedVanillaTitles.Add(name);
							title.Holder = null;
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

		private void PurgeLandlessVanillaCharacters() {
			var farewellIds = new HashSet<string>(Characters.Keys);
			foreach (var id in farewellIds) {
				if (id.StartsWith("imperator")) {
					farewellIds.Remove(id);
				}
			}
			foreach (var title in LandedTitles.Values) {
				farewellIds.Remove(title.HolderID);
			}

			foreach (var characterId in farewellIds) {
				Characters[characterId].BreakAllLinks();
				Characters.Remove(characterId);
			}
			Logger.Info($"Purged {farewellIds.Count} landless vanilla characters.");
		}

		private void LinkSpouses() {
			var spouseCounter = 0;
			foreach (var ck3Character in Characters.Values) {
				var newSpouses = new Dictionary<ulong, Character>();
				// make links between Imperator characters
				foreach (var impSpouseCharacter in ck3Character.ImperatorCharacter.Spouses.Values) {
					if (impSpouseCharacter is not null) {
						var ck3SpouseCharacter = impSpouseCharacter.CK3Character;
						ck3Character.Spouses[ck3SpouseCharacter.ID] = ck3SpouseCharacter;
						ck3SpouseCharacter.Spouses[ck3Character.ID] = ck3Character;
						++spouseCounter;
					}
				}
			}
			Logger.Info($"{spouseCounter} spouses linked in CK3.");
		}

		private void LinkMothersAndFathers() {
			var motherCounter = 0;
			var fatherCounter = 0;
			foreach (var ck3Character in Characters.Values) {
				// make links between Imperator characters
				var impMotherCharacter = ck3Character.ImperatorCharacter.Mother.Value;
				if (impMotherCharacter is not null) {
					var ck3MotherCharacter = impMotherCharacter.CK3Character;
					ck3Character.Mother = ck3MotherCharacter;
					ck3MotherCharacter.Children[ck3Character.ID] = ck3Character;
					++motherCounter;
				}

				// make links between Imperator characters
				var impFatherCharacter = ck3Character.ImperatorCharacter.Father.Value;
				if (impFatherCharacter is not null) {
					var ck3FatherCharacter = impFatherCharacter.CK3Character;
					ck3Character.Father = ck3FatherCharacter;
					ck3FatherCharacter.Children[ck3Character.ID] = ck3Character;
					++fatherCounter;
				}
			}
			Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked in CK3.");
		}

		private void ImportImperatorFamilies(Imperator.World impWorld) {
			Logger.Info("Importing Imperator Families.");

			// dynasties only holds dynasties converted from Imperator families, as vanilla ones aren't modified
			foreach (var family in impWorld.Families.StoredFamilies.Values) {
				if (family.Minor)
					continue;

				var newDynasty = new Dynasty(family, localizationMapper);
				Dynasties.Add(newDynasty.ID, newDynasty);
			}
			Logger.Info($"{Dynasties.Count} total families imported.");
		}

		private readonly CoaMapper coaMapper;
		private readonly CultureMapper cultureMapper = new();
		private readonly DeathReasonMapper deathReasonMapper = new();
		private readonly GovernmentMapper governmentMapper = new();
		private readonly LocalizationMapper localizationMapper = new();
		private readonly NicknameMapper nicknameMapper = new("configurables/nickname_map.txt");
		private readonly ProvinceMapper provinceMapper = new();
		private readonly ReligionMapper religionMapper = new();
		private readonly SuccessionLawMapper successionLawMapper = new("configurables/succession_law_map.txt");
		private readonly TagTitleMapper tagTitleMapper = new("configurables/title_map.txt");
		private readonly TraitMapper traitMapper = new("configurables/trait_map.txt");
		private readonly CK3RegionMapper ck3RegionMapper;
		private readonly ImperatorRegionMapper imperatorRegionMapper;
		private readonly TitlesHistory titlesHistory;

		private readonly HashSet<string> countyHoldersCache = new(); // used by RemoveInvalidLandlessTitles
	}
}
