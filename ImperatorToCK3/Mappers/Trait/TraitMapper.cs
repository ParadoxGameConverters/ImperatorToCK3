using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.Trait {
	public class TraitMapper : Parser {
		private readonly Dictionary<string, string> impToCK3TraitMap = new();

		public TraitMapper(string filePath) {
			Logger.Log(LogLevel.Info, "Parsing trait mappings.");
			RegisterKeys();
			ParseFile(filePath);
			ClearRegisteredRules();
			Logger.Log(LogLevel.Info, $"Loaded {impToCK3TraitMap.Count} trait links.");
		}
		public TraitMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("link", (reader) => {
				var mapping = new TraitMapping(reader);
				if (mapping.Ck3Trait is not null) {
					foreach (var imperatorTrait in mapping.ImpTraits) {
						impToCK3TraitMap.Add(imperatorTrait, mapping.Ck3Trait);
					}
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public string? GetCK3TraitForImperatorTrait(string impTrait) {
			if (impToCK3TraitMap.TryGetValue(impTrait, out var ck3Trait)) {
				return ck3Trait;
			}
			return null;
		}
	}
}
