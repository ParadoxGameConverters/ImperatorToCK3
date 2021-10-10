using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Genes {
	public class AccessoryGene : Parser {
		public uint Index { get; private set; } = 0;
		public ParadoxBool Inheritable { get; private set; } = new(false);
		public Dictionary<string, AccessoryGeneTemplate> GeneTemplates { get; } = new();

		public AccessoryGene(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("index", reader => {
				Index = (uint)ParserHelpers.GetInt(reader);
			});
			RegisterKeyword("inheritable", reader =>
				Inheritable = new ParadoxBool(reader)
			);
			RegisterRegex(CommonRegexes.String, (reader, geneTemplateName) => {
				GeneTemplates.Add(geneTemplateName, new AccessoryGeneTemplate(reader));
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
		public KeyValuePair<string, AccessoryGeneTemplate> GetGeneTemplateByIndex(uint indexInDna) {
			foreach (var geneTemplatePair in GeneTemplates) {
				if (geneTemplatePair.Value.Index == indexInDna) {
					return geneTemplatePair;
				}
			}
			Logger.Warn("Could not find gene template by index from DNA: " + indexInDna);
			return GeneTemplates.First(); // fallback: return first element
		}
	}
}
