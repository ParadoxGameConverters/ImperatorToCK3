using commonItems;
using commonItems.Collections;
using commonItems.Serialization;

namespace ImperatorToCK3.CK3.Legends;

public class LegendSeed : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	private StringOfItem Body { get; }

	public LegendSeed(string id, BufferedReader reader) {
		Id = id;
		Body = reader.GetStringOfItem();
	}
	
	public string Serialize(string indent, bool withBraces) {
		return Body.ToString();
	}
}