using commonItems;
using System.Collections.Generic;
using System.Linq;
using Assignment = System.Collections.Generic.KeyValuePair<string, string>;

namespace ImperatorToCK3.Mappers.Gene;

public sealed class MorphGeneTemplateMapper {
	private readonly Dictionary<string, IList<Assignment>> templateMappings = []; // <geneName, <irTemplate, ck3Template>>
	
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

		var ck3TemplateName = templateMappingsForGene
			.Where(mapping => mapping.Key == irTemplateName)
			.Select(mapping => mapping.Value)
			.FirstOrDefault();
		if (ck3TemplateName is null) {
			Logger.Warn($"I:R template {irTemplateName} not found in morph gene template mappings!");
		}
		return ck3TemplateName;
	}
}