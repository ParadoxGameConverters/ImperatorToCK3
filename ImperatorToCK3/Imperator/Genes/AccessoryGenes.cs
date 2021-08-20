using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Imperator.Genes {
	public class AccessoryGenes : Parser {
		public uint Index { get; private set; } = 0;
		public Dictionary<string, AccessoryGene> Genes { get; private set; } = new();

		public AccessoryGenes() { }
		public AccessoryGenes(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("index", reader => {
				Index = (uint)new SingleInt(reader).Int;
			});
			RegisterRegex(CommonRegexes.StringRegex, (reader, geneName) => {
				Genes.Add(geneName, new AccessoryGene(reader));
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
	}
}
