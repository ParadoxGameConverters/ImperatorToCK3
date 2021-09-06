using System.Collections.Generic;
using ImperatorToCK3.Mappers.Region;
using commonItems;

namespace ImperatorToCK3.Mappers.Culture {
	public class CultureMapper : Parser {
		public CultureMapper() {
			Logger.Info("Parsing culture mappings.");
			RegisterKeys();
			ParseFile("configurables/culture_map.txt");
			ClearRegisteredRules();
			Logger.Info($"Loaded {cultureMappingRules.Count} cultural links.");
		}
		public CultureMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}

		public void LoadRegionMappers(ImperatorRegionMapper imperatorRegionMapper, CK3RegionMapper ck3RegionMapper) {
			foreach (var mapping in cultureMappingRules) {
				mapping.ImperatorRegionMapper = imperatorRegionMapper;
				mapping.CK3RegionMapper = ck3RegionMapper;
			}
		}

		private void RegisterKeys() {
			RegisterKeyword("link", reader => {
				cultureMappingRules.Add(CultureMappingRule.Parse(reader));
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public string? Match(
			string impCulture,
			string ck3religion,
			ulong ck3ProvinceID,
			ulong impProvinceID,
			string ck3ownerTitle
		) {
			foreach (var cultureMappingRule in cultureMappingRules) {
				var possibleMatch = cultureMappingRule.Match(impCulture, ck3religion, ck3ProvinceID, impProvinceID, ck3ownerTitle);
				if (possibleMatch is not null)
					return possibleMatch;
			}
			return null;
		}

		public string? NonReligiousMatch(
			string impCulture,
			string ck3religion,
			ulong ck3ProvinceID,
			ulong impProvinceID,
			string ck3ownerTitle
		) {
			foreach (var cultureMappingRule in cultureMappingRules) {
				var possibleMatch = cultureMappingRule.NonReligiousMatch(impCulture, ck3religion, ck3ProvinceID, impProvinceID, ck3ownerTitle);
				if (possibleMatch is not null)
					return possibleMatch;
			}
			return null;
		}

		private readonly List<CultureMappingRule> cultureMappingRules = new();
	}
}
