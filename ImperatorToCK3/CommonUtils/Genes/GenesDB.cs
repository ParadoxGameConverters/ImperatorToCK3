using commonItems;
using commonItems.Collections;
using commonItems.Mods;

namespace ImperatorToCK3.CommonUtils.Genes;

public sealed class GenesDB {
	public IdObjectCollection<string, AccessoryGene> AccessoryGenes { get; } = new();
	public IdObjectCollection<string, MorphGene> MorphGenes { get; } = new();
	public IdObjectCollection<string, AccessoryGene> SpecialAccessoryGenes { get; } = new();
	public IdObjectCollection<string, MorphGene> SpecialMorphGenes { get; } = new();

	public GenesDB() { }
	public GenesDB(ModFilesystem modFS) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseGameFolder("common/genes", modFS, "txt", true, logFilePaths: true);
	}
	public GenesDB(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}

	private void RegisterKeys(Parser parser) {
		var accessoryGenesParser = new Parser();
		accessoryGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) =>
			AccessoryGenes.AddOrReplace(new AccessoryGene(geneName, geneReader))
		);
		accessoryGenesParser.IgnoreAndLogUnregisteredItems();
		
		var morphGenesParser = new Parser();
		morphGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) => {
			MorphGenes.AddOrReplace(new MorphGene(geneName, geneReader));
		});
		morphGenesParser.IgnoreAndLogUnregisteredItems();
		
		var specialAccessoryGenesParser = new Parser();
		specialAccessoryGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) =>
			SpecialAccessoryGenes.AddOrReplace(new AccessoryGene(geneName, geneReader))
		);
		specialAccessoryGenesParser.IgnoreAndLogUnregisteredItems();
		
		var specialMorphGenesParser = new Parser();
		specialMorphGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) => {
			SpecialMorphGenes.AddOrReplace(new MorphGene(geneName, geneReader));
		});

		var specialGenesParser = new Parser();
		specialGenesParser.RegisterKeyword("accessory_genes", specialAccessoryGenesParser.ParseStream);
		specialGenesParser.RegisterKeyword("morph_genes", specialMorphGenesParser.ParseStream);
		specialGenesParser.IgnoreAndLogUnregisteredItems();
		
		parser.RegisterKeyword("age_presets", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("decal_atlases", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("color_genes", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("special_genes", specialGenesParser.ParseStream);
		parser.RegisterKeyword("accessory_genes", accessoryGenesParser.ParseStream);
		parser.RegisterKeyword("morph_genes", morphGenesParser.ParseStream);
		parser.IgnoreAndLogUnregisteredItems();
	}
}