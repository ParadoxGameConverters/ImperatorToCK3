using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.Province {
	public class ProvinceMappingsVersion : Parser {
		public List<ProvinceMapping> Mappings { get; private set; } = new();
		public ProvinceMappingsVersion() { }
		public ProvinceMappingsVersion(BufferedReader reader) {
			RegisterKeyword("link", reader => {
				var mapping = ProvinceMapping.Parse(reader);
				if (mapping.CK3Provinces.Count == 0 && mapping.ImperatorProvinces.Count == 0) {
					return;
				}
				Mappings.Add(mapping);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			ParseStream(reader);
			ClearRegisteredRules();
		}
	}
}
