using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Gene; 

public class AccessoryGeneMapper {
	public Dictionary<string, IDictionary<string, string>> ObjectToObjectMappings { get; } = new();
	public Dictionary<string, IDictionary<string, string>> TemplateToTemplateMappings { get; } = new();

	public AccessoryGeneMapper(string mappingsFilePath) {
		var objectToObjectMappingsParser = new Parser();
		objectToObjectMappingsParser.RegisterRegex(CommonRegexes.String, (reader, geneName) => {
			ObjectToObjectMappings[geneName] = reader.GetAssignments();
		});
		objectToObjectMappingsParser.IgnoreAndLogUnregisteredItems();
		
		var templateToTemplateMappingsParser = new Parser();
		templateToTemplateMappingsParser.RegisterRegex(CommonRegexes.String, (reader, geneName) => {
			TemplateToTemplateMappings[geneName] = reader.GetAssignments();
		});
		
		var parser = new Parser();
		parser.RegisterKeyword("object_to_object", objectToObjectMappingsParser.ParseStream);
		parser.RegisterKeyword("template_to_template", templateToTemplateMappingsParser.ParseStream);
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(mappingsFilePath);
	}
}