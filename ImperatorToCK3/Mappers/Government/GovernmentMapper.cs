using commonItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Mappers.Government;

public sealed class GovernmentMapper {
	private readonly List<GovernmentMapping> mappings = new();

	public GovernmentMapper(ICollection<string> ck3GovernmentIds) {
		Logger.Info("Parsing government mappings...");

		var parser = new Parser();
		RegisterKeys(parser);
		var mappingsPath = Path.Combine("configurables", "government_map.txt");
		parser.ParseFile(mappingsPath);

		Logger.Info($"Loaded {mappings.Count} government links.");
		
		Logger.Debug("Removing invalid government links...");
		RemoveInvalidLinks(ck3GovernmentIds);
	}
	public GovernmentMapper(BufferedReader reader, ICollection<string> ck3GovernmentIds) { // used for testing only, TODO: remove
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
		
		RemoveInvalidLinks(ck3GovernmentIds);
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
	private void RemoveInvalidLinks(ICollection<string> ck3GovernmentIds) {
		var toRemove = mappings
			.Where(mapping => !ck3GovernmentIds.Contains(mapping.CK3GovernmentId))
			.ToList();
		foreach (var mapping in toRemove) {
			mappings.Remove(mapping);
		}
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