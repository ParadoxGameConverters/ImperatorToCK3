using commonItems;

namespace ImperatorToCK3.CommonUtils.Genes {
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
			RegisterKeyword("special_genes", reader => {
				var db = new GenesDB(reader);
				Genes = db.Genes;
			});
			RegisterKeyword("accessory_genes", reader =>
				Genes = new AccessoryGenes(reader)
			);
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
	}
}
