using commonItems;

namespace ImperatorToCK3.Imperator.Genes {
	public class GenesDB : Parser {
		public AccessoryGenes Genes { get; private set; } = new();

		public GenesDB() { }
		public GenesDB(string path) {
			RegisterKeys();
			ParseFile(path);
			ClearRegisteredRules();
		}
		public GenesDB(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("accessory_genes", reader =>
				Genes = new AccessoryGenes(reader)
			);
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
	}
}
