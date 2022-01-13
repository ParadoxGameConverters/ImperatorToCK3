using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.CommonUtils.Genes {
	public class SpecialGenes : Parser {
		public AccessoryGenes Genes { get; private set; } = new();

		public SpecialGenes(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("accessory_genes", reader => {
				Genes.LoadGenes(reader);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
