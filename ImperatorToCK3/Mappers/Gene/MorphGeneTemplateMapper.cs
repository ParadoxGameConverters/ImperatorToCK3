using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Gene; 

public class MorphGeneTemplateMapper {
	private readonly Dictionary<string, IDictionary<string, string>> templateMappings = new(); // <geneName, <irTemplate, ck3Template>>
	
	public MorphGeneTemplateMapper(string mappingsFilePath) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, geneName) => {
			templateMappings[geneName] = reader.GetAssignments();
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(mappingsFilePath);
	}
	
	public string? GetCK3Template(string irGeneName, string irTemplateName) {
		if (!templateMappings.TryGetValue(irGeneName, out var templateMappingsForGene)) {
			Logger.Warn($"I:R gene {irGeneName} not found in morph gene template mappings!");
			return null;
		}

		if (templateMappingsForGene.TryGetValue(irTemplateName, out var ck3TemplateName)) {
			return ck3TemplateName;
		}

		Logger.Warn($"I:R template {irTemplateName} not found in morph gene template mappings!");
		return null;
	}
}