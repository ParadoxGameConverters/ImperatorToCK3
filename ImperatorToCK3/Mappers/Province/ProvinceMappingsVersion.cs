using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Province;

public sealed class ProvinceMappingsVersion {
	public IList<ProvinceMapping> Mappings { get; } = new List<ProvinceMapping>();
	public ProvinceMappingsVersion() { }
	public ProvinceMappingsVersion(BufferedReader reader) {
		var referencedImperatorProvs = new HashSet<ulong>();
		var imperatorProvsReferencedMoreThanOnce = new HashSet<ulong>();
		var referencedCK3Provs = new HashSet<ulong>();
		var ck3ProvsReferencedMoreThanOnce = new HashSet<ulong>();

		var parser = new Parser();
		parser.RegisterKeyword("link", linkReader => {
			var mapping = ProvinceMapping.Parse(linkReader);
			if (mapping.CK3Provinces.Count == 0 && mapping.ImperatorProvinces.Count == 0) {
				return;
			}
			Mappings.Add(mapping);

			foreach (var prov in mapping.ImperatorProvinces) {
				if (referencedImperatorProvs.Contains(prov)) {
					imperatorProvsReferencedMoreThanOnce.Add(prov);
				}
				referencedImperatorProvs.Add(prov);
			}
			foreach (var prov in mapping.CK3Provinces) {
				if (referencedCK3Provs.Contains(prov)) {
					ck3ProvsReferencedMoreThanOnce.Add(prov);
				}
				referencedCK3Provs.Add(prov);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		parser.ParseStream(reader);

		if (imperatorProvsReferencedMoreThanOnce.Any()) {
			Logger.Warn($"I:R provinces referenced more than once: {string.Join(", ", imperatorProvsReferencedMoreThanOnce)}");
		}
		if (ck3ProvsReferencedMoreThanOnce.Any()) {
			Logger.Warn($"CK3 provinces referenced more than once: {string.Join(", ", ck3ProvsReferencedMoreThanOnce)}");
		}
	}
}