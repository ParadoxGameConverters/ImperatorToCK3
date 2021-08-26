using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.TagTitle {
	public class TagTitleMapping {
		public string? TagRankMatch(string imperatorTag, string rank) {
			if (this.imperatorTag != imperatorTag) {
				return null;
			}
			if (ranks.Count > 0 && !ranks.Contains(rank)) {
				return null;
			}
			return ck3Title;
		}

		private string ck3Title = string.Empty;
		private string imperatorTag = string.Empty;
		private readonly SortedSet<string> ranks = new();

		private static readonly Parser parser = new();
		private static TagTitleMapping mappingToReturn = new();
		static TagTitleMapping() {
			parser.RegisterKeyword("ck3", reader => {
				mappingToReturn.ck3Title = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("imp", reader => {
				mappingToReturn.imperatorTag = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("rank", reader => {
				mappingToReturn.ranks.Add(ParserHelpers.GetString(reader));
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public static TagTitleMapping Parse(BufferedReader reader) {
			mappingToReturn = new TagTitleMapping();
			parser.ParseStream(reader);
			return mappingToReturn;
		}
	}
}
