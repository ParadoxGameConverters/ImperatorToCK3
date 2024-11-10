using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.War;

internal sealed class WarMapping {
	public SortedSet<string> ImperatorWarGoals { get; } = [];
	public string? CK3CasusBelli { get; private set; }

	static WarMapping() {
		parser.RegisterKeyword("ck3", reader => mappingToReturn.CK3CasusBelli = reader.GetString());
		parser.RegisterKeyword("ir", reader => mappingToReturn.ImperatorWarGoals.Add(reader.GetString()));
		parser.IgnoreAndLogUnregisteredItems();
	}
	public static WarMapping Parse(BufferedReader reader) {
		mappingToReturn = new WarMapping();
		parser.ParseStream(reader);
		return mappingToReturn;
	}

	private static WarMapping mappingToReturn = new();
	private static readonly Parser parser = new();
}