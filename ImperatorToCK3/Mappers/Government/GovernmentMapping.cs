using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Government;

public sealed class GovernmentMapping {
	public string CK3GovernmentId { get; private set; } = "";
	public SortedSet<string> ImperatorGovernmentIds { get; } = [];
	public SortedSet<string> ImperatorCultureIds { get; } = [];

	public GovernmentMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ck3", reader => CK3GovernmentId = reader.GetString());
		parser.RegisterKeyword("ir", reader => ImperatorGovernmentIds.Add(reader.GetString()));
		parser.RegisterKeyword("irCulture", reader => ImperatorCultureIds.Add(reader.GetString()));
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(mappingReader);
	}

	public string? Match(string irGovernmentId, string? irCultureId) {
		if (!ImperatorGovernmentIds.Contains(irGovernmentId)) {
			return null;
		}

		if (ImperatorCultureIds.Count != 0) {
			if (irCultureId is null) {
				return null;
			}
			if (!ImperatorCultureIds.Contains(irCultureId)) {
				return null;
			}
		}

		return CK3GovernmentId;
	}
}