﻿using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Province;

internal sealed class ProvinceMappingsVersion {
	public List<ProvinceMapping> Mappings { get; } = [];
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
		parser.RegisterKeyword("triangulation_pair", ParserHelpers.IgnoreItem);
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		parser.ParseStream(reader);

		if (imperatorProvsReferencedMoreThanOnce.Count != 0) {
			Logger.Warn($"I:R provinces referenced more than once: {string.Join(", ", imperatorProvsReferencedMoreThanOnce)}");
		}
		if (ck3ProvsReferencedMoreThanOnce.Count != 0) {
			Logger.Warn($"CK3 provinces referenced more than once: {string.Join(", ", ck3ProvsReferencedMoreThanOnce)}");
		}
	}
}