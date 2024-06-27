using commonItems;

namespace ImperatorToCK3.Imperator.Characters;

public sealed class CharacterAttributes {
	public int Martial { get; set; } = 0;
	public int Finesse { get; set; } = 0;
	public int Charisma { get; set; } = 0;
	public int Zeal { get; set; } = 0;

	public static CharacterAttributes Parse(BufferedReader reader) {
		var attributes = new CharacterAttributes();
		
		var parser = new Parser();
		parser.RegisterKeyword("martial", r => attributes.Martial = r.GetInt());
		parser.RegisterKeyword("finesse", r => attributes.Finesse = r.GetInt());
		parser.RegisterKeyword("charisma", r => attributes.Charisma = r.GetInt());
		parser.RegisterKeyword("zeal", r => attributes.Zeal = r.GetInt());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(reader);
		
		return attributes;
	}
}