using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Gene {
	public class AccessoryGeneMapper {
		public Dictionary<string, Dictionary<string, string>> Mappings { get; } = new();

		public AccessoryGeneMapper(string mappingsFilePath) {
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.String, (reader, geneName) => Mappings[geneName] = reader.GetAssignments());
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseFile(mappingsFilePath);
		}
	}
}
