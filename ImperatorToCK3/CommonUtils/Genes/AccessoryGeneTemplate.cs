using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes {
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
				Index = (uint)ParserHelpers.GetInt(reader);
			});
			RegisterKeyword("set_tags", ParserHelpers.IgnoreAndLogItem);
			RegisterRegex("male|female|boy|girl", (reader, ageSexStr) => {
				var stringOfItem = new StringOfItem(reader).String;
				var tempStream = new BufferedReader(stringOfItem);
				if (stringOfItem.IndexOf('{') != -1) { // for full blocks: "male = { 6 = hoodie 7 = tshirt }"
					var ageSexBlock = new WeightBlock(tempStream);
					AgeSexWeightBlocks.Add(ageSexStr, ageSexBlock);
				} else { // for copies: "boy = male"
					var sexAge = new SingleString(tempStream).String;
					if (AgeSexWeightBlocks.TryGetValue(sexAge, out var blockToCopy)) {
						AgeSexWeightBlocks.Add(ageSexStr, blockToCopy);
					}
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
	}
}
