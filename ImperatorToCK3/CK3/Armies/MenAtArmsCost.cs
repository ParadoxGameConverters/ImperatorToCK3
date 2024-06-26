using commonItems;
using commonItems.Serialization;
using commonItems.SourceGenerators;

namespace ImperatorToCK3.CK3.Armies;

[SerializationByProperties]
public sealed partial class MenAtArmsCost : IPDXSerializable {
	[SerializedName("gold")] public double? Gold { get; set; }
	[SerializedName("piety")] public double? Piety { get; set; }
	[SerializedName("prestige")] public double? Prestige { get; set; }

	public MenAtArmsCost() { }
	public MenAtArmsCost(BufferedReader costReader, ScriptValueCollection scriptValues) {
		var parser = new Parser();
		parser.RegisterKeyword("gold", reader => Gold = reader.GetDouble(scriptValues));
		parser.RegisterKeyword("piety", reader => Piety = reader.GetDouble(scriptValues));
		parser.RegisterKeyword("prestige", reader => Prestige = reader.GetDouble(scriptValues));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(costReader);
	}

	public static MenAtArmsCost operator /(MenAtArmsCost cost, double divisor) {
		return new MenAtArmsCost {
			Gold = cost.Gold / divisor,
			Piety = cost.Piety / divisor,
			Prestige = cost.Prestige / divisor
		};
	}

	public static MenAtArmsCost operator *(MenAtArmsCost cost, double multiplier) {
		return new MenAtArmsCost {
			Gold = cost.Gold * multiplier,
			Piety = cost.Piety * multiplier,
			Prestige = cost.Prestige * multiplier
		};
	}
}