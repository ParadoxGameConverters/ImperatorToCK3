using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Technology;

public sealed class InnovationBonus { // TODO: add tests
	private readonly HashSet<string> imperatorInventions = [];
	private string? ck3Innovation;
	
	public InnovationBonus(BufferedReader bonusReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => imperatorInventions.Add(reader.GetString()));
		parser.RegisterKeyword("ck3", reader => ck3Innovation = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(bonusReader);
		
		if (ck3Innovation is null) {
			Logger.Warn($"Innovation bonus from {string.Join(", ", imperatorInventions)} has no CK3 innovation.");
		}
	}
	
	public KeyValuePair<string, ushort>? GetProgress(IEnumerable<string> activeInventions) {
		if (ck3Innovation is null) {
			return null;
		}
		
		// For each matching invention, add 25 to the progress.
		int progress = activeInventions
			.Where(invention => imperatorInventions.Contains(invention))
			.Sum(invention => 25);
		if (progress == 0) {
			return null;
		}
		
		return new(ck3Innovation, (ushort)progress);
	}
}