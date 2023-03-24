using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Gene; 

public class MorphGeneTemplateMapper {
	private Dictionary<string, IDictionary<string, string>> templateMappings = new(); // <geneName, <irTemplate, ck3Template>>
	
	public MorphGeneTemplateMapper(string mappingsFilePath) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, geneName) => {
			templateMappings[geneName] = reader.GetAssignments();
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(mappingsFilePath);
	}
}