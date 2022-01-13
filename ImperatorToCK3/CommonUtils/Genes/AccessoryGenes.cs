using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes;

public class AccessoryGenes {
	public Dictionary<string, AccessoryGene> Genes { get; set; } = new();

	public AccessoryGenes() { }
	public void LoadGenes(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.String, (reader, geneName) => {
			Genes.Add(geneName, new AccessoryGene(reader));
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}