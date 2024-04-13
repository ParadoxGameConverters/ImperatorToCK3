using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Technology;

public class InnovationBonus {
	// TODO: use this class
	
	private HashSet<string> imperatorInventions = new();
	private string? ck3Innovation;
	
	public InnovationBonus(BufferedReader bonusReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => imperatorInventions.Add(reader.GetString()));
		parser.RegisterKeyword("ck3", reader => ck3Innovation = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(bonusReader);
		
		// A bonus should have at most 3 inventions.
		if (imperatorInventions.Count > 3) {
			Logger.Warn($"Innovation bonus for {ck3Innovation} has more than 3 inventions: {string.Join(", ", imperatorInventions)}");
		}
	}
	
	public double GetProgress(IEnumerable<string> activeInventions) {
		// For each matching invention, add 0.25 to the progress.
		return activeInventions
			.Where(invention => imperatorInventions.Contains(invention))
			.Sum(invention => 0.25);
	}
}