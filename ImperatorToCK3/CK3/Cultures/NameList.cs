using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.CK3.Cultures; 

public class NameList : IIdentifiable<string> {
	public string Id { get; }
	public OrderedSet<string> MaleNames { get; }= new();
	public OrderedSet<string> FemaleNames { get; } = new();

	public NameList(string id, BufferedReader nameListReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("male_names", reader => {
			MaleNames.UnionWith(reader.GetStrings());
		});
		parser.RegisterKeyword("female_names", reader => {
			FemaleNames.UnionWith(reader.GetStrings());
		});
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(nameListReader);
	}
}