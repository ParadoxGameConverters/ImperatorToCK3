using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes; 

public class MorphGene : Gene {
	private readonly Dictionary<string, MorphGeneTemplate> geneTemplates = new();
	public IReadOnlyDictionary<string, MorphGeneTemplate> GeneTemplates => geneTemplates;

	public MorphGene(BufferedReader geneReader) {
		var parser = new Parser();
		parser.RegisterKeyword("index", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("ugliness_feature_categories", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("can_have_portrait_extremity_shift", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("visible", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("group", ParserHelpers.IgnoreItem);
		parser.RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) => {
			geneTemplates[geneTemplateName] = new MorphGeneTemplate(reader);
		});
		parser.ParseStream(geneReader);
	}
}