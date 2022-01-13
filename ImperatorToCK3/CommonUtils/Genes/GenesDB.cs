using commonItems;
using System.Linq;

namespace ImperatorToCK3.CommonUtils.Genes;

public class GenesDB {
	public AccessoryGenes Genes { get; private set; } = new();

	public GenesDB() { }
	public GenesDB(string path) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(path);
	}
	public GenesDB(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("special_genes", reader => {
			var specialGenes = new SpecialGenes(reader);
			Genes.Genes = Genes.Genes
				.Concat(specialGenes.Genes.Genes)
				.GroupBy(d => d.Key)
				.ToDictionary(d => d.Key, d => d.Last().Value);
		});
		parser.RegisterKeyword("accessory_genes", reader =>
			Genes.LoadGenes(reader)
		);
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}