using commonItems;

namespace ImperatorToCK3.Imperator.Jobs {
	public class Governorship {
		public ulong CountryID { get; private set; } = 0;
		public ulong CharacterID { get; private set; } = 0;
		public Date StartDate { get; private set; } = new(1, 1, 1);
		public string RegionName { get; private set; } = string.Empty;

		public Governorship(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("who", reader => {
				CountryID = ParserHelpers.GetULong(reader);
			});
			parser.RegisterKeyword("character", reader => {
				CharacterID = ParserHelpers.GetULong(reader);
			});
			parser.RegisterKeyword("start_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				StartDate = new Date(dateStr);
			});
			parser.RegisterKeyword("governorship", reader => {
				RegionName = ParserHelpers.GetString(reader);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			parser.ParseStream(reader);
		}
	}
}
