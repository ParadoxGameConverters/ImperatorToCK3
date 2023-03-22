using commonItems;
using commonItems.Collections;
using commonItems.Mods;

namespace ImperatorToCK3.CommonUtils.Genes;

public class GenesDB {
	public IdObjectCollection<string, AccessoryGene> AccessoryGenes { get; } = new();
	public IdObjectCollection<string, MorphGene> MorphGenes { get; } = new();

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
			AccessoryGenes.Add(new AccessoryGene(geneName, geneReader))
		);
		accessoryGenesParser.IgnoreAndLogUnregisteredItems();
		
		var morphGenesParser = new Parser();
		morphGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) => {
			MorphGenes.Add(new MorphGene(geneName, geneReader));
		});
		morphGenesParser.IgnoreAndLogUnregisteredItems();

		var specialGenesParser = new Parser();
		specialGenesParser.RegisterKeyword("accessory_genes", LoadAccessoryGenes);
		specialGenesParser.RegisterKeyword("morph_genes", LoadMorphGenes);
		specialGenesParser.IgnoreAndLogUnregisteredItems();
		
		parser.RegisterKeyword("age_presets", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("decal_atlases", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("color_genes", ParserHelpers.IgnoreItem);
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