using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.Imperator.Countries;

namespace ImperatorToCK3.Mappers.TagTitle {
	public class TagTitleMapper : Parser {
		public TagTitleMapper(string filePath) {
			Logger.Info("Parsing Title mappings.");
			RegisterKeys();
			ParseFile(filePath);
			ClearRegisteredRules();
			Logger.Info($"{mappings.Count} title mappings loaded.");
		}
		public void RegisterTag(string imperatorTag, string ck3Title) {
			registeredTagTitles.Add(imperatorTag, ck3Title);
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
				var match = mapping.TagRankMatch(imperatorTag, GetCK3TitleRank(countryRank, localizedTitleName));
				if (match is not null) {
					if (usedTitles.Contains(match)) {
						continue;
					}

					RegisterTag(imperatorTag, match);
					return match;
				}
			}

			// Generate a new tag
			var generatedTitle = GenerateNewTitle(imperatorTag, countryRank, localizedTitleName);
			RegisterTag(imperatorTag, generatedTitle);
			return generatedTitle;
		}
		public string? GetTitleForTag(string imperatorTag, CountryRank countryRank) {
			return GetTitleForTag(imperatorTag, countryRank, string.Empty);
		}

		private void RegisterKeys() {
			RegisterKeyword("link", reader => {
				mappings.Add(TagTitleMapping.Parse(reader));
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
		private static string GenerateNewTitle(string imperatorTag, CountryRank countryRank, string localizedTitleName) {
			var ck3Tag = GetCK3TitleRank(countryRank, localizedTitleName);
			ck3Tag += "_";
			ck3Tag += generatedCK3TitlePrefix;
			ck3Tag += imperatorTag;

			return ck3Tag;
		}

		private readonly List<TagTitleMapping> mappings = new();
		private readonly Dictionary<string, string> registeredTagTitles = new(); // We store already mapped countries here.
		private readonly SortedSet<string> usedTitles = new();

		private const string generatedCK3TitlePrefix = "IMPTOCK3_";
	}
}
