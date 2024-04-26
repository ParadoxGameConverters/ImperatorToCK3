using commonItems;

namespace ImperatorToCK3.Mappers.Technology;

public sealed class InnovationLink { // TODO: ADD TESTS
	private string? imperatorInvention;
	public string? CK3InnovationId { get; private set; }
	
	public InnovationLink(BufferedReader linkReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => imperatorInvention = reader.GetString());
		parser.RegisterKeyword("ck3", reader => CK3InnovationId = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(linkReader);
		
		if (CK3InnovationId is null) {
			Logger.Warn($"Innovation link from {imperatorInvention} has no CK3 innovation.");
		}
		
		if (imperatorInvention is null) {
			Logger.Warn($"Innovation link to {CK3InnovationId} has no Imperator invention.");
		}
	}
	
	public string? Match(string irInvention) {
		if (imperatorInvention is null) {
			return null;
		}
		return imperatorInvention == irInvention ? CK3InnovationId : null;
	}
}