using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes; 

public class MorphGene : Gene {
	public Dictionary<string, MorphGeneTemplate> GeneTemplates { get; } = new();
	
	public MorphGene(BufferedReader geneReader) {
		var parser = new Parser();
		parser.RegisterKeyword("index", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("ugliness_feature_categories", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("group", ParserHelpers.IgnoreItem);
		parser.RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) => {
			GeneTemplates[geneTemplateName] = new MorphGeneTemplate(reader);
		});
		parser.ParseStream(geneReader);
	}
}