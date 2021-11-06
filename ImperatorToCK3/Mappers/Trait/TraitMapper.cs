using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Trait {
	public class TraitMapper : Parser {
		private readonly Dictionary<string, string> impToCK3TraitMap = new();

		public TraitMapper(string filePath) {
			Logger.Info("Parsing trait mappings.");
			RegisterKeys();
			ParseFile(filePath);
			ClearRegisteredRules();
			Logger.Info($"Loaded {impToCK3TraitMap.Count} trait links.");
		}
		public TraitMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("link", reader => {
				var mapping = new TraitMapping(reader);
				if (mapping.CK3Trait is null) {
					return;
				}
				foreach (var imperatorTrait in mapping.ImpTraits) {
					impToCK3TraitMap.Add(imperatorTrait, mapping.CK3Trait);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public string? GetCK3TraitForImperatorTrait(string impTrait) {
			return impToCK3TraitMap.TryGetValue(impTrait, out var ck3Trait) ? ck3Trait : null;
		}
	}
}
