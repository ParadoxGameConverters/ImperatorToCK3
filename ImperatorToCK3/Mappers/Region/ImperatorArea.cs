using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Region;

public class ImperatorArea : IIdentifiable<string> {
	public SortedSet<ulong> ProvinceIds { get; } = new();
	public string Id { get; }

	public ImperatorArea(string id, BufferedReader reader) {
		Id = id;
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("provinces", reader => ProvinceIds.UnionWith(reader.GetULongs()));
		parser.IgnoreAndLogUnregisteredItems();
	}
	public bool ContainsProvince(ulong province) {
		return ProvinceIds.Contains(province);
	}
}