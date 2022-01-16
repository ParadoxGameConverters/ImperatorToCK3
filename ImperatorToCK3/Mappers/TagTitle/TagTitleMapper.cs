using commonItems;
using ImperatorToCK3.Imperator.Countries;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.TagTitle {
	public class TagTitleMapper {
		public TagTitleMapper() { }
		public TagTitleMapper(string tagTitleMappingsPath, string governorshipTitleMappingsPath) {
			Logger.Info("Parsing Title mappings...");

			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseFile(tagTitleMappingsPath);
			parser.ParseFile(governorshipTitleMappingsPath);

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
		public string? GetTitleForTag(Country country, string localizedTitleName) {
			// the only case where we fail is on invalid invocation. Otherwise, failure is
			// not an option!
			if (string.IsNullOrEmpty(country.Tag)) {
				return null;
			}

			// look up register
			if (registeredTagTitles.TryGetValue(country.Tag, out var titleToReturn)) {
				return titleToReturn;
			}

			// Attempt a title match
			foreach (var mapping in mappings) {
				var match = mapping.RankMatch(country.Tag, GetCK3TitleRank(country, localizedTitleName));
				if (match is not null) {
					if (usedTitles.Contains(match)) {
						continue;
					}

					RegisterTag(country.Tag, match);
					return match;
				}
			}

			// Generate a new title
			var generatedTitle = GenerateNewTitle(country, localizedTitleName);
			RegisterTag(country.Tag, generatedTitle);
			return generatedTitle;
		}
		public string? GetTitleForTag(Country country) {
			return GetTitleForTag(country, string.Empty);
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

		private void RegisterKeys(Parser parser) {
			parser.RegisterKeyword("link", reader => {
				mappings.Add(Mapping.Parse(reader));
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private static string GetCK3TitleRank(Country country, string localizedTitleName) {
			if (localizedTitleName.Contains("Empire", System.StringComparison.Ordinal)) {
				return "e";
			}

			if (localizedTitleName.Contains("Kingdom", System.StringComparison.Ordinal)) {
				return "k";
			}

			switch (country.Rank) {
				case CountryRank.migrantHorde:
				case CountryRank.cityState:
					return "d";
				case CountryRank.localPower:
				case CountryRank.regionalPower:
				case CountryRank.majorPower:
					return "k";
				case CountryRank.greatPower:
					return "e";
				default:
					return "d";
			}
		}
		private static string GetCK3TitleRank(string ck3LiegeTitle) {
			if (ck3LiegeTitle.StartsWith('e')) {
				return "k";
			}
			return "d";
		}
		private static string GenerateNewTitle(Country country, string localizedTitleName) {
			var ck3Tag = GetCK3TitleRank(country, localizedTitleName);
			ck3Tag += "_";
			ck3Tag += generatedCK3TitlePrefix;
			ck3Tag += country.Tag;

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
