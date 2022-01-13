using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes;

public class AccessoryGeneTemplate : Parser {
	public uint Index { get; private set; } = 0;
	public Dictionary<string, WeightBlock> AgeSexWeightBlocks { get; private set; } = new();

	public AccessoryGeneTemplate(BufferedReader reader) {
		RegisterKeys();
		ParseStream(reader);
		ClearRegisteredRules();
	}
	private void RegisterKeys() {
		RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		RegisterKeyword("set_tags", ParserHelpers.IgnoreAndLogItem);
		RegisterRegex("male|female|boy|girl", (reader, ageSexStr) => {
			var stringOfItem = new StringOfItem(reader).ToString();
			var tempStream = new BufferedReader(stringOfItem);
			if (stringOfItem.Contains('{')) { // for full blocks: "male = { 6 = hoodie 7 = tshirt }"
				var ageSexBlock = new WeightBlock(tempStream);
				AgeSexWeightBlocks.Add(ageSexStr, ageSexBlock);
			} else { // for copies: "boy = male"
				if (AgeSexWeightBlocks.TryGetValue(stringOfItem, out var blockToCopy)) {
					AgeSexWeightBlocks.Add(ageSexStr, blockToCopy);
				}
			}
		});
		RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
	}
}