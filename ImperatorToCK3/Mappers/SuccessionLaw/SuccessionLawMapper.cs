using System;
using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.SuccessionLaw {
	public class SuccessionLawMapper : Parser {
		private readonly Dictionary<string, SortedSet<string>> impToCK3SuccessionLawMap = new();
		public SuccessionLawMapper(string filePath) {
			Logger.Info("Parsing succession law mappings.");
			RegisterKeys();
			ParseFile(filePath);
			ClearRegisteredRules();
			Logger.Info($"Loaded {impToCK3SuccessionLawMap.Count} succession law links.");
		}
		public SuccessionLawMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("link", (reader) => {
				var mapping = new SuccessionLawMapping(reader);
				if (mapping.Ck3SuccessionLaws.Count == 0) {
					Logger.Warn("SuccessionLawMapper: link with no CK3 successions laws");
					return;
				}
				if (impToCK3SuccessionLawMap.TryAdd(mapping.ImperatorLaw, mapping.Ck3SuccessionLaws) == false) {
					impToCK3SuccessionLawMap[mapping.ImperatorLaw].UnionWith(mapping.Ck3SuccessionLaws);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
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
}
