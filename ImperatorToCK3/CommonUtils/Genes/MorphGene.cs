using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.CommonUtils.Genes; 

public class MorphGene : Gene {
	public IdObjectCollection<string, MorphGeneTemplate> GeneTemplates { get; } = new();

	public MorphGene(BufferedReader geneReader) {
		var parser = new Parser();
		parser.RegisterKeyword("index", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("ugliness_feature_categories", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("can_have_portrait_extremity_shift", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("visible", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("group", ParserHelpers.IgnoreItem);
		parser.RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) => {
			GeneTemplates.AddOrReplace(new MorphGeneTemplate(geneTemplateName, reader));
		});
		parser.ParseStream(geneReader);
	}
}