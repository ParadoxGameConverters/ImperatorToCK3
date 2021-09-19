using commonItems;
using ImperatorToCK3.Imperator.Countries;
using System.Collections.Generic;

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
			// the only case where we fail is on invalid invocation. Otherwise, failure is
			// not an option!
			if (string.IsNullOrEmpty(imperatorTag)) {
				return null;
			}

			// look up register
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
			string rank = GetCK3TitleRank(ck3LiegeTitle);

			// the only case where we fail is on invalid invocation. Otherwise, failure is not an option!
			if (string.IsNullOrEmpty(imperatorRegion)) {
				return null;
			}

			// look up register
			if (registeredGovernorshipTitles.TryGetValue($"{imperatorCountryTag}_{imperatorRegion}", out var titleToReturn)) {
				return titleToReturn;
			}

			// Generate a new title
			var generatedTitle = GenerateNewTitle(imperatorRegion, imperatorCountryTag, ck3LiegeTitle);
			RegisterGovernorship(imperatorRegion, imperatorCountryTag, generatedTitle);
			return generatedTitle;
		}

		private void RegisterKeys() {
			RegisterKeyword("link", reader => {
				mappings.Add(Mapping.Parse(reader));
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private static string GetCK3TitleRank(CountryRank imperatorRank, string localizedTitleName) {
			if (localizedTitleName.IndexOf("Empire", System.StringComparison.Ordinal) != -1) {
				return "e";
			} else if (localizedTitleName.IndexOf("Kingdom", System.StringComparison.Ordinal) != -1) {
				return "k";
			} else {
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
		}
		private static string GetCK3TitleRank(string ck3LiegeTitle) {
			if (ck3LiegeTitle.StartsWith('e')) {
				return "k";
			}
			return "d";
		}
		private static string GenerateNewTitle(string imperatorTag, CountryRank countryRank, string localizedTitleName) {
			var ck3Tag = GetCK3TitleRank(countryRank, localizedTitleName);
			ck3Tag += "_";
			ck3Tag += generatedCK3TitlePrefix;
			ck3Tag += imperatorTag;

			return ck3Tag;
		}
		private static string GenerateNewTitle(string imperatorRegion, string imperatorCountryTag, string ck3LiegeTitle) {
			var ck3Tag = GetCK3TitleRank(ck3LiegeTitle);
			ck3Tag += "_";
			ck3Tag += generatedCK3TitlePrefix;
			ck3Tag += imperatorCountryTag;
			ck3Tag += "_";
			ck3Tag += imperatorRegion;

			return ck3Tag;
		}

		private readonly List<Mapping> mappings = new();
		private readonly Dictionary<string, string> registeredTagTitles = new(); // We store already mapped countries here.
		private readonly Dictionary<string, string> registeredGovernorshipTitles = new(); // We store already mapped governorships here.
		private readonly SortedSet<string> usedTitles = new();

		private const string generatedCK3TitlePrefix = "IMPTOCK3_";
	}
}
