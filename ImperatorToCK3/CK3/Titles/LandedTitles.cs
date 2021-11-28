using commonItems;
using ImperatorToCK3.Imperator.Countries;
using System.Collections.Generic;
using System.Linq;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.Region;

namespace ImperatorToCK3.CK3.Titles {
	// This is a recursive class that scrapes 00_landed_titles.txt (and related files) looking for title colors, landlessness,
	// and most importantly relation between baronies and barony provinces so we can link titles to actual clay.
	// Since titles are nested according to hierarchy we do this recursively.
	public class LandedTitles : TitleCollection {
		public Dictionary<string, object> Variables { get; } = new();

		public void LoadTitles(string fileName) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseFile(fileName);
			ProcessPostLoad(parser);
		}
		public void LoadTitles(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
			ProcessPostLoad(parser);
		}
		private void ProcessPostLoad(Parser parser) {
			foreach (var (name, value) in parser.Variables) {
				Variables[name] = value;
			}
			Logger.Debug($"Ignored Title tokens: {string.Join(", ", Title.IgnoredTokens)}");
			LinkCapitals();
		}

		public override void Add(Title? title) {
			if (title is null) {
				Logger.Warn("Cannot insert null Title to LandedTitles!");
				return;
			}
			if (!string.IsNullOrEmpty(title.Id)) {
				dict[title.Id] = title;
				title.LinkCapital(this);
			} else {
				Logger.Warn("Not inserting a Title with empty name!");
			}
		}
		public override void Remove(string name) {
			if (dict.TryGetValue(name, out var titleToErase)) {
				var deJureLiege = titleToErase.DeJureLiege;
				if (deJureLiege is not null) {
					deJureLiege.DeJureVassals.Remove(name);
				}

				var deFactoLiege = titleToErase.DeFactoLiege;
				if (deFactoLiege is not null) {
					deFactoLiege.DeFactoVassals.Remove(name);
				}

				foreach (var vassal in titleToErase.DeJureVassals) {
					vassal.DeJureLiege = null;
				}
				foreach (var vassal in titleToErase.DeFactoVassals) {
					vassal.DeFactoLiege = null;
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
				var newTitle = new Title(titleNameStr);
				newTitle.LoadTitles(reader, parser.Variables);

				Title.AddFoundTitle(newTitle, dict);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		private void LinkCapitals() {
			foreach (var title in this) {
				title.LinkCapital(this);
			}
		}

		public void ImportImperatorCountries(
			CountryCollection imperatorCountries,
			TagTitleMapper tagTitleMapper,
			LocalizationMapper localizationMapper,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			GovernmentMapper governmentMapper,
			SuccessionLawMapper successionLawMapper,
			DefiniteFormMapper definiteFormMapper,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			CharacterCollection characters) {
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
					localizationMapper,
					provinceMapper,
					coaMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					characters
				);
				++counter;
			}
			Logger.Info($"Imported {counter} countries from I:R.");
		}

		private void ImportImperatorCountry(
			Country country,
			CountryCollection imperatorCountries,
			TagTitleMapper tagTitleMapper,
			LocalizationMapper localizationMapper,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			GovernmentMapper governmentMapper,
			SuccessionLawMapper successionLawMapper,
			DefiniteFormMapper definiteFormMapper,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			CharacterCollection characters) {
			// Create a new title or update existing title
			var name = Title.DetermineName(country, imperatorCountries, tagTitleMapper, localizationMapper);

			if (TryGetValue(name, out var existingTitle)) {
				existingTitle.InitializeFromTag(
					country,
					imperatorCountries,
					localizationMapper,
					this,
					provinceMapper,
					coaMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					characters
				);
			} else {
				var newTitle = new Title(
					country,
					imperatorCountries,
					localizationMapper,
					this,
					provinceMapper,
					coaMapper,
					tagTitleMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					characters
				);
				Add(newTitle);
			}
		}

		public void ImportImperatorGovernorships(
				Imperator.World impWorld,
				TagTitleMapper tagTitleMapper,
				LocalizationMapper localizationMapper,
				ProvinceMapper provinceMapper,
				DefiniteFormMapper definiteFormMapper,
				ImperatorRegionMapper imperatorRegionMapper,
				CoaMapper coaMapper
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
					impWorld.Characters,
					governorshipsPerRegion[governorship.RegionName] > 1,
					tagTitleMapper,
					localizationMapper,
					provinceMapper,
					definiteFormMapper,
					imperatorRegionMapper,
					coaMapper
				);
				++counter;
			}
			Logger.Info($"Imported {counter} governorships from I:R.");
		}
		private void ImportImperatorGovernorship(
			Governorship governorship,
			CountryCollection imperatorCountries,
			Imperator.Characters.CharacterCollection imperatorCharacters,
			bool regionHasMultipleGovernorships,
			TagTitleMapper tagTitleMapper,
			LocalizationMapper localizationMapper,
			ProvinceMapper provinceMapper,
			DefiniteFormMapper definiteFormMapper,
			ImperatorRegionMapper imperatorRegionMapper,
			CoaMapper coaMapper) {
			var country = imperatorCountries[governorship.CountryId];
			// Create a new title or update existing title
			var name = Title.DetermineName(governorship, country, tagTitleMapper);
			if (name is null) {
				Logger.Warn($"Cannot convert {governorship.RegionName} of country {country.Id}");
				return;
			}

			//Logger.Notice($"GOVERNORSHIP {name} to {country.CK3Title.Id}");

			if (TryGetValue(name, out var existingTitle)) {
				existingTitle.InitializeFromGovernorship(
					governorship,
					country,
					imperatorCharacters,
					regionHasMultipleGovernorships,
					localizationMapper,
					this,
					provinceMapper,
					definiteFormMapper,
					imperatorRegionMapper
				);
			} else {
				var newTitle = new Title(
					name,
					governorship,
					country,
					imperatorCharacters,
					regionHasMultipleGovernorships,
					localizationMapper,
					this,
					provinceMapper,
					coaMapper,
					tagTitleMapper,
					definiteFormMapper,
					imperatorRegionMapper
				);
				Add(newTitle);
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
				if (title.Rank > TitleRank.county && !countyHoldersCache.Contains(title.GetHolderId(ck3BookmarkDate))) {
					var id = title.Id;
					if (!this[id].Landless) { // does not have landless attribute set to true
						if (title.IsImportedOrUpdatedFromImperator && id.Contains("IMPTOCK3")) {
							removedGeneratedTitles.Add(id);
							Remove(id);
						} else {
							revokedVanillaTitles.Add(id);
							title.ClearHolderSpecificHistory();
							title.DeFactoLiege = null;
						}
					}
				}
			}
			if (removedGeneratedTitles.Count > 0) {
				Logger.Debug($"Found landless generated titles that can't be landless: {string.Join(", ", removedGeneratedTitles)}");
			}
			if (revokedVanillaTitles.Count > 0) {
				Logger.Debug($"Found landless vanilla titles that can't be landless: {string.Join(", ", revokedVanillaTitles)}");
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
