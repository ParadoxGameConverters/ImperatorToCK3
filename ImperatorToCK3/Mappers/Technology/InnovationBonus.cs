using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Technology;

public sealed class InnovationBonus { // TODO: add tests
	private readonly HashSet<string> imperatorInventions = [];
	public string? CK3InnovationId { get; private set; }
	
	public InnovationBonus(BufferedReader bonusReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => imperatorInventions.Add(reader.GetString()));
		parser.RegisterKeyword("ck3", reader => CK3InnovationId = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(bonusReader);
		
		if (CK3InnovationId is null) {
			Logger.Warn($"Innovation bonus from {string.Join(", ", imperatorInventions)} has no CK3 innovation.");
		}
	}
	
	public KeyValuePair<string, ushort>? GetProgress(IEnumerable<string> activeInventions) {
		if (CK3InnovationId is null) {
			return null;
		}
		
		// For each matching invention, add 25 to the progress.
		int progress = activeInventions
			.Where(invention => imperatorInventions.Contains(invention))
			.Sum(invention => 25);
		if (progress == 0) {
			return null;
		}
		
		return new(CK3InnovationId, (ushort)progress);
	}
}