using commonItems;
using commonItems.Collections;
using System.Linq;

namespace ImperatorToCK3.CommonUtils.Genes; 

public sealed class MorphGene : Gene, IIdentifiable<string> {
	public string Id { get; }
	public uint? Index { get; private set; }
	public IdObjectCollection<string, MorphGeneTemplate> GeneTemplates { get; } = [];

	public MorphGene(string id, BufferedReader geneReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.RegisterKeyword("ugliness_feature_categories", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("can_have_portrait_extremity_shift", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("visible", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("group", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("inheritable", ParserHelpers.IgnoreItem);
		parser.RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) =>
			GeneTemplates.AddOrReplace(new MorphGeneTemplate(geneTemplateName, reader))
		);
		parser.ParseStream(geneReader);
	}
	public MorphGeneTemplate? GetGeneTemplateByIndex(uint indexInDna) {
		foreach (var template in GeneTemplates) {
			if (template.Index == indexInDna) {
				return template;
			}
		}
		Logger.Warn($"{Id}: could not find morph gene template by index from DNA: {indexInDna}");
		// Fallback: return first element or null if none.
		return GeneTemplates.FirstOrDefault();
	}
}