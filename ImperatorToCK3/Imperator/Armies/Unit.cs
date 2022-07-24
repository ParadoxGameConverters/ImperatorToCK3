using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Security.Policy;

namespace ImperatorToCK3.Imperator.Armies;

public class Unit : IIdentifiable<ulong> {
	public ulong Id { get; }
	public bool IsArmy { get; private set; } = true;
	public bool IsLegion { get; private set; } = false;
	public ulong CountryId { get; set; }
	public ulong LeaderId { get; set; } // character id
	public ulong Location { get; set; } // province id
	public List<ulong> CohortIds { get; } = new();

	public Unit(ulong id, BufferedReader legionReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("is_army", reader => IsArmy = reader.GetBool());
		parser.RegisterKeyword("country", reader => CountryId = reader.GetULong());
		parser.RegisterKeyword("leader", reader => LeaderId = reader.GetULong());
		parser.RegisterKeyword("location", reader => Location = reader.GetULong());
		parser.RegisterKeyword("cohort", reader => CohortIds.Add(reader.GetULong()));
		parser.RegisterKeyword("legion", reader => {
			ParserHelpers.IgnoreItem(reader);
			IsLegion = true;
		});
		parser.IgnoreAndStoreUnregisteredItems(IgnoredTokens);

		parser.ParseStream(legionReader);
	}
	
	public static HashSet<string> IgnoredTokens { get; } = new();
}