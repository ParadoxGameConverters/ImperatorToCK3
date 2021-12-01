using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.DeathReason {
	public class DeathReasonMapper : Parser {
		public DeathReasonMapper() {
			Logger.Info("Parsing death reason mappings.");
			RegisterKeys();
			ParseFile("configurables/deathMappings.txt");
			ClearRegisteredRules();
			Logger.Info("Loaded " + impToCK3ReasonMap.Count + " death reason links.");
		}
		public DeathReasonMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		public string? GetCK3ReasonForImperatorReason(string impReason) {
			return impToCK3ReasonMap.TryGetValue(impReason, out var value) ? value : null;
		}

		private void RegisterKeys() {
			RegisterKeyword("link", reader => {
				var mapping = new DeathReasonMapping(reader);
				if (mapping.Ck3Reason == null) {
					return;
				}

				foreach (var impReason in mapping.ImpReasons) {
					impToCK3ReasonMap.Add(impReason, mapping.Ck3Reason);
				}
			});
		}
		private readonly Dictionary<string, string> impToCK3ReasonMap = new();
	}
}
