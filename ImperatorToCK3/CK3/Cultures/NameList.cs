using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImperatorToCK3.CK3.Cultures; 

public class NameList : IIdentifiable<string> {
	public string Id { get; }
	private readonly OrderedSet<string> maleNames = new();
	private readonly OrderedSet<string> femaleNames = new();
	public IReadOnlyCollection<string> MaleNames => maleNames.ToImmutableList();
	public IReadOnlyCollection<string> FemaleNames => femaleNames.ToImmutableList();

	public NameList(string id, BufferedReader nameListReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("male_names", reader => {
			maleNames.UnionWith(reader.GetStrings());
		});
		parser.RegisterKeyword("female_names", reader => {
			femaleNames.UnionWith(reader.GetStrings());
		});
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(nameListReader);
	}
}