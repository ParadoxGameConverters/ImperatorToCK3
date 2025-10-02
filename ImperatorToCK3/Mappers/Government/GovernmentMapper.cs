using commonItems;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Government;

internal sealed class GovernmentMapper {
	private readonly List<GovernmentMapping> mappings = [];

	public GovernmentMapper(ICollection<string> ck3GovernmentIds) {
		Logger.Info("Parsing government mappings...");

		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile("configurables/government_map.txt");

		Logger.Info($"Loaded {mappings.Count} government links.");
		
		Logger.Debug("Removing invalid government links...");
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
			.ToArray();
		foreach (var mapping in toRemove) {
			mappings.Remove(mapping);
		}
	}
	
	public string? GetCK3GovernmentForImperatorGovernment(string irGovernmentId, TitleRank? rank, string? irCultureId, IReadOnlyCollection<string> enabledCK3Dlcs) {
		foreach (var mapping in mappings) {
			var match = mapping.Match(irGovernmentId, rank, irCultureId, enabledCK3Dlcs);
			if (match is not null) {
				return match;
			}
		}

		return null;
	}
}