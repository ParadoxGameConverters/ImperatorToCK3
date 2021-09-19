using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Imperator.Jobs {
	public class Jobs {
		public List<Governorship> Governorships { get; } = new();

		public Jobs() { }
		public Jobs(BufferedReader reader) {
			var ignoredTokens = new SortedSet<string>();
			var parser = new Parser();
			parser.RegisterKeyword("province_job", reader => {
				var governorship = new Governorship(reader);
				Governorships.Add(governorship);
				var regionName = governorship.RegionName;
			});
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				ignoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});

			parser.ParseStream(reader);
			Logger.Debug("Ignored Jobs tokens: " + string.Join(", ", ignoredTokens));
		}
	}
}
