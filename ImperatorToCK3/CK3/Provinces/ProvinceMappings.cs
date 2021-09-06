using System.Collections.Generic;
using commonItems;

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
				var targetProvID = ulong.Parse(provIdString);
				var baseProvID = ParserHelpers.GetULong(reader);
				if (targetProvID != baseProvID) { // if left and right IDs are equal, no point in mapping
					if (Mappings.ContainsKey(targetProvID)) {
						Logger.Warn($"Duplicate province mapping for {targetProvID}, overwriting!");
					}
					Mappings[targetProvID] = baseProvID;
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
