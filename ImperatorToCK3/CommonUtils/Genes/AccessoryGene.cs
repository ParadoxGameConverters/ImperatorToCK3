using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils.Genes;

public class AccessoryGene : Gene {
	public Dictionary<string, AccessoryGeneTemplate> GeneTemplates { get; } = new();

	public AccessoryGene(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.RegisterKeyword("inheritable", reader => Inheritable = reader.GetPDXBool());
		parser.RegisterKeyword("group", ParserHelpers.IgnoreAndLogItem);
		parser.RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) =>
			GeneTemplates.Add(geneTemplateName, new AccessoryGeneTemplate(reader))
		);
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
	}
	public KeyValuePair<string, AccessoryGeneTemplate> GetGeneTemplateByIndex(uint indexInDna) {
		foreach (var geneTemplatePair in GeneTemplates) {
			if (geneTemplatePair.Value.Index == indexInDna) {
				return geneTemplatePair;
			}
		}
		Logger.Warn($"Could not find gene template by index from DNA: {indexInDna}");
		return GeneTemplates.First(); // fallback: return first element
	}
}