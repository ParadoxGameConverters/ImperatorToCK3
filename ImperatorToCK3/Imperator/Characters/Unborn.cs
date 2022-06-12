using commonItems;

namespace ImperatorToCK3.Imperator.Characters; 

public class Unborn {
	public ulong? MotherId { get; private set; }
	public ulong? FatherId { get; private set; }
	public Date? BirthDate { get; private set; }

	public Unborn(BufferedReader unbornReader) {
		var parser = new Parser();
		parser.RegisterKeyword("mother", reader=>MotherId = reader.GetULong());
		parser.RegisterKeyword("father", reader=>FatherId = reader.GetULong());
		parser.RegisterKeyword("date", reader=>BirthDate = new Date(reader.GetString(), AUC: true));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(unbornReader);
	}
}