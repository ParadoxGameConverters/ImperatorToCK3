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
		
		if (ck3Innovation is null) {
			Logger.Warn($"Innovation link from {imperatorInvention} has no CK3 innovation.");
		}
		
		if (imperatorInvention is null) {
			Logger.Warn($"Innovation link to {ck3Innovation} has no Imperator invention.");
		}
	}
	
	public string? Match(string irInvention) {
		if (imperatorInvention is null) {
			return null;
		}
		return imperatorInvention == irInvention ? ck3Innovation : null;
	}
}