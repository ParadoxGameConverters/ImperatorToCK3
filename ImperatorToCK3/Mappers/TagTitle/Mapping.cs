using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.TagTitle {
	public class Mapping {
		public string? RankMatch(string imperatorTagOrRegion, string rank) {
			if (this.imperatorTagOrRegion != imperatorTagOrRegion) {
				return null;
			}
			if (ranks.Count > 0 && !ranks.Contains(rank)) {
				return null;
			}
			return ck3Title;
		}

		private string ck3Title = string.Empty;
		private string imperatorTagOrRegion = string.Empty;
		private readonly SortedSet<string> ranks = new();

		private static readonly Parser parser = new();
		private static Mapping mappingToReturn = new();
		static Mapping() {
			parser.RegisterKeyword("ck3", reader => {
				mappingToReturn.ck3Title = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("imp", reader => {
				mappingToReturn.imperatorTagOrRegion = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("rank", reader => {
				mappingToReturn.ranks.Add(ParserHelpers.GetString(reader));
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public static Mapping Parse(BufferedReader reader) {
			mappingToReturn = new Mapping();
			parser.ParseStream(reader);
			return mappingToReturn;
		}
	}
}
