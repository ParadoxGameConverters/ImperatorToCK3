using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Gene {
	public class AccessoryGeneMapper {
		public Dictionary<string, string> BeardMappings { get; private set; } = new();

		public AccessoryGeneMapper(string mappingsFilePath) {
			var parser = new Parser();
			parser.RegisterKeyword("beards", reader => BeardMappings = reader.GetAssignments());
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseFile(mappingsFilePath);
		}
	}
}
