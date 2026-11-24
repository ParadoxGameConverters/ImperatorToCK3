using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class DoctrineType : IIdentifiable<string> {
	public string Id { get; }
	public string? GroupId { get; private set; }

	public DoctrineType(string id, BufferedReader doctrineReader) {
		Id = id;

		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterKeyword("doctrine_group_type", reader => GroupId = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(doctrineReader);
	}
}
