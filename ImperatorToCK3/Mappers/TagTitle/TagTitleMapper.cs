using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.TagTitle {
	public class TagTitleMapper : Parser {
		public TagTitleMapper(string tagTitleMappingsPath, string governorshipTitleMappingsPath) {
			Logger.Info("Parsing Title mappings.");
			RegisterKeys();
			ParseFile(tagTitleMappingsPath);
			ParseFile(governorshipTitleMappingsPath);
			ClearRegisteredRules();
			Logger.Info($"{mappings.Count} title mappings loaded.");
		}
		public void RegisterTag(string imperatorTag, string ck3Title) {
			registeredTagTitles.Add(imperatorTag, ck3Title);
			usedTitles.Add(ck3Title);
		}
		public void RegisterGovernorship(string imperatorRegion, string imperatorCountryTag, string ck3Title) {
			registeredGovernorshipTitles.Add($"{imperatorCountryTag}_{imperatorRegion}", ck3Title);
			usedTitles.Add(ck3Title);
		}
		public string? GetTitleForTag(string imperatorTag, CountryRank countryRank, string localizedTitleName) {
			// The only case where we fail is on invalid invocation. Otherwise, failure is not an option!
			if (string.IsNullOrEmpty(imperatorTag)) {
				return null;
			}

			// Look up register
			if (registeredTagTitles.TryGetValue(imperatorTag, out var titleToReturn)) {
				return titleToReturn;
			}

			// Attempt a title match
			foreach (var mapping in mappings) {
				var match = mapping.RankMatch(imperatorTag, GetCK3TitleRank(countryRank, localizedTitleName));
				if (match is not null) {
					if (usedTitles.Contains(match)) {
						continue;
					}

					RegisterTag(imperatorTag, match);
					return match;
				}
			}

			// Generate a new title
			var generatedTitle = GenerateNewTitle(imperatorTag, countryRank, localizedTitleName);
			RegisterTag(imperatorTag, generatedTitle);
			return generatedTitle;
		}
		public string? GetTitleForTag(string imperatorTag, CountryRank countryRank) {
			return GetTitleForTag(imperatorTag, countryRank, string.Empty);
		}
		public string? GetTitleForGovernorship(string imperatorRegion, string imperatorCountryTag, string ck3LiegeTitle) {
			var rank = GetCK3GovernorshipRank(ck3LiegeTitle);
			if (rank is null) {
				return null;
			}

			if (string.IsNullOrEmpty(imperatorRegion)) {
				return null;
			}

			// Look up register
			if (registeredGovernorshipTitles.TryGetValue($"{imperatorCountryTag}_{imperatorRegion}", out var titleToReturn)) {
				return titleToReturn;
			}

			if (rank == "c") {
				return GetCountyForGovernorship(governorship, titles, provinces, imperatorRegionMapper);
			} else {
				// Attempt a title match
				foreach (var mapping in mappings) {
					var match = mapping.RankMatch(imperatorRegion, rank);
					if (match is null) {
						continue;
					}

					if (usedTitles.Contains(match)) {
						continue;
					}
					RegisterGovernorship(imperatorRegion, imperatorCountryTag, match);
					return match;
				}

				// Generate a new title
				var generatedTitle = GenerateNewTitle(imperatorRegion, imperatorCountryTag, ck3LiegeTitle);
				RegisterGovernorship(imperatorRegion, imperatorCountryTag, generatedTitle);
				return generatedTitle;
			}
		}

		private string? GetCountyForGovernorship(Governorship governorship, LandedTitles titles, ProvinceCollection provinces, ImperatorRegionMapper imperatorRegionMapper) {
			foreach (var county in titles.Where(t => t.Rank == TitleRank.county)) {
				ulong capitalBaronyProvinceId = (ulong)county.CapitalBaronyProvince!;
				if (capitalBaronyProvinceId == 0) {
					// title's capital province has an invalid ID (0 is not a valid province in CK3)
					continue;
				}

				if (!provinces.ContainsKey(capitalBaronyProvinceId)) {
					Logger.Warn($"Capital barony province not found: {county.CapitalBaronyProvince}");
					continue;
				}

				var ck3CapitalBaronyProvince = provinces[capitalBaronyProvinceId];
				var impProvince = ck3CapitalBaronyProvince.ImperatorProvince;
				if (impProvince is null) { // probably outside of Imperator map
					continue;
				}

				var impCountry = impProvince.OwnerCountry;
				if (impCountry is null) { // e.g. uncolonized Imperator province
					continue;
				}


				var ck3Country = impCountry.CK3Title;
				if (ck3Country is null) {
					continue;
				}

				var ck3CapitalCounty = ck3Country.CapitalCounty;
				if (ck3CapitalCounty is null) {
					continue;
				}
				// if title belongs to country ruler's capital's de jure duchy, it needs to be directly held by the ruler
				var countryCapitalDuchy = ck3CapitalCounty.DeJureLiege;
				var deJureDuchyOfCounty = county.DeJureLiege;
				if (countryCapitalDuchy is not null && deJureDuchyOfCounty is not null && countryCapitalDuchy.Id == deJureDuchyOfCounty.Id) {
					continue;
				}


				if (governorship.RegionName == imperatorRegionMapper.GetParentRegionName(impProvince.Id)) {
					return county.Id;
				}
			}

			return null;
		}

		private void RegisterKeys() {
			RegisterKeyword("link", reader => mappings.Add(Mapping.Parse(reader)));
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private static string GetCK3TitleRank(CountryRank imperatorRank, string localizedTitleName) {
			if (localizedTitleName.Contains("Empire", System.StringComparison.Ordinal)) {
				return "e";
			}

			if (localizedTitleName.Contains("Kingdom", System.StringComparison.Ordinal)) {
				return "k";
			}

			return imperatorRank switch {
				CountryRank.migrantHorde => "d",
				CountryRank.cityState => "d",
				CountryRank.localPower => "k",
				CountryRank.regionalPower => "k",
				CountryRank.majorPower => "k",
				CountryRank.greatPower => "e",
				_ => "d"
			};
		}
		private static string? GetCK3GovernorshipRank(string ck3LiegeTitle) {
			if (ck3LiegeTitle.StartsWith('e')) {
				return "k";
			}
			if (ck3LiegeTitle.StartsWith('k')) {
				return "d";
			}
			if (ck3LiegeTitle.StartsWith('d')) {
				return "c";
			}
			return null;
		}
		private static string GenerateNewTitle(string imperatorTag, CountryRank countryRank, string localizedTitleName) {
			var ck3Tag = GetCK3TitleRank(countryRank, localizedTitleName);
			ck3Tag += "_";
			ck3Tag += GeneratedCK3TitlePrefix;
			ck3Tag += imperatorTag;

			return ck3Tag;
		}
		private static string GenerateNewTitle(string imperatorRegion, string imperatorCountryTag, string ck3LiegeTitle) {
			var ck3Tag = GetCK3GovernorshipRank(ck3LiegeTitle);
			ck3Tag += "_";
			ck3Tag += GeneratedCK3TitlePrefix;
			ck3Tag += imperatorCountryTag;
			ck3Tag += "_";
			ck3Tag += imperatorRegion;

			return ck3Tag;
		}

		private readonly List<Mapping> mappings = new();
		private readonly Dictionary<string, string> registeredTagTitles = new(); // We store already mapped countries here.
		private readonly Dictionary<string, string> registeredGovernorshipTitles = new(); // We store already mapped governorships here.
		private readonly SortedSet<string> usedTitles = new();

		private const string GeneratedCK3TitlePrefix = "IMPTOCK3_";
	}
}
