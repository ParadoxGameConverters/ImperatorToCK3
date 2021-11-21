using commonItems;

namespace ImperatorToCK3.Imperator.Jobs {
	public class Governorship {
		public ulong CountryId { get; private set; } = 0;
		public ulong CharacterId { get; private set; } = 0;
		public Date StartDate { get; private set; } = new(1, 1, 1);
		public string RegionName { get; private set; } = string.Empty;

		public Governorship(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("who", reader => CountryId = ParserHelpers.GetULong(reader));
			parser.RegisterKeyword("character", reader => CharacterId = ParserHelpers.GetULong(reader));
			parser.RegisterKeyword("start_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				StartDate = new Date(dateStr, AUC: true);
			});
			parser.RegisterKeyword("governorship", reader => RegionName = ParserHelpers.GetString(reader));
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			parser.ParseStream(reader);
		}
	}
}
