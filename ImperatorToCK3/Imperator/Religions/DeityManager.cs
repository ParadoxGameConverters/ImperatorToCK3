using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Religions; 

public class DeityManager {
	private readonly Dictionary<ulong, string> holySiteIdToDeityIdDictionary = new();

	public void LoadHolySiteDatabase(BufferedReader deityManagerReader) {
		Logger.Info("Loading Imperator holy site database...");
		
		var parser = new Parser();
		parser.RegisterKeyword("deities_database", databaseReader => {
			var databaseParser = new Parser();
			databaseParser.RegisterRegex(CommonRegexes.Integer, (reader, holySiteIdStr) => {
				var deityId = reader.GetAssignments()["deity"];
				holySiteIdToDeityIdDictionary[ulong.Parse(holySiteIdStr)] = deityId;
			});
			databaseParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			databaseParser.ParseStream(databaseReader);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		
		parser.ParseStream(deityManagerReader);
	}

	public string GetDeityIdForHolySiteId(ulong holySiteId) {
		return holySiteIdToDeityIdDictionary[holySiteId];
	}
}