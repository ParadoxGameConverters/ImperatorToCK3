using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.CommonUtils.Genes; 

public sealed class MorphGeneTemplate : IIdentifiable<string> {
	public string Id { get; }
	public uint Index { get; private set; } = 0;
	public bool Visible { get; private set; } = true;

	public MorphGeneTemplate(string id, BufferedReader templateReader) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.RegisterKeyword("visible", reader => Visible = reader.GetBool());
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(templateReader);
	}
}