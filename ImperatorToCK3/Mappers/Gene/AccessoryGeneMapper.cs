using commonItems;
using System;
using System.Collections.Generic;
using Assignment = System.Collections.Generic.KeyValuePair<string, string>;

namespace ImperatorToCK3.Mappers.Gene;

internal sealed class AccessoryGeneMapper {
	private Dictionary<string, List<Assignment>> ObjectToObjectMappings { get; } = [];
	private Dictionary<string, List<Assignment>> TemplateToTemplateMappings { get; } = [];

	public AccessoryGeneMapper(string mappingsFilePath) {
		var objectToObjectMappingsParser = new Parser(implicitVariableHandling: true);
		objectToObjectMappingsParser.RegisterRegex(CommonRegexes.String, (reader, geneName) => {
			ObjectToObjectMappings[geneName] = reader.GetAssignments();
		});
		objectToObjectMappingsParser.IgnoreAndLogUnregisteredItems();

		var templateToTemplateMappingsParser = new Parser(implicitVariableHandling: true);
		templateToTemplateMappingsParser.RegisterRegex(CommonRegexes.String, (reader, geneName) => {
			TemplateToTemplateMappings[geneName] = reader.GetAssignments();
		});

		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterKeyword("object_to_object", objectToObjectMappingsParser.ParseStream);
		parser.RegisterKeyword("template_to_template", templateToTemplateMappingsParser.ParseStream);
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(mappingsFilePath);
	}

	internal string? GetObjectFromObject(string irGeneName, string irObjectName) {
		if (!ObjectToObjectMappings.TryGetValue(irGeneName, out var mappings)) {
			return null;
		}

		for (int i = 0; i < mappings.Count; ++i) {
			var mapping = mappings[i];
			if (mapping.Key == irObjectName) {
				return mapping.Value;
			}
		}

		return null;
	}

	internal string? GetTemplateFromTemplate(string irGeneName, string irTemplateName, string[] validCK3TemplateIds) {
		if (!TemplateToTemplateMappings.TryGetValue(irGeneName, out var mappings)) {
			Logger.Warn($"No template-to-template mappings found for gene {irGeneName}!");
			return null;
		}

		for (int i = 0; i < mappings.Count; ++i) {
			var mapping = mappings[i];
			if (mapping.Key != irTemplateName) {
				continue;
			}
			if (Array.IndexOf(validCK3TemplateIds, mapping.Value) >= 0) {
				return mapping.Value;
			}
		}

		return null;
	}

	internal string? GetFallbackTemplateForGene(string irGeneName, string[] validCK3TemplateIds) {
		if (!TemplateToTemplateMappings.TryGetValue(irGeneName, out var mappings)) {
			return null;
		}

		for (int i = 0; i < mappings.Count; ++i) {
			var template = mappings[i].Value;
			if (Array.IndexOf(validCK3TemplateIds, template) >= 0) {
				return template;
			}
		}

		return null;
	}
}