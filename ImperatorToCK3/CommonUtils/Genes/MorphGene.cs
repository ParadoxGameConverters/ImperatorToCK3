using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes; 

public class MorphGene : Gene {
	public Dictionary<string, AccessoryGeneTemplate> GeneTemplates { get; } = new();
	public MorphGene(BufferedReader reader) {
		var parser = new Parser();
		parser.RegisterKeyword("group", ParserHelpers.IgnoreAndLogItem);
		parser.RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) =>
			GeneTemplates[geneTemplateName] = new AccessoryGeneTemplate(reader)
		);
		parser.ParseStream(reader);
	}
}