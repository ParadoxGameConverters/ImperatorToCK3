using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using Open.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.CK3.Titles;

public partial class Title {
	private readonly LandedTitles parentCollection;

	// This is a recursive class that scrapes common/landed_titles looking for title colors, landlessness,
	// and most importantly relation between baronies and barony provinces so we can link titles to actual clay.
	// Since titles are nested according to hierarchy we do this recursively.
	public class LandedTitles : TitleCollection {
		public Dictionary<string, object> Variables { get; } = new();

		public void LoadTitles(ModFilesystem ck3ModFS) {
			Logger.Info("Loading landed titles...");

			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseGameFolder("common/landed_titles", ck3ModFS, "txt", recursive: true, logFilePaths: true);
			LogIgnoredTokens();

			Logger.IncrementProgress();
		}
		public void LoadTitles(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);

			LogIgnoredTokens();
		}
		public void LoadStaticTitles() {
			Logger.Info("Loading static landed titles...");

			var parser = new Parser();
			RegisterKeys(parser);

			parser.ParseFile("configurables/static_landed_titles.txt");

			LogIgnoredTokens();

			Logger.IncrementProgress();
		}
		public void LoadStaticTitles(BufferedReader reader) {
			Logger.Info("Loading static landed titles...");

			var parser = new Parser();
			RegisterKeys(parser);

			parser.ParseStream(reader);

			LogIgnoredTokens();

			Logger.IncrementProgress();
		}

		public void CarveTitles(LandedTitles overrides) {
			// merge in new king and empire titles into this from overrides, overriding duplicates
			foreach (var overrideTitle in overrides.Where(t => t.Rank > TitleRank.duchy)) {
				// inherit vanilla vassals
				TryGetValue(overrideTitle.Id, out Title? vanillaTitle);
				AddOrReplace(new Title(vanillaTitle, overrideTitle, this));
			}

			// update duchies to correct de jure liege, remove de jure titles that lose all de jure vassals
			foreach (var title in overrides.Where(t => t.Rank == TitleRank.duchy)) {
				var duchy = this[title.Id];
				if (duchy.DeJureLiege is not null) {
					if (duchy.DeJureLiege.DeJureVassals.Count <= 1) {
						duchy.DeJureLiege.DeJureLiege = null;
					}
				}
				duchy.DeJureLiege = title.DeJureLiege;
			}
		}

		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Variable, (reader, variableName) => {
				var variableValue = reader.GetString();
				Variables[variableName[1..]] = variableValue;
			});
			parser.RegisterRegex(Regexes.TitleId, (reader, titleNameStr) => {
				// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
				var newTitle = Add(titleNameStr);
				newTitle.LoadTitles(reader);
			});
			parser.IgnoreAndLogUnregisteredItems();
		}

		private static void LogIgnoredTokens() {
			Logger.Debug($"Ignored Title tokens: {Title.IgnoredTokens}");
		}

		public Title Add(string id) {
			if (string.IsNullOrEmpty(id)) {
				throw new ArgumentException("Not inserting a Title with empty id!");
			}

			var newTitle = new Title(this, id);
			dict[newTitle.Id] = newTitle;
			return newTitle;
		}

		public Title Add(
			Country country,
			Dependency? dependency,
			CountryCollection imperatorCountries,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			TagTitleMapper tagTitleMapper,
			GovernmentMapper governmentMapper,
			SuccessionLawMapper successionLawMapper,
			DefiniteFormMapper definiteFormMapper,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			CharacterCollection characters,
			Date conversionDate,
			Configuration config
		) {
			var newTitle = new Title(this,
				country,
				dependency,
				imperatorCountries,
				locDB,
				provinceMapper,
				coaMapper,
				tagTitleMapper,
				governmentMapper,
				successionLawMapper,
				definiteFormMapper,
				religionMapper,
				cultureMapper,
				nicknameMapper,
				characters,
				conversionDate,
				config
			);
			dict[newTitle.Id] = newTitle;
			return newTitle;
		}

		public Title Add(
			string id,
			Governorship governorship,
			Country country,
			Imperator.Provinces.ProvinceCollection irProvinces,
			Imperator.Characters.CharacterCollection imperatorCharacters,
			bool regionHasMultipleGovernorships,
			bool staticDeJure,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			DefiniteFormMapper definiteFormMapper,
			ImperatorRegionMapper imperatorRegionMapper
		) {
			var newTitle = new Title(this,
				id,
				governorship,
				country,
				irProvinces,
				imperatorCharacters,
				regionHasMultipleGovernorships,
				staticDeJure,
				locDB,
				provinceMapper,
				coaMapper,
				definiteFormMapper,
				imperatorRegionMapper
			);
			dict[newTitle.Id] = newTitle;
			return newTitle;
		}
		public override void Remove(string name) {
			if (dict.TryGetValue(name, out var titleToErase)) {
				var deJureLiege = titleToErase.DeJureLiege;
				deJureLiege?.DeJureVassals.Remove(name);

				foreach (var vassal in titleToErase.DeJureVassals) {
					vassal.DeJureLiege = null;
				}

				foreach (var title in this) {
					title.RemoveDeFactoLiegeReferences(name);
				}

				if (titleToErase.ImperatorCountry is not null) {
					titleToErase.ImperatorCountry.CK3Title = null;
				}
			}
			dict.Remove(name);
		}
		public Title? GetCountyForProvince(ulong provinceId) {
			foreach (var county in this.Where(title => title.Rank == TitleRank.county)) {
				if (county.CountyProvinceIds.Contains(provinceId)) {
					return county;
				}
			}
			return null;
		}

		public Title? GetBaronyForProvince(ulong provinceId) {
			var baronies = this.Where(title => title.Rank == TitleRank.barony);
			return baronies.FirstOrDefault(b => provinceId == b?.ProvinceId, defaultValue: null);
		}

		public HashSet<string> GetHolderIds(Date date) {
			return new HashSet<string>(this.Select(t => t.GetHolderId(date)));
		}
		public HashSet<string> GetAllHolderIds() {
			return this.SelectMany(t => t.GetAllHolderIds()).ToHashSet();
		}

		public void ImportImperatorCountries(
			CountryCollection imperatorCountries,
			IReadOnlyCollection<Dependency> dependencies,
			TagTitleMapper tagTitleMapper,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			GovernmentMapper governmentMapper,
			SuccessionLawMapper successionLawMapper,
			DefiniteFormMapper definiteFormMapper,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			CharacterCollection characters,
			Date conversionDate,
			Configuration config,
			List<KeyValuePair<Country, Dependency?>> countyLevelCountries
		) {
			Logger.Info("Importing Imperator countries...");

			// landedTitles holds all titles imported from CK3. We'll now overwrite some and
			// add new ones from Imperator tags.
			int counter = 0;
			
			// We don't need pirates, barbarians etc.
			var realCountries = imperatorCountries.Where(c => c.CountryType == CountryType.real).ToImmutableList();
			
			// Import independent countries first, then subjects.
			var independentCountries = realCountries.Where(c => dependencies.All(d => d.SubjectId != c.Id)).ToImmutableList();
			var subjects = realCountries.Except(independentCountries).ToImmutableList();
			
			foreach (var country in independentCountries) {
				ImportImperatorCountry(
					country,
					dependency: null,
					imperatorCountries,
					tagTitleMapper,
					locDB,
					provinceMapper,
					coaMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					characters,
					conversionDate,
					config,
					countyLevelCountries
				);
				++counter;
			}
			foreach (var country in subjects) {
				ImportImperatorCountry(
					country,
					dependency: dependencies.FirstOrDefault(d => d.SubjectId == country.Id),
					imperatorCountries,
					tagTitleMapper,
					locDB,
					provinceMapper,
					coaMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					characters,
					conversionDate,
					config,
					countyLevelCountries
				);
				++counter;
			}
			Logger.Info($"Imported {counter} countries from I:R.");
		}

		private void ImportImperatorCountry(
			Country country,
			Dependency? dependency,
			CountryCollection imperatorCountries,
			TagTitleMapper tagTitleMapper,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			GovernmentMapper governmentMapper,
			SuccessionLawMapper successionLawMapper,
			DefiniteFormMapper definiteFormMapper,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			CharacterCollection characters,
			Date conversionDate,
			Configuration config,
			List<KeyValuePair<Country, Dependency?>> countyLevelCountries) {
			// Create a new title or update existing title.
			var titleId = DetermineId(country, dependency, imperatorCountries, tagTitleMapper, locDB);

			if (GetRankForId(titleId) == TitleRank.county) {
				countyLevelCountries.Add(new(country, dependency));
				Logger.Debug($"Country {country.Id} can only be converted as county level.");
				return;
			}

			if (TryGetValue(titleId, out var existingTitle)) {
				existingTitle.InitializeFromTag(
					country,
					dependency,
					imperatorCountries,
					locDB,
					provinceMapper,
					coaMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					characters,
					conversionDate,
					config
				);
			} else {
				Add(
					country,
					dependency,
					imperatorCountries,
					locDB,
					provinceMapper,
					coaMapper,
					tagTitleMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					characters,
					conversionDate,
					config
				);
			}
		}

		public void ImportImperatorGovernorships(
			Imperator.World irWorld,
			ProvinceCollection ck3Provinces,
			TagTitleMapper tagTitleMapper,
			LocDB locDB,
			Configuration config,
			ProvinceMapper provinceMapper,
			DefiniteFormMapper definiteFormMapper,
			ImperatorRegionMapper imperatorRegionMapper,
			CoaMapper coaMapper,
			List<Governorship> countyLevelGovernorships
		) {
			Logger.Info("Importing Imperator Governorships...");

			var governorships = irWorld.JobsDB.Governorships;
			var governorshipsPerRegion = governorships.GroupBy(g => g.Region.Id)
				.ToDictionary(g => g.Key, g => g.Count());

			// landedTitles holds all titles imported from CK3. We'll now overwrite some and
			// add new ones from Imperator governorships.
			var counter = 0;
			foreach (var governorship in governorships) {
				ImportImperatorGovernorship(
					governorship,
					this,
					ck3Provinces,
					irWorld.Provinces,
					irWorld.Characters,
					governorshipsPerRegion[governorship.Region.Id] > 1,
					config.StaticDeJure,
					tagTitleMapper,
					locDB,
					provinceMapper,
					definiteFormMapper,
					imperatorRegionMapper,
					coaMapper,
					countyLevelGovernorships
				);
				++counter;
			}
			Logger.Info($"Imported {counter} governorships from I:R.");
			Logger.IncrementProgress();
		}
		private void ImportImperatorGovernorship(
			Governorship governorship,
			LandedTitles titles,
			ProvinceCollection ck3Provinces,
			Imperator.Provinces.ProvinceCollection irProvinces,
			Imperator.Characters.CharacterCollection imperatorCharacters,
			bool regionHasMultipleGovernorships,
			bool staticDeJure,
			TagTitleMapper tagTitleMapper,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			DefiniteFormMapper definiteFormMapper,
			ImperatorRegionMapper imperatorRegionMapper,
			CoaMapper coaMapper,
			ICollection<Governorship> countyLevelGovernorships
		) {
			var country = governorship.Country;

			var id = DetermineId(governorship, titles, irProvinces, ck3Provinces, imperatorRegionMapper, tagTitleMapper, provinceMapper);
			if (id is null) {
				Logger.Warn($"Cannot convert {governorship.Region.Id} of country {country.Id}");
				return;
			}

			if (GetRankForId(id) == TitleRank.county) {
				countyLevelGovernorships.Add(governorship);
				return;
			}

			// Create a new title or update existing title
			if (TryGetValue(id, out var existingTitle)) {
				existingTitle.InitializeFromGovernorship(
					governorship,
					country,
					irProvinces,
					imperatorCharacters,
					regionHasMultipleGovernorships,
					staticDeJure,
					locDB,
					provinceMapper,
					definiteFormMapper,
					imperatorRegionMapper
				);
			} else {
				Add(
					id,
					governorship,
					country,
					irProvinces,
					imperatorCharacters,
					regionHasMultipleGovernorships,
					staticDeJure,
					locDB,
					provinceMapper,
					coaMapper,
					definiteFormMapper,
					imperatorRegionMapper
				);
			}
		}

		public void ImportImperatorHoldings(ProvinceCollection ck3Provinces, Imperator.Characters.CharacterCollection irCharacters, Date conversionDate) {
			Logger.Info("Importing Imperator holdings...");
			var counter = 0;
			
			var titlesThatHaveHolders = this
				.Where(t => t.Rank >= TitleRank.duchy && t.GetHolderId(conversionDate) != "0")
				.ToImmutableList();
			var titleCapitalBaronyIds = titlesThatHaveHolders
				.Select(t=>t.CapitalCounty?.CapitalBaronyId ?? t.CapitalBaronyId)
				.ToImmutableHashSet();
			
			// Dukes and above should be excluded from having their holdings converted.
			// Otherwise, governors with holdings would own parts of other governorships.
			var dukeAndAboveIds = titlesThatHaveHolders
				.Where(t => t.Rank >= TitleRank.duchy)
				.Select(t => t.GetHolderId(conversionDate))
				.ToImmutableHashSet();
			
			var baronies = this.Where(t => t.Rank == TitleRank.barony).ToImmutableHashSet();
			var countyCapitalBaronies = baronies
				.Where(b => b.DeJureLiege?.CapitalBaronyId == b.Id)
				.ToImmutableHashSet();
			
			var eligibleBaronies = baronies
				.Where(b => !titleCapitalBaronyIds.Contains(b.Id))
				.ToImmutableHashSet();

			foreach (var barony in eligibleBaronies) {
				var ck3ProvinceId = barony.ProvinceId;
				if (ck3ProvinceId is null) {
					continue;
				}
				if (!ck3Provinces.TryGetValue(ck3ProvinceId.Value, out var ck3Province)) {
					continue;
				}

				// Skip none holdings and temple holdings.
				if (ck3Province.GetHoldingType(conversionDate) is "church_holding" or "none") {
					continue;
				}

				var irProvince = ck3Province.PrimaryImperatorProvince;
				var holdingOwnerId = irProvince?.HoldingOwnerId;
				if (holdingOwnerId is null) {
					continue;
				}

				var irOwner = irCharacters[holdingOwnerId.Value];
				var ck3Owner = irOwner.CK3Character;
				if (ck3Owner is null) {
					continue;
				}
				if (dukeAndAboveIds.Contains(ck3Owner.Id)) {
					continue;
				}
				
				var realm = ck3Owner.ImperatorCharacter?.HomeCountry?.CK3Title;
				var deFactoLiege = realm;
				if (realm is not null) {
					var deJureDuchy = barony.DeJureLiege?.DeJureLiege;
					if (deJureDuchy is not null && deJureDuchy.GetHolderId(conversionDate) != "0" && deJureDuchy.GetTopRealm(conversionDate) == realm) {
						deFactoLiege = deJureDuchy;
					} else {
						var deJureKingdom = deJureDuchy?.DeJureLiege;
						if (deJureKingdom is not null && deJureKingdom.GetHolderId(conversionDate) != "0" && deJureKingdom.GetTopRealm(conversionDate) == realm) {
							deFactoLiege = deJureKingdom;
						}
					}
				}
				if (countyCapitalBaronies.Contains(barony)) {
					// If barony is a county capital, set the county holder to the holding owner.
					var county = barony.DeJureLiege;
					if (county is null) {
						Logger.Warn($"County capital barony {barony.Id} has no de jure county!");
						continue;
					}
					county.SetHolder(ck3Owner, conversionDate);
					county.SetDeFactoLiege(deFactoLiege, conversionDate);
				} else {
					barony.SetHolder(ck3Owner, conversionDate);
					// No need to set de facto liege for baronies, they are tied to counties.
				}
				++counter;
			}
			Logger.Info($"Imported {counter} holdings from I:R.");
			Logger.IncrementProgress();
		}

		public void RemoveInvalidLandlessTitles(Date ck3BookmarkDate) {
			Logger.Info("Removing invalid landless titles...");
			var removedGeneratedTitles = new HashSet<string>();
			var revokedVanillaTitles = new HashSet<string>();

			HashSet<string> countyHoldersCache = GetCountyHolderIds(ck3BookmarkDate);

			foreach (var title in this) {
				// If duchy/kingdom/empire title holder holds no counties, revoke the title.
				// In case of titles created from Imperator, completely remove them.
				if (title.Rank <= TitleRank.county) {
					continue;
				}
				if (countyHoldersCache.Contains(title.GetHolderId(ck3BookmarkDate))) {
					continue;
				}

				// Check if the title has "landless = yes" attribute.
				// If it does, it should be always kept.
				var id = title.Id;
				if (this[id].Landless) {
					continue;
				}

				if (title.IsCreatedFromImperator) {
					removedGeneratedTitles.Add(id);
					Remove(id);
				} else {
					revokedVanillaTitles.Add(id);
					title.ClearHolderSpecificHistory();
					title.SetDeFactoLiege(null, ck3BookmarkDate);
				}
			}
			if (removedGeneratedTitles.Count > 0) {
				Logger.Debug($"Found landless generated titles that can't be landless: {string.Join(", ", removedGeneratedTitles)}");
			}
			if (revokedVanillaTitles.Count > 0) {
				Logger.Debug($"Found landless vanilla titles that can't be landless: {string.Join(", ", revokedVanillaTitles)}");
			}

			Logger.IncrementProgress();
		}

		private void SetDeJureKingdoms(Date ck3BookmarkDate) {
			Logger.Info("Setting de jure kingdoms...");

			var duchies = this.Where(t => t.Rank == TitleRank.duchy).ToHashSet();
			var duchiesWithDeJureVassals = duchies.Where(d => d.DeJureVassals.Count > 0).ToHashSet();

			foreach (var duchy in duchiesWithDeJureVassals) {
				// If capital county belongs to an empire and contains the empire's capital,
				// create a kingdom from the duchy and make the empire a de jure liege of the kingdom.
				var capitalEmpireRealm = duchy.CapitalCounty?.GetRealmOfRank(TitleRank.empire, ck3BookmarkDate);
				var duchyCounties = duchy.GetDeJureVassalsAndBelow("c").Values;
				if (capitalEmpireRealm is not null && duchyCounties.Any(c => c.Id == capitalEmpireRealm.CapitalCountyId)) {
					var kingdom = Add("k_IRTOCK3_kingdom_from_" + duchy.Id);
					kingdom.Color1 = duchy.Color1;
					kingdom.CapitalCounty = duchy.CapitalCounty;

					var kingdomNameLoc = kingdom.Localizations.AddLocBlock(kingdom.Id);
					kingdomNameLoc.ModifyForEveryLanguage(
						(orig, language) => $"${duchy.Id}$"
					);
					
					var kingdomAdjLoc = kingdom.Localizations.AddLocBlock(kingdom.Id + "_adj");
					kingdomAdjLoc.ModifyForEveryLanguage(
						(orig, language) => $"${duchy.Id}_adj$"
					);
					
					kingdom.DeJureLiege = capitalEmpireRealm;
					duchy.DeJureLiege = kingdom;
					continue;
				}
				
				// If capital county belongs to a kingdom, make the kingdom a de jure liege of the duchy.
				var capitalKingdomRealm = duchy.CapitalCounty?.GetRealmOfRank(TitleRank.kingdom, ck3BookmarkDate);
				if (capitalKingdomRealm is not null) {
					duchy.DeJureLiege = capitalKingdomRealm;
					continue;
				}

				// Otherwise, use the kingdom that owns the biggest percentage of the duchy.
				var kingdomRealmShares = new Dictionary<string, int>(); // realm, number of provinces held in duchy
				foreach (var county in duchyCounties) {
					var kingdomRealm = county.GetRealmOfRank(TitleRank.kingdom, ck3BookmarkDate);
					if (kingdomRealm is null) {
						continue;
					}
					kingdomRealmShares.TryGetValue(kingdomRealm.Id, out int currentCount);
					kingdomRealmShares[kingdomRealm.Id] = currentCount + county.CountyProvinceIds.Count();
				}

				if (kingdomRealmShares.Count > 0) {
					var biggestShare = kingdomRealmShares.MaxBy(pair => pair.Value);
					duchy.DeJureLiege = this[biggestShare.Key];
				}
			}

			// Duchies without de jure vassals should not be de jure part of any kingdom.
			var duchiesWithoutDeJureVassals = duchies.Except(duchiesWithDeJureVassals);
			foreach (var duchy in duchiesWithoutDeJureVassals) {
				Logger.Debug($"Duchy {duchy.Id} has no de jure vassals. Removing de jure liege.");
				duchy.DeJureLiege = null;
			}

			Logger.IncrementProgress();
		}

		private void SetDeJureEmpires(CultureCollection ck3Cultures, CharacterCollection ck3Characters, MapData ck3MapData, Date ck3BookmarkDate) {
			Logger.Info("Setting de jure empires...");
			var deJureKingdoms = GetDeJureKingdoms();
			
			// Try to assign kingdoms to existing empires.
			foreach (var kingdom in deJureKingdoms) {
				var empireShares = new Dictionary<string, int>();
				var kingdomProvincesCount = 0;
				foreach (var county in kingdom.GetDeJureVassalsAndBelow("c").Values) {
					var countyProvincesCount = county.CountyProvinceIds.Count();
					kingdomProvincesCount += countyProvincesCount;

					var empireRealm = county.GetRealmOfRank(TitleRank.empire, ck3BookmarkDate);
					if (empireRealm is null) {
						continue;
					}

					empireShares.TryGetValue(empireRealm.Id, out var currentCount);
					empireShares[empireRealm.Id] = currentCount + countyProvincesCount;
				}

				kingdom.DeJureLiege = null;
				if (empireShares.Count == 0) {
					continue;
				}

				(string empireId, int share) = empireShares.MaxBy(pair => pair.Value);
				// The potential de jure empire must hold at least 50% of the kingdom.
				if (share < (kingdomProvincesCount * 0.50)) {
					continue;
				}

				kingdom.DeJureLiege = this[empireId];
			}

			// For kingdoms that still have no de jure empire, create empires based on dominant culture of the realms
			// holding land in that de jure kingdom.
			var removableEmpireIds = new HashSet<string>();
			var kingdomToDominantHeritagesDict = new Dictionary<string, ImmutableArray<Pillar>>();
			var heritageToEmpireDict = GetHeritageIdToExistingTitleDict();
			CreateEmpiresBasedOnDominantHeritages(deJureKingdoms, ck3Cultures, ck3Characters, removableEmpireIds, kingdomToDominantHeritagesDict, heritageToEmpireDict, ck3BookmarkDate);
			
			Logger.Debug("Building kingdom adjacencies dict...");
			// Create a cache of province IDs per kingdom.
			var provincesPerKingdomDict = deJureKingdoms
				.ToDictionary(
					k => k.Id,
					k => k.GetDeJureVassalsAndBelow("c").Values.SelectMany(c => c.CountyProvinceIds).ToHashSet()
				);
			var kingdomAdjacenciesByLand = deJureKingdoms.ToDictionary(k => k.Id, _ => new ConcurrentHashSet<string>());
			var kingdomAdjacenciesByWaterBody = deJureKingdoms.ToDictionary(k => k.Id, _ => new ConcurrentHashSet<string>());
			Parallel.ForEach(deJureKingdoms, kingdom => {
				FindKingdomsAdjacentToKingdom(ck3MapData, deJureKingdoms, kingdom.Id, provincesPerKingdomDict, kingdomAdjacenciesByLand, kingdomAdjacenciesByWaterBody);
			});
			
			SplitDisconnectedEmpires(kingdomAdjacenciesByLand, kingdomAdjacenciesByWaterBody, removableEmpireIds, kingdomToDominantHeritagesDict, heritageToEmpireDict, ck3BookmarkDate);
			
			SetEmpireCapitals(ck3BookmarkDate);
		}

		private void CreateEmpiresBasedOnDominantHeritages(
			IReadOnlyCollection<Title> deJureKingdoms,
			CultureCollection ck3Cultures,
			CharacterCollection ck3Characters,
			HashSet<string> removableEmpireIds,
			IDictionary<string, ImmutableArray<Pillar>> kingdomToDominantHeritagesDict,
			Dictionary<string, Title> heritageToEmpireDict,
			Date ck3BookmarkDate
		) {
			var kingdomsWithoutEmpire = deJureKingdoms
				.Where(k => k.DeJureLiege is null)
				.ToImmutableArray();

			foreach (var kingdom in kingdomsWithoutEmpire) {
				var counties = kingdom.GetDeJureVassalsAndBelow("c").Values;
				
				// Get list of dominant heritages in the kingdom, in descending order.
				var dominantHeritages = counties
					.Select(c => new { County = c, HolderId = c.GetHolderId(ck3BookmarkDate)})
					.Select(x => new { x.County, Holder = ck3Characters.TryGetValue(x.HolderId, out var holder) ? holder : null})
					.Select(x => new { x.County, CultureId = x.Holder?.GetCultureId(ck3BookmarkDate) })
					.Where(x => x.CultureId is not null)
					.Select(x => new { x.County, Culture = ck3Cultures.TryGetValue(x.CultureId!, out var culture) ? culture : null })
					.Where(x => x.Culture is not null)
					.Select(x => new { x.County, x.Culture!.Heritage })
					.GroupBy(x => x.Heritage)
					.OrderByDescending(g => g.Count())
					.Select(g => g.Key)
					.ToImmutableArray();
				if (dominantHeritages.Length == 0) {
					if (kingdom.GetDeJureVassalsAndBelow("c").Count > 0) {
						Logger.Warn($"Kingdom {kingdom.Id} has no dominant heritage!");
					}
					continue;
				}
				kingdomToDominantHeritagesDict[kingdom.Id] = dominantHeritages;

				var dominantHeritage = dominantHeritages.First();

				if (heritageToEmpireDict.TryGetValue(dominantHeritage.Id, out var empire)) {
					kingdom.DeJureLiege = empire;
				} else {
					// Create new de jure empire based on heritage.
					var heritageEmpire = CreateEmpireForHeritage(dominantHeritage, ck3Cultures);
					removableEmpireIds.Add(heritageEmpire.Id);
					
					kingdom.DeJureLiege = heritageEmpire;
					heritageToEmpireDict[dominantHeritage.Id] = heritageEmpire;
				}
			}
		}

		private static void FindKingdomsAdjacentToKingdom(
			MapData ck3MapData,
			IReadOnlyCollection<Title> deJureKingdoms,
			string kingdomId, Dictionary<string, HashSet<ulong>> provincesPerKingdomDict,
			Dictionary<string, ConcurrentHashSet<string>> kingdomAdjacenciesByLand,
			Dictionary<string, ConcurrentHashSet<string>> kingdomAdjacenciesByWaterBody)
		{
			foreach (var otherKingdom in deJureKingdoms) {
				// Since this code is parallelized, make sure we don't check the same pair twice.
				// Also make sure we don't check the same kingdom against itself.
				if (kingdomId.CompareTo(otherKingdom.Id) >= 0) {
					continue;
				}
				
				var kingdom1Provinces = provincesPerKingdomDict[kingdomId];
				var kingdom2Provinces = provincesPerKingdomDict[otherKingdom.Id];
				if (AreTitlesAdjacentByLand(kingdom1Provinces, kingdom2Provinces, ck3MapData)) {
					kingdomAdjacenciesByLand[kingdomId].Add(otherKingdom.Id);
					kingdomAdjacenciesByLand[otherKingdom.Id].Add(kingdomId);
				} else if (AreTitlesAdjacentByWaterBody(kingdom1Provinces, kingdom2Provinces, ck3MapData)) {
					kingdomAdjacenciesByWaterBody[kingdomId].Add(otherKingdom.Id);
					kingdomAdjacenciesByWaterBody[otherKingdom.Id].Add(kingdomId);
				}
			}
		}

		private Dictionary<string, Title> GetHeritageIdToExistingTitleDict() {
			var heritageToEmpireDict = new Dictionary<string, Title>();

			var reader = new BufferedReader(File.ReadAllText("configurables/heritage_empires_map.txt"));
			foreach (var (heritageId, empireId) in reader.GetAssignments()) {
				if (heritageToEmpireDict.ContainsKey(heritageId)) {
					continue;
				}
				if (!TryGetValue(empireId, out var empire)) {
					continue;
				}
				if (empire.Rank != TitleRank.empire) {
					continue;
				}
				
				heritageToEmpireDict[heritageId] = empire;
				Logger.Debug($"Mapped heritage {heritageId} to empire {empireId}.");
			}
			
			return heritageToEmpireDict;
		}

		private Title CreateEmpireForHeritage(Pillar heritage, CultureCollection ck3Cultures) {
			var newEmpireId = $"e_IRTOCK3_heritage_{heritage.Id}";
			var newEmpire = Add(newEmpireId);
			var nameLocBlock = newEmpire.Localizations.AddLocBlock(newEmpire.Id);
			nameLocBlock[ConverterGlobals.PrimaryLanguage] = $"${heritage.Id}_name$ Empire";
			var adjectiveLocBlock = newEmpire.Localizations.AddLocBlock($"{newEmpire.Id}_adj");
			adjectiveLocBlock[ConverterGlobals.PrimaryLanguage] = $"${heritage.Id}_name$";
			newEmpire.HasDefiniteForm = true;

			// Use color of one of the cultures as the empire color.
			var empireColor = ck3Cultures.First(c => c.Heritage == heritage).Color;
			newEmpire.Color1 = empireColor;
			
			return newEmpire;
		}

		private void SplitDisconnectedEmpires(
			IDictionary<string, ConcurrentHashSet<string>> kingdomAdjacenciesByLand,
			IDictionary<string, ConcurrentHashSet<string>> kingdomAdjacenciesByWaterBody,
			HashSet<string> removableEmpireIds,
			IDictionary<string, ImmutableArray<Pillar>> kingdomToDominantHeritagesDict,
			Dictionary<string, Title> heritageToEmpireDict,
			Date date
		) {
			Logger.Debug("Splitting disconnected empires...");
			
			// Combine kingdom adjacencies by land and water body into a single dictionary.
			var kingdomAdjacencies = new Dictionary<string, HashSet<string>>();
			foreach (var (kingdomId, adjacencies) in kingdomAdjacenciesByLand) {
				kingdomAdjacencies[kingdomId] = [..adjacencies];
			}
			foreach (var (kingdomId, adjacencies) in kingdomAdjacenciesByWaterBody) {
				if (!kingdomAdjacencies.TryGetValue(kingdomId, out var set)) {
					set = [];
					kingdomAdjacencies[kingdomId] = set;
				}
				set.UnionWith(adjacencies);
			}
			
			// If one separated kingdom is separated from the rest of its de jure empire, try to get the second dominant heritage in the kingdom.
			// If any neighboring kingdom has that heritage as dominant one, transfer the separated kingdom to the neighboring kingdom's empire.
			var disconnectedEmpiresDict = GetDictOfDisconnectedEmpires(kingdomAdjacencies, removableEmpireIds);
			if (disconnectedEmpiresDict.Count == 0) {
				return;
			}
			Logger.Debug("\tTransferring stranded kingdoms to neighboring empires...");
			foreach (var (empire, kingdomGroups) in disconnectedEmpiresDict) {
				var dissolvableGroups = kingdomGroups.Where(g => g.Count == 1).ToList();
				foreach (var group in dissolvableGroups) {
					var kingdom = group.First();
					if (!kingdomToDominantHeritagesDict.TryGetValue(kingdom.Id, out var dominantHeritages)) {
						continue;
					}
					if (dominantHeritages.Length < 2) {
						continue;
					}
					
					var adjacentEmpiresByLand = kingdomAdjacenciesByLand[kingdom.Id].Select(k => this[k].DeJureLiege)
						.Where(e => e is not null)
						.Select(e => e!)
						.ToHashSet();
					
					// Try to find valid neighbor by land first, to reduce the number of exclaves.
					Title? validNeighbor = null;
					foreach (var secondaryHeritage in dominantHeritages.Skip(1)) {
						if (!heritageToEmpireDict.TryGetValue(secondaryHeritage.Id, out var heritageEmpire)) {
							continue;
						}
						if (!adjacentEmpiresByLand.Contains(heritageEmpire)) {
							continue;
						}

						validNeighbor = heritageEmpire;
						Logger.Debug($"\t\tTransferring kingdom {kingdom.Id} from empire {empire.Id} to empire {validNeighbor.Id} neighboring by land.");
						break;
					}
					
					// If no valid neighbor by land, try to find valid neighbor by water.
					if (validNeighbor is null) {
						var adjacentEmpiresByWaterBody = kingdomAdjacenciesByWaterBody[kingdom.Id].Select(k => this[k].DeJureLiege)
							.Where(e => e is not null)
							.Select(e => e!)
							.ToHashSet();
						
						foreach (var secondaryHeritage in dominantHeritages.Skip(1)) {
							if (!heritageToEmpireDict.TryGetValue(secondaryHeritage.Id, out var heritageEmpire)) {
								continue;
							}
							if (!adjacentEmpiresByWaterBody.Contains(heritageEmpire)) {
								continue;
							}

							validNeighbor = heritageEmpire;
							Logger.Debug($"\t\tTransferring kingdom {kingdom.Id} from empire {empire.Id} to empire {validNeighbor.Id} neighboring by water body.");
							break;
						}
					}

					if (validNeighbor is not null) {
						kingdom.DeJureLiege = validNeighbor;
					}
				}
			}	
			
			disconnectedEmpiresDict = GetDictOfDisconnectedEmpires(kingdomAdjacencies, removableEmpireIds);
			if (disconnectedEmpiresDict.Count == 0) {
				return;
			}
			Logger.Debug("\tCreating new empires for disconnected groups...");
			foreach (var (empire, groups) in disconnectedEmpiresDict) {
				// Keep the largest group as is, and create new empires based on most developed counties for the rest.
				var largestGroup = groups.MaxBy(g => g.Count);
				foreach (var group in groups) {
					if (group == largestGroup) {
						continue;
					}
					
					var mostDevelopedCounty = group
						.SelectMany(k => k.GetDeJureVassalsAndBelow("c").Values)
						.MaxBy(c => c.GetOwnOrInheritedDevelopmentLevel(date));
					if (mostDevelopedCounty is null) {
						continue;
					}
					
					string newEmpireId = $"e_IRTOCK3_from_{mostDevelopedCounty.Id}";
					var newEmpire = Add(newEmpireId);
					newEmpire.Color1 = mostDevelopedCounty.Color1;
					newEmpire.CapitalCounty = mostDevelopedCounty;
					newEmpire.HasDefiniteForm = false;
					
					var empireNameLoc = newEmpire.Localizations.AddLocBlock(newEmpireId);
					empireNameLoc.ModifyForEveryLanguage(
						(orig, language) => $"${mostDevelopedCounty.Id}$"
					);
					
					var empireAdjLoc = newEmpire.Localizations.AddLocBlock(newEmpireId + "_adj");
					empireAdjLoc.ModifyForEveryLanguage(
						(orig, language) => $"${mostDevelopedCounty.Id}_adj$"
					);

					foreach (var kingdom in group) {
						kingdom.DeJureLiege = newEmpire;
					}
					
					Logger.Debug($"\t\tCreated new empire {newEmpire.Id} for group {string.Join(',', group.Select(k => k.Id))}.");
				}
			}
			
			disconnectedEmpiresDict = GetDictOfDisconnectedEmpires(kingdomAdjacencies, removableEmpireIds);
			if (disconnectedEmpiresDict.Count > 0) {
				Logger.Warn("Failed to split some disconnected empires: " + string.Join(", ", disconnectedEmpiresDict.Keys.Select(e => e.Id)));
			}
		}

		private Dictionary<Title, List<HashSet<Title>>> GetDictOfDisconnectedEmpires(
			IDictionary<string, HashSet<string>> kingdomAdjacencies,
			IReadOnlySet<string> removableEmpireIds
		) {
			var dictToReturn = new Dictionary<Title, List<HashSet<Title>>>();
			
			foreach (var empire in this.Where(t => t.Rank == TitleRank.empire)) {
				IEnumerable<Title> deJureKingdoms = empire.GetDeJureVassalsAndBelow("k").Values;

				// Unassign de jure kingdoms that have no de jure land themselves.
				var deJureKingdomsWithoutLand =
					deJureKingdoms.Where(k => k.GetDeJureVassalsAndBelow("c").Count == 0).ToHashSet();
				foreach (var deJureKingdomWithLand in deJureKingdomsWithoutLand) {
					deJureKingdomWithLand.DeJureLiege = null;
				}

				deJureKingdoms = deJureKingdoms.Except(deJureKingdomsWithoutLand).ToList();

				if (!deJureKingdoms.Any()) {
					if (removableEmpireIds.Contains(empire.Id)) {
						Remove(empire.Id);
					}

					continue;
				}

				// Group the kingdoms into contiguous groups.
				var kingdomGroups = new List<HashSet<Title>>();
				foreach (var kingdom in deJureKingdoms) {
					var added = false;
					List<HashSet<Title>> connectedGroups = [];

					foreach (var group in kingdomGroups) {
						if (group.Any(k => kingdomAdjacencies[k.Id].Contains(kingdom.Id))) {
							group.Add(kingdom);
							connectedGroups.Add(group);

							added = true;
						}
					}

					// If the kingdom is adjacent to multiple groups, merge them.
					if (connectedGroups.Count > 1) {
						var mergedGroup = new HashSet<Title>();
						foreach (var group in connectedGroups) {
							mergedGroup.UnionWith(group);
							kingdomGroups.Remove(group);
						}

						mergedGroup.Add(kingdom);
						kingdomGroups.Add(mergedGroup);
					}

					if (!added) {
						kingdomGroups.Add([kingdom]);
					}
				}

				if (kingdomGroups.Count <= 1) {
					continue;
				}

				Logger.Debug($"\tEmpire {empire.Id} has {kingdomGroups.Count} disconnected groups of kingdoms: {string.Join(" ; ", kingdomGroups.Select(g => string.Join(',', g.Select(k => k.Id))))}");
				dictToReturn[empire] = kingdomGroups;
			}
			
			return dictToReturn;
		}

		private static bool AreTitlesAdjacent(HashSet<ulong> title1ProvinceIds, HashSet<ulong> title2ProvinceIds, MapData mapData) {
			return mapData.AreProvinceGroupsAdjacent(title1ProvinceIds, title2ProvinceIds);
		}
		private static bool AreTitlesAdjacentByLand(HashSet<ulong> title1ProvinceIds, HashSet<ulong> title2ProvinceIds, MapData mapData) {
			return mapData.AreProvinceGroupsAdjacentByLand(title1ProvinceIds, title2ProvinceIds);
		}
		private static bool AreTitlesAdjacentByWaterBody(HashSet<ulong> title1ProvinceIds, HashSet<ulong> title2ProvinceIds, MapData mapData) {
			return mapData.AreProvinceGroupsConnectedByWaterBody(title1ProvinceIds, title2ProvinceIds);
		}

		private void SetEmpireCapitals(Date ck3BookmarkDate) {
			// Make sure every empire's capital is within the empire's de jure land.
			Logger.Info("Setting empire capitals...");
			foreach (var empire in this.Where(t => t.Rank == TitleRank.empire)) {
				// Try to use most developed county among the de jure kingdom capitals.
				var deJureKingdoms = empire.GetDeJureVassalsAndBelow("k").Values;
				var mostDevelopedCounty = deJureKingdoms
					.Select(k => k.CapitalCounty)
					.Where(c => c is not null)
					.MaxBy(c => c!.GetOwnOrInheritedDevelopmentLevel(ck3BookmarkDate));
				if (mostDevelopedCounty is not null) {
					empire.CapitalCounty = mostDevelopedCounty;
					continue;
				}
				
				// Otherwise, use the most developed county among the de jure empire's counties.
				var deJureCounties = empire.GetDeJureVassalsAndBelow("c").Values;
				mostDevelopedCounty = deJureCounties
					.MaxBy(c => c.GetOwnOrInheritedDevelopmentLevel(ck3BookmarkDate));
				if (mostDevelopedCounty is not null) {
					empire.CapitalCounty = mostDevelopedCounty;
				}
			}
		}

		public void SetDeJureKingdomsAndEmpires(Date ck3BookmarkDate, CultureCollection ck3Cultures, CharacterCollection ck3Characters, MapData ck3MapData) {
			SetDeJureKingdoms(ck3BookmarkDate);
			SetDeJureEmpires(ck3Cultures, ck3Characters, ck3MapData, ck3BookmarkDate);
		}

		private HashSet<string> GetCountyHolderIds(Date date) {
			var countyHoldersCache = new HashSet<string>();
			foreach (var county in this.Where(t => t.Rank == TitleRank.county)) {
				var holderId = county.GetHolderId(date);
				if (holderId != "0") {
					countyHoldersCache.Add(holderId);
				}
			}

			return countyHoldersCache;
		}

		public void ImportDevelopmentFromImperator(ProvinceCollection ck3Provinces, Date date, double irCivilizationWorth) {
			static bool IsCountyOutsideImperatorMap(Title county, IReadOnlyDictionary<string, int> impProvsPerCounty) {
				return impProvsPerCounty[county.Id] == 0;
			}

			double CalculateCountyDevelopment(Title county, IReadOnlyDictionary<ulong, int> ck3ProvsPerIRProv) {
				double dev = 0;
				IEnumerable<ulong> countyProvinceIds = county.CountyProvinceIds;
				int provsCount = 0;
				foreach (var ck3ProvId in countyProvinceIds) {
					if (!ck3Provinces.TryGetValue(ck3ProvId, out var ck3Province)) {
						Logger.Warn($"CK3 province {ck3ProvId} not found!");
						continue;
					}
					++provsCount;
					var sourceProvinces = ck3Province.ImperatorProvinces;
					if (sourceProvinces.Count == 0) {
						continue;
					}

					dev += sourceProvinces.Average(srcProv => srcProv.CivilizationValue / ck3ProvsPerIRProv[srcProv.Id]);
				}

				dev *= irCivilizationWorth;
				dev /= provsCount;
				dev -= Math.Sqrt(dev);
				return dev;
			}

			Logger.Info("Importing development from Imperator...");

			var counties = this.Where(t => t.Rank == TitleRank.county).ToList();
			var (irProvsPerCounty, ck3ProvsPerImperatorProv) = GetIRProvsPerCounty(ck3Provinces, counties);

			foreach (var county in counties) {
				if (IsCountyOutsideImperatorMap(county, irProvsPerCounty)) {
					// Don't change development for counties outside of Imperator map.
					continue;
				}

				double dev = CalculateCountyDevelopment(county, ck3ProvsPerImperatorProv);

				county.History.Fields.Remove("development_level");
				county.History.AddFieldValue(date, "development_level", "change_development_level", (int)dev);
			}

			Logger.IncrementProgress();
			return;

			static (Dictionary<string, int>, Dictionary<ulong, int>) GetIRProvsPerCounty(ProvinceCollection ck3Provinces, IEnumerable<Title> counties) {
				Dictionary<string, int> impProvsPerCounty = [];
				Dictionary<ulong, int> ck3ProvsPerImperatorProv = [];
				foreach (var county in counties) {
					HashSet<ulong> imperatorProvs = [];
					foreach (ulong ck3ProvId in county.CountyProvinceIds) {
						if (!ck3Provinces.TryGetValue(ck3ProvId, out var ck3Province)) {
							Logger.Warn($"CK3 province {ck3ProvId} not found!");
							continue;
						}

						var sourceProvinces = ck3Province.ImperatorProvinces;
						foreach (var irProvince in sourceProvinces) {
							imperatorProvs.Add(irProvince.Id);
							ck3ProvsPerImperatorProv.TryGetValue(irProvince.Id, out var currentValue);
							ck3ProvsPerImperatorProv[irProvince.Id] = currentValue + 1;
						}
					}

					impProvsPerCounty[county.Id] = imperatorProvs.Count;
				}

				return (impProvsPerCounty, ck3ProvsPerImperatorProv);
			}
		}

		public IEnumerable<Title> GetCountriesImportedFromImperator() {
			return this.Where(t => t.ImperatorCountry is not null);
		}

		public IReadOnlyCollection<Title> GetDeJureDuchies() => this
			.Where(t => t is {Rank: TitleRank.duchy, DeJureVassals.Count: > 0})
			.ToImmutableArray();
		
		public IReadOnlyCollection<Title> GetDeJureKingdoms() => this
			.Where(t => t is {Rank: TitleRank.kingdom, DeJureVassals.Count: > 0})
			.ToImmutableArray();
		
		private HashSet<Color> UsedColors => this.Select(t => t.Color1).Where(c => c is not null).ToHashSet()!;
		public bool IsColorUsed(Color color) {
			return UsedColors.Contains(color);
		}
		public Color GetDerivedColor(Color baseColor) {
			HashSet<Color> usedHueColors = UsedColors.Where(c => Math.Abs(c.H - baseColor.H) < 0.001).ToHashSet();

			for (double v = 0.05; v <= 1; v += 0.02) {
				var newColor = new Color(baseColor.H, baseColor.S, v);
				if (usedHueColors.Contains(newColor)) {
					continue;
				}
				return newColor;
			}

			Logger.Warn($"Couldn't generate new color from base {baseColor.OutputRgb()}");
			return baseColor;
		}

		private readonly HistoryFactory titleHistoryFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithSimpleField("holder", new OrderedSet<string> { "holder", "holder_ignore_head_of_faith_requirement" }, null)
			.WithSimpleField("government", "government", null)
			.WithSimpleField("liege", "liege", null)
			.WithSimpleField("development_level", "change_development_level", null)
			.WithSimpleField("succession_laws", "succession_laws", new SortedSet<string>())
			.Build();

		public void LoadHistory(Configuration config, ModFilesystem ck3ModFS) {
			var ck3BookmarkDate = config.CK3BookmarkDate;

			int loadedHistoriesCount = 0;

			var titlesHistoryParser = new Parser();
			titlesHistoryParser.RegisterRegex(Regexes.TitleId, (reader, titleName) => {
				var historyItem = reader.GetStringOfItem().ToString();
				if (!historyItem.Contains('{')) {
					return;
				}

				if (!TryGetValue(titleName, out var title)) {
					return;
				}

				var tempReader = new BufferedReader(historyItem);

				titleHistoryFactory.UpdateHistory(title.History, tempReader);
				++loadedHistoriesCount;
			});
			titlesHistoryParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			Logger.Info("Parsing title history...");
			titlesHistoryParser.ParseGameFolder("history/titles", ck3ModFS, "txt", true, true);
			Logger.Info($"Loaded {loadedHistoriesCount} title histories.");

			// Add vanilla development to counties
			// For counties that inherit development level from de jure lieges, assign it to them directly for better reliability.
			foreach (var title in this.Where(t => t.Rank == TitleRank.county && t.GetDevelopmentLevel(ck3BookmarkDate) is null)) {
				var inheritedDev = title.GetOwnOrInheritedDevelopmentLevel(ck3BookmarkDate);
				title.SetDevelopmentLevel(inheritedDev ?? 0, ck3BookmarkDate);
			}

			// Remove history entries past the bookmark date.
			foreach (var title in this) {
				title.RemoveHistoryPastDate(ck3BookmarkDate);
			}
		}

		public void LoadCulturalNamesFromConfigurables() {
			const string filePath = "configurables/cultural_title_names.txt";
			Logger.Info($"Loading cultural title names from \"{filePath}\"...");

			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.String, (reader, titleId) => {
				var nameListToLocKeyDict = reader.GetAssignments()
					.GroupBy(a => a.Key)
					.ToDictionary(g => g.Key, g => g.Last().Value);

				if (!TryGetValue(titleId, out var title)) {
					return;
				}
				if (title.CulturalNames is null) {
					title.CulturalNames = nameListToLocKeyDict;
				} else {
					foreach (var (nameList, locKey) in nameListToLocKeyDict) {
						title.CulturalNames[nameList] = locKey;
					}
				}
			});
			parser.IgnoreAndLogUnregisteredItems();
			parser.ParseFile(filePath);
		}
	}
}