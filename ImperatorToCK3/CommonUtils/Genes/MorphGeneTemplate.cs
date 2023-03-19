using commonItems;

namespace ImperatorToCK3.CommonUtils.Genes; 

public class MorphGeneTemplate {
	public uint Index { get; private set; } = 0;

	public MorphGeneTemplate(BufferedReader templateReader) {
		var parser = new Parser();
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(templateReader);
	}
}