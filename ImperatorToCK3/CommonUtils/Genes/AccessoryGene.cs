using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils.Genes;

public class AccessoryGene : Gene {
	public uint? Index { get; private set; }
	public IdObjectCollection<string, AccessoryGeneTemplate> GeneTemplates { get; } = new();

	public AccessoryGene(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.RegisterKeyword("inheritable", reader => Inheritable = reader.GetBool());
		parser.RegisterKeyword("group", ParserHelpers.IgnoreAndLogItem);
		parser.RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) =>
			GeneTemplates.AddOrReplace(new AccessoryGeneTemplate(geneTemplateName, reader))
		);
		parser.IgnoreUnregisteredItems();
	}
	public AccessoryGeneTemplate GetGeneTemplateByIndex(uint indexInDna) {
		foreach (var template in GeneTemplates) {
			if (template.Index == indexInDna) {
				return template;
			}
		}
		Logger.Warn($"Could not find gene template by index from DNA: {indexInDna}");
		return GeneTemplates.First(); // fallback: return first element
	}
}