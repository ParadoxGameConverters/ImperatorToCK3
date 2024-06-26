using commonItems;
using commonItems.Collections;
using System.Linq;

namespace ImperatorToCK3.CommonUtils.Genes;

public sealed class AccessoryGene : Gene, IIdentifiable<string> {
	public string Id { get; }
	public uint? Index { get; private set; }
	public IdObjectCollection<string, AccessoryGeneTemplate> GeneTemplates { get; } = new();

	public AccessoryGene(string id, BufferedReader reader) {
		Id = id;
		
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.RegisterKeyword("inheritable", reader => Inheritable = reader.GetBool());
		parser.RegisterKeyword("group", ParserHelpers.IgnoreItem);
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
		Logger.Warn($"{Id}: could not find accessory gene template by index from DNA: {indexInDna}");
		// Fallback: return first element.
		return GeneTemplates.First();
	}
}