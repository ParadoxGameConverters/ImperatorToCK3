using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Characters;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Armies; 

public class MenAtArmsType : IIdentifiable<string>, IPDXSerializable {
	[NonSerialized] public string Id { get; }

	[SerializedName("can_recruit")] public StringOfItem CanRecruit { get; private set; } = new("{}");
	[SerializedName("stack")] public int Stack { get; private set; } = 100;
	
	[SerializedName("buy_cost")] public MenAtArmsCost? BuyCost { get; set; }
	[SerializedName("low_maintenance_cost")] public MenAtArmsCost? LowMaintenanceCost { get; set; }
	[SerializedName("buy_cost")] public MenAtArmsCost? HighMaintenanceCost { get; set; }

	[NonSerialized] private Dictionary<string, StringOfItem> attributes = new();
	[NonSerialized] public bool ToBeOutputted { get; } = false;
	
	public MenAtArmsType(string id, BufferedReader typeReader, ScriptValueCollection scriptValues) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("stack", reader => Stack = reader.GetInt());
		parser.RegisterKeyword("can_recruit", reader => CanRecruit = reader.GetStringOfItem());
		parser.RegisterKeyword("buy_cost", costReader => BuyCost = new MenAtArmsCost(costReader, scriptValues));
		parser.RegisterKeyword("low_maintenance_cost", costReader => LowMaintenanceCost = new MenAtArmsCost(costReader, scriptValues));
		parser.RegisterKeyword("high_maintenance_cost", costReader => HighMaintenanceCost = new MenAtArmsCost(costReader, scriptValues));
		parser.RegisterRegex(CommonRegexes.String, (reader, keyword) => {
			attributes[keyword] = reader.GetStringOfItem();
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(typeReader);
	}

	public MenAtArmsType(MenAtArmsType baseType, Character character, int stack, Date bookmarkDate) {
		ToBeOutputted = true;
		
		Id = $"IRToCK3_maa_{character.Id}_{baseType.Id}";
		CanRecruit = new StringOfItem($"{{ this = character:{character.Id} current_date<={bookmarkDate.ChangeByMonths(1)} }}");
		Stack = stack;
		
		BuyCost = new MenAtArmsCost {Gold = 0};
		var stackRatio = stack / baseType.Stack;
		if (baseType.LowMaintenanceCost is not null) {
			LowMaintenanceCost = baseType.LowMaintenanceCost * stackRatio;
		}
		if (baseType.HighMaintenanceCost is not null) {
			HighMaintenanceCost = baseType.HighMaintenanceCost * stackRatio;
		}

		attributes = new Dictionary<string, StringOfItem>(attributes);
	}

	public string Serialize(string indent, bool withBraces) {
		var sb = new StringBuilder();
		sb.AppendLine("{");
		sb.Append((this as IPDXSerializable).SerializeMembers(indent + '\t'));
		sb.AppendLine(PDXSerializer.Serialize(attributes, indent + '\t', withBraces: false));
		sb.AppendLine("}");

		return sb.ToString();
	}
}