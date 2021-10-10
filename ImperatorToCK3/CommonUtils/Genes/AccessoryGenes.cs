using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes {
	public class AccessoryGenes : Parser {
		public uint? Index { get; private set; }
		public Dictionary<string, AccessoryGene> Genes { get; set; } = new();

		public AccessoryGenes() { }
		public void LoadGenes(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.String, (reader, geneName) => {
				Genes.Add(geneName, new AccessoryGene(reader));
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
