using commonItems;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.Government;

public class GovernmentMapper {
	private readonly Dictionary<string, string> impToCK3GovernmentMap = new();

	public GovernmentMapper() {
		Logger.Info("Parsing government mappings...");

		var parser = new Parser();
		RegisterKeys(parser);
		var mappingsPath = Path.Combine("configurables", "government_map.txt");
		parser.ParseFile(mappingsPath);

		Logger.Info($"Loaded {impToCK3GovernmentMap.Count} government links.");
	}
	public GovernmentMapper(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			var mapping = new GovernmentMapping(reader);
			if (string.IsNullOrEmpty(mapping.Ck3Government)) {
				throw new MissingFieldException("GovernmentMapper: link with no ck3Government");
			}

			foreach (var imperatorGovernment in mapping.ImperatorGovernments) {
				impToCK3GovernmentMap.Add(imperatorGovernment, mapping.Ck3Government);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public string? GetCK3GovernmentForImperatorGovernment(string impGovernment) {
		return impToCK3GovernmentMap.TryGetValue(impGovernment, out var value) ? value : null;
	}
}