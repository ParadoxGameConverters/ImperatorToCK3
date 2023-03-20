using commonItems;

namespace ImperatorToCK3.CommonUtils.Genes; 

public class MorphGeneTemplate {
	public uint Index { get; private set; } = 0;
	public bool Visible { get; private set; } = true;

	public MorphGeneTemplate(BufferedReader templateReader) {
		var parser = new Parser();
		parser.RegisterKeyword("index", reader => Index = (uint)reader.GetInt());
		parser.RegisterKeyword("visible", reader => Visible = reader.GetBool());
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(templateReader);
	}
}