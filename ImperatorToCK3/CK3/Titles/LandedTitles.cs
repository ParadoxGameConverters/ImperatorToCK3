using commonItems;
using commonItems.Localization;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Imperator.Countries;
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Titles;

public partial class Title {
	[commonItems.Serialization.NonSerialized] private readonly LandedTitles parentCollection;

	// This is a recursive class that scrapes 00_landed_titles.txt (and related files) looking for title colors, landlessness,
	// and most importantly relation between baronies and barony provinces so we can link titles to actual clay.
	// Since titles are nested according to hierarchy we do this recursively.
	public class LandedTitles : TitleCollection {
		public Dictionary<string, object> Variables { get; } = new();

		public void LoadTitles(string fileName) {
			var parser = new Parser();
			RegisterKeys(parser);
			var reader = parser.ParseFile(fileName);
			ProcessPostLoad(reader);
		}
		public void LoadTitles(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
			ProcessPostLoad(reader);
		}
		private void ProcessPostLoad(BufferedReader? reader) {
			if (reader is not null) {
				foreach (var (name, value) in reader.Variables) {
					Variables[name] = value;
				}
			}

			Logger.Debug($"Ignored Title tokens: {string.Join(", ", Title.IgnoredTokens)}");
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
			Date conversionDate
		) {
			var newTitle = new Title(this,
				country,
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
				conversionDate
			);
			dict[newTitle.Id] = newTitle;
			return newTitle;
		}

		public Title Add(
			string id,
			Governorship governorship,
			Country country,
			Imperator.Characters.CharacterCollection imperatorCharacters,
			bool regionHasMultipleGovernorships,
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
				imperatorCharacters,
				regionHasMultipleGovernorships,
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
				if (deJureLiege is not null) {
					deJureLiege.DeJureVassals.Remove(name);
				}

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
				if (county.CountyProvinces.Contains(provinceId)) {
					return county;
				}
			}
			return null;
		}

		public HashSet<string> GetHolderIds(Date date) {
			return new HashSet<string>(this.Select(t => t.GetHolderId(date)));
		}

		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(@"(e|k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleNameStr) => {
				// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
				var newTitle = Add(titleNameStr);
				newTitle.LoadTitles(reader);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		public void ImportImperatorCountries(
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
			Date conversionDate
		) {
			Logger.Info("Importing Imperator Countries...");

			// landedTitles holds all titles imported from CK3. We'll now overwrite some and
			// add new ones from Imperator tags.
			var counter = 0;
			// We don't need pirates, barbarians etc.
			foreach (var country in imperatorCountries.Where(c => c.CountryType == CountryType.real)) {
				ImportImperatorCountry(
					country,
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
					conversionDate
				);
				++counter;
			}
			Logger.Info($"Imported {counter} countries from I:R.");
		}

		private void ImportImperatorCountry(
			Country country,
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
			Date conversionDate
		) {
			// Create a new title or update existing title
			var name = DetermineName(country, imperatorCountries, tagTitleMapper, locDB);

			if (TryGetValue(name, out var existingTitle)) {
				existingTitle.InitializeFromTag(
					country,
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
					conversionDate
				);
			} else {
				Add(
					country,
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
					conversionDate
				);
			}
		}

		public void ImportImperatorGovernorships(
			Imperator.World impWorld,
			ProvinceCollection provinces,
			TagTitleMapper tagTitleMapper,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			DefiniteFormMapper definiteFormMapper,
			ImperatorRegionMapper imperatorRegionMapper,
			CoaMapper coaMapper,
			List<Governorship> countryLevelGovernorships
		) {
			Logger.Info("Importing Imperator Governorships...");

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
					this,
					provinces,
					impWorld.Characters,
					governorshipsPerRegion[governorship.RegionName] > 1,
					tagTitleMapper,
					locDB,
					provinceMapper,
					definiteFormMapper,
					imperatorRegionMapper,
					coaMapper,
					countryLevelGovernorships
				);
				++counter;
			}
			Logger.Info($"Imported {counter} governorships from I:R.");
		}
		private void ImportImperatorGovernorship(
			Governorship governorship,
			CountryCollection imperatorCountries,
			LandedTitles titles,
			ProvinceCollection provinces,
			Imperator.Characters.CharacterCollection imperatorCharacters,
			bool regionHasMultipleGovernorships,
			TagTitleMapper tagTitleMapper,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			DefiniteFormMapper definiteFormMapper,
			ImperatorRegionMapper imperatorRegionMapper,
			CoaMapper coaMapper,
			ICollection<Governorship> countryLevelGovernorships
		) {
			var country = imperatorCountries[governorship.CountryId];

			var name = DetermineName(governorship, country, titles, provinces, imperatorRegionMapper, tagTitleMapper);
			if (name is null) {
				Logger.Warn($"Cannot convert {governorship.RegionName} of country {country.Id}");
				return;
			}

			if (name.StartsWith("c_")) {
				countryLevelGovernorships.Add(governorship);
				return;
			}

			// Create a new title or update existing title
			if (TryGetValue(name, out var existingTitle)) {
				existingTitle.InitializeFromGovernorship(
					governorship,
					country,
					imperatorCharacters,
					regionHasMultipleGovernorships,
					locDB,
					provinceMapper,
					definiteFormMapper,
					imperatorRegionMapper
				);
			} else {
				Add(
					name,
					governorship,
					country,
					imperatorCharacters,
					regionHasMultipleGovernorships,
					locDB,
					provinceMapper,
					coaMapper,
					definiteFormMapper,
					imperatorRegionMapper
				);
			}
		}

		public void RemoveInvalidLandlessTitles(Date ck3BookmarkDate) {
			Logger.Info("Removing invalid landless titles.");
			var removedGeneratedTitles = new HashSet<string>();
			var revokedVanillaTitles = new HashSet<string>();

			HashSet<string> countyHoldersCache = GetCountyHolderIds(ck3BookmarkDate);

			foreach (var title in this) {
				// if duchy/kingdom/empire title holder holds no county (is landless), remove the title
				// this also removes landless titles initialized from Imperator
				if (title.Rank <= TitleRank.county || countyHoldersCache.Contains(title.GetHolderId(ck3BookmarkDate))) {
					continue;
				}
				var id = title.Id;
				if (this[id].Landless) {
					continue;
				}
				// does not have landless attribute set to true
				if (title.IsImportedOrUpdatedFromImperator && id.Contains("IMPTOCK3")) {
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
		}

		public void FixDeJure() {
			var deJureDuchies = this.Where(t => t.Rank == TitleRank.duchy && t.DeJureVassals.Count > 0);
			foreach (var duchy in deJureDuchies) {
				var deJureCounties = duchy.DeJureVassals.Where(t => t.Rank == TitleRank.county);
				foreach (var county in deJureCounties) {
					var numOfProvinces = county.CountyProvinces.Count();

				}
			}
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
	}
}