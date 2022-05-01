using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Region;

public class ImperatorArea : IIdentifiable<string> {
	public SortedSet<ulong> Provinces { get; } = new();
	public string Id { get; }

	public ImperatorArea(string id, BufferedReader reader) {
		Id = id;
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("provinces", reader => Provinces.UnionWith(reader.GetULongs()));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public bool ContainsProvince(ulong province) {
		return Provinces.Contains(province);
	}
}