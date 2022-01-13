using commonItems;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CommonUtils.Genes;

public class GenesDB {
	public AccessoryGenes Genes { get; private set; } = new();

	public GenesDB() { }
	public GenesDB(string gamePath, IEnumerable<Mod> mods) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseGameFolder(Path.Combine("common", "genes"), gamePath, mods, true);
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