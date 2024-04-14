using commonItems;

namespace ImperatorToCK3.Mappers.Technology;

public sealed class InnovationLink { // TODO: ADD TESTS
	private string? imperatorInvention;
	private string? ck3Innovation;
	
	public InnovationLink(BufferedReader linkReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => imperatorInvention = reader.GetString());
		parser.RegisterKeyword("ck3", reader => ck3Innovation = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(linkReader);
	}
	
	public string? Match(string irInvention) {
		if (imperatorInvention is null) {
			return null;
		}
		return imperatorInvention == irInvention ? ck3Innovation : null;
	}
}