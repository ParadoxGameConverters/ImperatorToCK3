using commonItems;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Government {
	public class GovernmentMapper : Parser {
		private Dictionary<string, string> impToCK3GovernmentMap = new();

		public GovernmentMapper() {
			Logger.Info("Parsing government mappings.");
			RegisterKeys();
			ParseFile("configurables/government_map.txt");
			ClearRegisteredRules();
			Logger.Info("Loaded " + impToCK3GovernmentMap.Count + " government links.");
		}
		public GovernmentMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("link", (reader) => {
				var mapping = new GovernmentMapping(reader);
				if (string.IsNullOrEmpty(mapping.Ck3Government)) {
					throw new MissingFieldException("GovernmentMapper: link with no ck3Government");
				}

				foreach (var imperatorGovernment in mapping.ImperatorGovernments) {
					impToCK3GovernmentMap.Add(imperatorGovernment, mapping.Ck3Government);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public string? GetCK3GovernmentForImperatorGovernment(string impGovernment) {
			var gotValue = impToCK3GovernmentMap.TryGetValue(impGovernment, out var value);
			if (gotValue) {
				return value;
			}
			return null;
		}
	}
}
