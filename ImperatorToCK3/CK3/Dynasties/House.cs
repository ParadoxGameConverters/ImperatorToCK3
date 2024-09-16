using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using commonItems.SourceGenerators;

namespace ImperatorToCK3.CK3.Dynasties;

[SerializationByProperties]
public sealed partial class House : IPDXSerializable, IIdentifiable<string> {
	[NonSerialized] public string Id { get; }
	
	[SerializedName("prefix")] public string? Prefix { get; private set; }
	[SerializedName("name")] public string? Name { get; private set; }
	[SerializedName("dynasty")] public string? DynastyId { get; private set; }
	[SerializedName("motto")] public string? Motto { get; private set; }
	[SerializedName("forced_coa_religiongroup")] public string? ForcedCoaReligionGroup { get; private set; }

	public House(string id, BufferedReader houseReader) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("prefix", reader => Prefix = reader.GetString());
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterKeyword("dynasty", reader => DynastyId = reader.GetString());
		parser.RegisterKeyword("motto", reader => Motto = reader.GetString());
		parser.RegisterKeyword("forced_coa_religiongroup", reader => ForcedCoaReligionGroup = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(houseReader);
	}
}