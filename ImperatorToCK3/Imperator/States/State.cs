using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.States; 

public class State : IIdentifiable<ulong> {
	public ulong Id { get; }
	public ulong CapitalProvinceId { get; private set; }
	public string AreaId { get; private set; } = null!;
	public ulong CountryId { get; private set; }

	public State(ulong id, BufferedReader stateReader) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("capital", reader => CapitalProvinceId = reader.GetULong());
		parser.RegisterKeyword("area", reader => AreaId = reader.GetString());
		parser.RegisterKeyword("country", reader => CountryId = reader.GetULong());
		parser.IgnoreAndStoreUnregisteredItems(IgnoredKeywords);
		parser.ParseStream(stateReader);
	}

	public static OrderedSet<string> IgnoredKeywords { get; } = new();
}