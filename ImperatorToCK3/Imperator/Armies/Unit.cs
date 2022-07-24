using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Armies;

public class Unit : IIdentifiable<ulong> {
	public ulong Id { get; }
	public bool IsArmy { get; set; } = true;
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
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(legionReader);
	}
}