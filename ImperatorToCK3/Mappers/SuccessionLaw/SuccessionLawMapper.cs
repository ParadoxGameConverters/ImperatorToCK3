using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.Mappers.SuccessionLaw {
	public class SuccessionLawMapper : Parser {
		private Dictionary<string, SortedSet<string>> impToCK3SuccessionLawMap = new();
		public SuccessionLawMapper() {
			Logger.Log(LogLevel.Info, "arsing succession law mappings.");
			RegisterKeys();
			ParseFile("configurables/succession_law_map.txt");
			ClearRegisteredRules();
			Logger.Log(LogLevel.Info, "Loaded " + impToCK3SuccessionLawMap.Count + " succession law links.");
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
					throw new MissingFieldException("SuccessionLawMapper: link with no CK3 successions laws");
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
				var gotValue = impToCK3SuccessionLawMap.TryGetValue(impLaw, out var ck3Laws);
				if (gotValue) {
					lawsToReturn.UnionWith(ck3Laws);
				}
			}
			return lawsToReturn;
		}
	}
}
