using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.SuccessionLaw;

public sealed class SuccessionLawMapper {
	private readonly Dictionary<string, SortedSet<string>> impToCK3SuccessionLawMap = new();

	public SuccessionLawMapper() { }
	public SuccessionLawMapper(string filePath) {
		Logger.Info("Parsing succession law mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(filePath);
		Logger.Info($"Loaded {impToCK3SuccessionLawMap.Count} succession law links.");

		Logger.IncrementProgress();
	}
	public SuccessionLawMapper(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			var mapping = new SuccessionLawMapping(reader);
			if (mapping.CK3SuccessionLaws.Count == 0) {
				Logger.Warn("SuccessionLawMapper: link with no CK3 successions laws");
				return;
			}
			if (!impToCK3SuccessionLawMap.TryAdd(mapping.ImperatorLaw, mapping.CK3SuccessionLaws)) {
				impToCK3SuccessionLawMap[mapping.ImperatorLaw].UnionWith(mapping.CK3SuccessionLaws);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public SortedSet<string> GetCK3LawsForImperatorLaws(SortedSet<string> impLaws) {
		var lawsToReturn = new SortedSet<string>();
		foreach (var impLaw in impLaws) {
			if (impToCK3SuccessionLawMap.TryGetValue(impLaw, out var ck3Laws)) {
				lawsToReturn.UnionWith(ck3Laws);
			}
		}
		return lawsToReturn;
	}
}