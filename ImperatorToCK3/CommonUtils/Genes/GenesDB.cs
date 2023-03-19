using commonItems;
using commonItems.Mods;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes;

public class GenesDB {
	public Dictionary<string, AccessoryGene> AccessoryGenes { get; } = new();
	public Dictionary<string, MorphGene> MorphGenes { get; } = new();

	public GenesDB() { }
	public GenesDB(ModFilesystem modFS) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseGameFolder("common/genes", modFS, "txt", true);
	}
	public GenesDB(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}

	private void RegisterKeys(Parser parser) {
		var accessoryGenesParser = new Parser();
		accessoryGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) =>
			AccessoryGenes.Add(geneName, new AccessoryGene(geneReader))
		);
		accessoryGenesParser.IgnoreAndLogUnregisteredItems();

		var specialGenesParser = new Parser();
		specialGenesParser.RegisterKeyword("accessory_genes", LoadAccessoryGenes);
		specialGenesParser.IgnoreAndLogUnregisteredItems();
		
		var morphGenesParser = new Parser();
		morphGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) =>
			MorphGenes.Add(geneName, new MorphGene(geneReader))
		);

		parser.RegisterKeyword("special_genes", LoadSpecialGenes);
		parser.RegisterKeyword("accessory_genes", LoadAccessoryGenes);
		parser.RegisterKeyword("morph_genes", LoadMorphGenes);
		parser.IgnoreAndLogUnregisteredItems();

		void LoadSpecialGenes(BufferedReader reader) {
			specialGenesParser.ParseStream(reader);
		}
		void LoadAccessoryGenes(BufferedReader reader) {
			accessoryGenesParser.ParseStream(reader);
		}
		void LoadMorphGenes(BufferedReader reader) {
			morphGenesParser.ParseStream(reader);
		}
	}
}