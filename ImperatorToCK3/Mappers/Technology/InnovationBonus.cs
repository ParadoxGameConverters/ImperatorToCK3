using commonItems;
using System;
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
		
		// A bonus should have at most 3 inventions.
		if (imperatorInventions.Count > 3) {
			Logger.Warn($"Innovation bonus for {ck3Innovation} has more than 3 inventions: {string.Join(", ", imperatorInventions)}");
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
		return new(ck3Innovation, (ushort)progress);
	}
}