using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Provinces {
	/// <summary>
	/// This class is used to read game/history/province_mapping in CK3.
	/// CK3 uses province_mapping to set history for provinces that don't need unique entries.
	///
	/// Example unique entry in game/history/provinces:
	/// 6872 = {
	/// 	religion = coptic
	/// 	culture = coptic
	/// }
	///
	/// Example province_mapping in game/history/province_mapping:
	/// 6874 = 6872
	///
	/// Now 6874 history is same as 6872 history.
	/// </summary>
	public class ProvinceMappings : Parser {
		public ProvinceMappings(string filePath) {
			RegisterKeys();
			ParseFile(filePath);
			ClearRegisteredRules();
		}
		public Dictionary<ulong, ulong> Mappings { get; private set; } = new();

		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, provIdString) => {
				var targetProvId = ulong.Parse(provIdString);
				var baseProvId = ParserHelpers.GetULong(reader);
				if (targetProvId == baseProvId) { // if left and right IDs are equal, no point in mapping
					return;
				}

				if (Mappings.ContainsKey(targetProvId)) {
					Logger.Warn($"Duplicate province mapping for {targetProvId}, overwriting!");
				}
				Mappings[targetProvId] = baseProvId;
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
