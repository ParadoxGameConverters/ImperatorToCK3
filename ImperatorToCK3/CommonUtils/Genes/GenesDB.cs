using commonItems;
using System.Linq;

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
				var specialGenes = new SpecialGenes(reader);
				Genes.Genes = Genes.Genes
					.Concat(specialGenes.Genes.Genes)
					.GroupBy(d => d.Key)
					.ToDictionary(d => d.Key, d => d.Last().Value);
			});
			RegisterKeyword("accessory_genes", reader =>
				Genes.LoadGenes(reader)
			);
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
