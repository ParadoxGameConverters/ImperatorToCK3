using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.Imperator.Genes {
	public class AccessoryGeneTemplate : Parser {
		public uint Index { get; private set; } = 0;
		public Dictionary<string, WeightBlock> AgeSexWeightBlocks { get; private set; } = new();

		public AccessoryGeneTemplate(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("index", reader => {
				Index = (uint)new SingleInt(reader).Int;
			});
			RegisterRegex("male|female|boy|girl", (reader, ageSexStr) => {
				var stringOfItem = new StringOfItem(reader).String;
				if (stringOfItem.IndexOf('{') != -1) { // for full blocks: "male = { 6 = hoodie 7 = tshirt }"
					var tempStream = new BufferedReader(stringOfItem);
					var ageSexBlock = new WeightBlock(tempStream);
					AgeSexWeightBlocks.Add(ageSexStr, ageSexBlock);
				} else { // for copies: "boy = male"
					var sexAge = new SingleString(reader).String;
					if (AgeSexWeightBlocks.TryGetValue(sexAge, out var blockToCopy)) {
						AgeSexWeightBlocks.Add(ageSexStr, blockToCopy);
					}
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
	}
}
