using commonItems;
using commonItems.Collections;
using commonItems.Mods;

namespace ImperatorToCK3.CommonUtils.Genes;

internal sealed class GenesDB {
	public IdObjectCollection<string, AccessoryGene> AccessoryGenes { get; } = new();
	public IdObjectCollection<string, MorphGene> MorphGenes { get; } = new();
	public IdObjectCollection<string, AccessoryGene> SpecialAccessoryGenes { get; } = new();
	public IdObjectCollection<string, MorphGene> SpecialMorphGenes { get; } = new();

	public GenesDB() { }
	public GenesDB(ModFilesystem modFS) {
		var parser = new Parser(implicitVariableHandling: true);
		RegisterKeys(parser);
		parser.ParseGameFolder("common/genes", modFS, "txt", recursive: true, logFilePaths: true);
	}
	public GenesDB(BufferedReader reader) {
		var parser = new Parser(implicitVariableHandling: true);
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}

	private void RegisterKeys(Parser parser) {
		var accessoryGenesParser = new Parser(implicitVariableHandling: true);
		accessoryGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) =>
			AccessoryGenes.AddOrReplace(new AccessoryGene(geneName, geneReader))
		);
		accessoryGenesParser.IgnoreAndLogUnregisteredItems();

		var morphGenesParser = new Parser(implicitVariableHandling: true);
		morphGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) => {
			MorphGenes.AddOrReplace(new MorphGene(geneName, geneReader));
		});
		morphGenesParser.IgnoreAndLogUnregisteredItems();
		
		var specialAccessoryGenesParser = new Parser(implicitVariableHandling: true);
		specialAccessoryGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) =>
			SpecialAccessoryGenes.AddOrReplace(new AccessoryGene(geneName, geneReader))
		);
		specialAccessoryGenesParser.IgnoreAndLogUnregisteredItems();
		
		var specialMorphGenesParser = new Parser(implicitVariableHandling: true);
		specialMorphGenesParser.RegisterRegex(CommonRegexes.String, (geneReader, geneName) => {
			SpecialMorphGenes.AddOrReplace(new MorphGene(geneName, geneReader));
		});

		var specialGenesParser = new Parser(implicitVariableHandling: true);
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