using System.Collections.Generic;
using System.Linq;
using commonItems;

namespace ImperatorToCK3.Imperator.Genes {
	public class AccessoryGene : Parser {
		public uint Index { get; private set; } = 0;
		public bool Inheritable { get; private set; } = false;
		public Dictionary<string, AccessoryGeneTemplate> GeneTemplates { get; private set; } = new();

		public AccessoryGene(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("index", reader => {
				Index = (uint)new SingleInt(reader).Int;
			});
			RegisterKeyword("inheritable", reader => {
				if (new SingleString(reader).String == "yes") {
					Inheritable = true;
				}
			});
			RegisterRegex(CommonRegexes.StringRegex, (reader, geneTemplateName) => {
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
			Logger.Log(LogLevel.Warning, "Could not find gene template by index from DNA: " + indexInDna);
			return GeneTemplates.First(); // fallback: return first element
		}
	}
}
