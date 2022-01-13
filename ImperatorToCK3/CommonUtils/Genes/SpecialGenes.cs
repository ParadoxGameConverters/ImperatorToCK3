using commonItems;

namespace ImperatorToCK3.CommonUtils.Genes;

public class SpecialGenes {
	public AccessoryGenes Genes { get; private set; } = new();

	public SpecialGenes(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("accessory_genes", reader => {
			Genes.LoadGenes(reader);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}