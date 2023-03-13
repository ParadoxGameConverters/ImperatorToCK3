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
		parser.RegisterKeyword("male_names", maleNamesReader => {
			var maleNamesBlockParser = new Parser();
			maleNamesBlockParser.RegisterRegex(CommonRegexes.Integer, (weightedBlockReader, _) => {
				maleNames.UnionWith(weightedBlockReader.GetStrings());
			});
			maleNamesBlockParser.RegisterRegex(CommonRegexes.String, (_, nameStr) => {
				maleNames.Add(nameStr);
			});
			maleNamesBlockParser.IgnoreAndLogUnregisteredItems();
			maleNamesBlockParser.ParseStream(maleNamesReader);
		});
		parser.RegisterKeyword("female_names", reader => {
			var femaleNamesBlockParser = new Parser();
			femaleNamesBlockParser.RegisterRegex(CommonRegexes.Integer, (weightedBlockReader, _) => {
				femaleNames.UnionWith(weightedBlockReader.GetStrings());
			});
			femaleNamesBlockParser.RegisterRegex(CommonRegexes.String, (_, nameStr) => {
				femaleNames.Add(nameStr);
			});
			femaleNamesBlockParser.IgnoreAndLogUnregisteredItems();
			femaleNamesBlockParser.ParseStream(reader);
		});
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(nameListReader);
	}
}