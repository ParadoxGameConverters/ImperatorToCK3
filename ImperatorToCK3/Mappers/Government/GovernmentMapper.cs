using commonItems;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.Government;

public class GovernmentMapper {
	private readonly List<GovernmentMapping> mappings = new();

	public GovernmentMapper() {
		Logger.Info("Parsing government mappings...");

		var parser = new Parser();
		RegisterKeys(parser);
		var mappingsPath = Path.Combine("configurables", "government_map.txt");
		parser.ParseFile(mappingsPath);

		Logger.Info($"Loaded {mappings.Count} government links.");

		Logger.IncrementProgress();
	}
	public GovernmentMapper(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			var mapping = new GovernmentMapping(reader);
			if (string.IsNullOrEmpty(mapping.CK3GovernmentId)) {
				throw new MissingFieldException("GovernmentMapper: link with no ck3Government");
			}
			mappings.Add(mapping);
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	public string? GetCK3GovernmentForImperatorGovernment(string irGovernmentId, string? irCultureId) {
		foreach (var mapping in mappings) {
			var match = mapping.Match(irGovernmentId, irCultureId);
			if (match is not null) {
				return match;
			}
		}

		return null;
	}
}