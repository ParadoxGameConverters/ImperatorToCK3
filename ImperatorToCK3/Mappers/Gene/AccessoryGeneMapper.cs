using commonItems;
using System.Collections.Generic;
using System.Linq;
using Assignment = System.Collections.Generic.KeyValuePair<string, string>;

namespace ImperatorToCK3.Mappers.Gene; 

public sealed class AccessoryGeneMapper {
	private Dictionary<string, IList<Assignment>> ObjectToObjectMappings { get; } = [];
	private Dictionary<string, IList<Assignment>> TemplateToTemplateMappings { get; } = [];

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
	
	public string? GetObjectFromObject(string irGeneName, string irObjectName) {
		if (!ObjectToObjectMappings.TryGetValue(irGeneName, out var mappings)) {
			return null;
		}

		return mappings
			.Where(mapping => mapping.Key == irObjectName)
			.Select(mapping => mapping.Value)
			.FirstOrDefault();
	}
	
	public string? GetTemplateFromTemplate(string irGeneName, string irTemplateName, IEnumerable<string> validCK3TemplateIds) {
		if (!TemplateToTemplateMappings.TryGetValue(irGeneName, out var mappings)) {
			Logger.Warn($"No template-to-template mappings found for gene {irGeneName}!");
			return null;
		}
		
		return mappings
			.Where(mapping => mapping.Key == irTemplateName)
			.Select(mapping => mapping.Value)
			.Intersect(validCK3TemplateIds)
			.FirstOrDefault();
	}
	
	public string? GetFallbackTemplateForGene(string irGeneName, IEnumerable<string> validCK3TemplateIds) {
		if (!TemplateToTemplateMappings.TryGetValue(irGeneName, out var mappings)) {
			return null;
		}

		return mappings
			.Select(mapping => mapping.Value)
			.Intersect(validCK3TemplateIds)
			.FirstOrDefault();
	}
}