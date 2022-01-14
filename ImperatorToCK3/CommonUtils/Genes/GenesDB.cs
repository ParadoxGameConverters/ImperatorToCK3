using commonItems;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CommonUtils.Genes;

public class GenesDB {
	public Dictionary<string, AccessoryGene> Genes { get; set; } = new();

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
		var accessoryGenesParser = new Parser();
		accessoryGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) =>
			Genes.Add(geneName, new AccessoryGene(geneReader))
		);
		accessoryGenesParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		var specialGenesParser = new Parser();
		specialGenesParser.RegisterKeyword("accessory_genes", LoadAccessoryGenes);
		specialGenesParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		parser.RegisterKeyword("special_genes", LoadSpecialGenes);
		parser.RegisterKeyword("accessory_genes", LoadAccessoryGenes);
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		void LoadSpecialGenes(BufferedReader reader) {
			specialGenesParser.ParseStream(reader);
		}
		void LoadAccessoryGenes(BufferedReader reader) {
			accessoryGenesParser.ParseStream(reader);
		}
	}
}