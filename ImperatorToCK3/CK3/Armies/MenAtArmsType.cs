using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Armies; 

public class MenAtArmsType : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }

	public StringOfItem CanRecruit { get; private set; } = new("{}");
	public int Stack { get; private set; } = 100;
	[commonItems.Serialization.NonSerialized] private double Cost { get; set; } = 100;
	public StringOfItem BuyCost => new($"{{ gold={Cost} }}");
	
	[commonItems.Serialization.NonSerialized] private Dictionary<string, StringOfItem> attributes = new();
	
	public MenAtArmsType(string id, BufferedReader typeReader, ScriptValueCollection scriptValues) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("stack", reader => Stack = reader.GetInt());
		parser.RegisterKeyword("can_recruit", reader => CanRecruit = reader.GetStringOfItem());
		parser.RegisterKeyword("buy_cost", costReader => {
			var buyCostParser = new Parser();
			buyCostParser.RegisterKeyword("gold", goldReader => {
				if (scriptValues.GetValueForString(goldReader.GetString()) is double goldValue) {
					Cost = goldValue;
				}
			});
			buyCostParser.IgnoreAndLogUnregisteredItems();
			buyCostParser.ParseStream(costReader);
		});
		parser.RegisterRegex(CommonRegexes.String, (reader, keyword) => {
			attributes[keyword] = reader.GetStringOfItem();
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(typeReader);
	}

	public string Serialize(string indent, bool withBraces) {
		var sb = new StringBuilder();
		sb.AppendLine("{");
		sb.Append((this as IPDXSerializable).SerializeMembers(indent));
		sb.AppendLine(PDXSerializer.Serialize(attributes, indent, withBraces: false));
		sb.AppendLine("}");

		return sb.ToString();
	}
}