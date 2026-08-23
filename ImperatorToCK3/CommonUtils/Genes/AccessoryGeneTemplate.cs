using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Genes;

internal sealed class AccessoryGeneTemplate : IIdentifiable<string> {
	public string Id { get; }
	public uint Index { get; private set; } = 0;
	public Dictionary<string, WeightBlock> AgeSexWeightBlocks { get; } = [];

	public AccessoryGeneTemplate(string id, BufferedReader reader) {
		Id = id;

		var parser = new Parser(implicitVariableHandling: true);
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.RegisterKeyword("set_tags", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("male", (reader) => AddAgeSexWeightBlock("male", reader));
		parser.RegisterKeyword("female", (reader) => AddAgeSexWeightBlock("female", reader));
		parser.RegisterKeyword("boy", (reader) => AddAgeSexWeightBlock("boy", reader));
		parser.RegisterKeyword("girl", (reader) => AddAgeSexWeightBlock("girl", reader));
		parser.IgnoreUnregisteredItems();
	}

	private void AddAgeSexWeightBlock(string ageSexStr, BufferedReader reader) {
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
	}

	public int ObjectCountForAgeSex(string ageSex) {
		return AgeSexWeightBlocks.TryGetValue(ageSex, out var weightBlock) ? weightBlock.ObjectCount : 0;
	}

	public bool ContainsObjectForAgeSex(string ageSex, string objectName) {
		return AgeSexWeightBlocks.TryGetValue(ageSex, out var weightBlock) && weightBlock.ContainsObject(objectName);
	}
}