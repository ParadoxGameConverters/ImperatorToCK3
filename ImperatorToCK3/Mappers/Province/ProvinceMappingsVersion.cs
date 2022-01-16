using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Province;

public class ProvinceMappingsVersion {
	public List<ProvinceMapping> Mappings { get; } = new();
	public ProvinceMappingsVersion() { }
	public ProvinceMappingsVersion(BufferedReader reader) {
		var parser = new Parser();
		parser.RegisterKeyword("link", linkReader => {
			var mapping = ProvinceMapping.Parse(linkReader);
			if (mapping.CK3Provinces.Count == 0 && mapping.ImperatorProvinces.Count == 0) {
				return;
			}
			Mappings.Add(mapping);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		parser.ParseStream(reader);
	}
}