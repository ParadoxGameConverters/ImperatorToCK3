using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.SuccessionLaw;

internal sealed class SuccessionLawMapper {
	private readonly List<SuccessionLawMapping> mappings = [];

	public SuccessionLawMapper() { }
	public SuccessionLawMapper(string filePath, OrderedDictionary<string, bool> ck3ModFlags) {
		Logger.Info("Parsing succession law mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseLiquidFile(filePath, ck3ModFlags);
		Logger.Info($"Loaded {mappings.Count} succession law links.");

		Logger.IncrementProgress();
	}
	public SuccessionLawMapper(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			mappings.Add(new(reader));
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public OrderedSet<string> GetCK3LawsForImperatorLaws(SortedSet<string> impLaws, string? irGovernment, IReadOnlyCollection<string> enabledCK3Dlcs) {
		var lawsToReturn = new OrderedSet<string>();
		foreach (var impLaw in impLaws) {
			foreach (var mapping in mappings) {
				var match = mapping.Match(impLaw, irGovernment, enabledCK3Dlcs);
				if (match is null) {
					continue;
				}
				
				lawsToReturn.UnionWith(match);
				break;
			}
		}
		return lawsToReturn;
	}
}