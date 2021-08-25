using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.Mappers.Region;

namespace ImperatorToCK3.Mappers.Religion {
	public class ReligionMapper : Parser {
		public ReligionMapper() {
			Logger.Info("Parsing religion mappings.");
			RegisterKeys();
			ParseFile("configurables/religion_map.txt");
			ClearRegisteredRules();
			Logger.Info($"Loaded {religionMappings.Count} religious links.");
		}
		public ReligionMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		public void LoadRegionMappers(ImperatorRegionMapper imperatorRegionMapper, CK3RegionMapper ck3RegionMapper) {
			foreach (var mapping in religionMappings) {
				mapping.ImperatorRegionMapper = imperatorRegionMapper;
				mapping.CK3RegionMapper = ck3RegionMapper;
			}
		}
		public string? Match(string imperatorReligion, ulong ck3ProvinceID, ulong imperatorProvinceID) {
			foreach (var religionMapping in religionMappings) {
				var possibleMatch = religionMapping.Match(imperatorReligion, ck3ProvinceID, imperatorProvinceID);
				if (possibleMatch is not null) {
					return possibleMatch;
				}
			}
			return null;
		}

		private void RegisterKeys() {
			RegisterKeyword("link", reader => {
				religionMappings.Add(ReligionMapping.Parse(reader));
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private readonly List<ReligionMapping> religionMappings = new();
	}
}
