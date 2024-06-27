using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImperatorToCK3.CK3.Religions; 

public sealed class DoctrineCategory : IIdentifiable<string> {
	public string Id { get; }
	public string? GroupId { get; private set; }
	private readonly OrderedSet<string> doctrineIds = new();
	public IReadOnlyCollection<string> DoctrineIds => doctrineIds.ToImmutableArray();

	public DoctrineCategory(string id, BufferedReader categoryReader) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("group", reader => GroupId = reader.GetString());
		parser.RegisterRegex(CommonRegexes.String, (reader, doctrineId) => {
			doctrineIds.Add(doctrineId);
			ParserHelpers.IgnoreItem(reader);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(categoryReader);
	}
}