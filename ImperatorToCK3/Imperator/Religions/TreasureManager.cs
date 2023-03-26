using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Religions; 

public class TreasureManager : IdObjectCollection<ulong, Treasure> {
	public TreasureManager(BufferedReader treasureManagerReader) {
		var parser = new Parser();
		parser.RegisterKeyword("database", LoadTreasures);
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(treasureManagerReader);
	}
	
	private void LoadTreasures(BufferedReader treasureDatabaseReader) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			AddOrReplace(new Treasure(ulong.Parse(idStr), reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(treasureDatabaseReader);
	}
}