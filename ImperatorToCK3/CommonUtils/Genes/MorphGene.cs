using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes; 

public class MorphGene : Gene {
	public Dictionary<string, MorphGeneTemplate> GeneTemplates { get; } = new();
	
	public MorphGene(BufferedReader geneReader) {
		var parser = new Parser();
		parser.RegisterKeyword("group", ParserHelpers.IgnoreAndLogItem);
		parser.RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) =>
			GeneTemplates[geneTemplateName] = new MorphGeneTemplate(reader)
		);
		parser.ParseStream(geneReader);
	}
}