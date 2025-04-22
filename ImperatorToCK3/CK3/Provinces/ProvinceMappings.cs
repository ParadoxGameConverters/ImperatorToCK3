using commonItems;
using commonItems.Mods;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Provinces;

/// <summary>
/// <para>
/// This class is used to read game/history/province_mapping in CK3.
/// CK3 uses province_mapping to set history for provinces that don't need unique entries.
/// </para>
/// <para>
/// Example unique entry in game/history/provinces:
/// 6872 = {
/// 	religion = coptic
/// 	culture = coptic
/// }
/// </para>
/// <para>
/// Example province_mapping in game/history/province_mapping:
/// 6874 = 6872
/// </para>
/// <para>Now 6874 history is same as 6872 history.</para>
/// </summary>
public sealed class ProvinceMappings : Dictionary<ulong, ulong> {
	public ProvinceMappings(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseGameFolder("history/province_mapping", ck3ModFS, "txt", recursive: true);
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provIdString) => {
			var targetProvId = ulong.Parse(provIdString);
			var baseProvId = reader.GetULong();
			if (targetProvId == baseProvId) { // if left and right IDs are equal, no point in mapping
				return;
			}

			if (ContainsKey(targetProvId)) {
				Logger.Debug($"Duplicate province mapping for {targetProvId}, overwriting!");
			}
			this[targetProvId] = baseProvId;
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
}