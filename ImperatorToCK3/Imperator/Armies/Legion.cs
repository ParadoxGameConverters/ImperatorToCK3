using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Armies;

public class Legion : IIdentifiable<ulong> {
	public ulong Id { get; }
	public PDXBool IsArmy { get; set; } = new PDXBool(true);
	public ulong CountryId { get; set; }
	public ulong LeaderId { get; set; }

	public Legion(ulong id, BufferedReader reader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("is_army", reader => IsArmy = reader.GetPDXBool());
		parser.RegisterKeyword("country", reader => CountryId = reader.GetULong());
		parser.RegisterKeyword("leader", reader => LeaderId = reader.GetULong());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		parser.ParseStream(reader);
	}
}