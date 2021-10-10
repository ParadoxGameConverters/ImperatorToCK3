using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.Mappers.Gene {
	public class AccessoryGeneMapper {
		public Dictionary<string, string> BeardMappings { get; private set; } = new();

		public AccessoryGeneMapper(string mappingsFilePath) {
			var parser = new Parser();
			parser.RegisterKeyword("beards", reader =>
				BeardMappings = ParserHelpers.GetAssignments(reader)
			);
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseFile(mappingsFilePath);
		}
	}
}
