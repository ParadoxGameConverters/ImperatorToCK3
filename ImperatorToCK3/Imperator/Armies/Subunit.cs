using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Armies; 

public class Subunit : IIdentifiable<ulong> {
	public ulong Id { get; }
	public string Category { get; private set; } = "levy";
	public string Type { get; private set; } = "light_infantry";
	public double Strength { get; private set; }
	public ulong CountryId { get; private set; }

	public Subunit(ulong id, BufferedReader subunitReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("category", reader => Category = reader.GetString());
		parser.RegisterKeyword("type", reader => Type = reader.GetString());
		parser.RegisterKeyword("country", reader => CountryId = reader.GetULong());
		parser.RegisterKeyword("strength", reader => Strength = reader.GetDouble());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.IgnoreAndStoreUnregisteredItems(IgnoredTokens);

		parser.ParseStream(subunitReader);
	}
	
	public static HashSet<string> IgnoredTokens { get; } = new();
}