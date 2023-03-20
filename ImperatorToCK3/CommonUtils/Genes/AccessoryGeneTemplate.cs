using commonItems;
using commonItems.Collections;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes;

public class AccessoryGeneTemplate : IIdentifiable<string> {
	public string Id { get; }
	public uint Index { get; private set; } = 0;
	public Dictionary<string, WeightBlock> AgeSexWeightBlocks { get; } = new();

	public AccessoryGeneTemplate(string id, BufferedReader reader) {
		Id = id;
		
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.RegisterKeyword("set_tags", ParserHelpers.IgnoreAndLogItem);
		parser.RegisterRegex("male|female|boy|girl", (reader, ageSexStr) => {
			var stringOfItem = new StringOfItem(reader).ToString();
			var tempStream = new BufferedReader(stringOfItem);
			if (stringOfItem.Contains('{')) { // for full blocks: "male = { 6 = hoodie 7 = t_shirt }"
				var ageSexBlock = new WeightBlock(tempStream);
				AgeSexWeightBlocks.Add(ageSexStr, ageSexBlock);
			} else { // for copies: "boy = male"
				if (AgeSexWeightBlocks.TryGetValue(stringOfItem, out var blockToCopy)) {
					AgeSexWeightBlocks.Add(ageSexStr, blockToCopy);
				}
			}
		});
		parser.IgnoreUnregisteredItems();
	}
}