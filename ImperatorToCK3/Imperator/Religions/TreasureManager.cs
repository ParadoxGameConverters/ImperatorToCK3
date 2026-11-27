using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Religions; 

public sealed class TreasureManager : IdObjectCollection<ulong, Treasure> {
	public void LoadTreasures(BufferedReader treasureManagerReader) {
		var parser = new Parser();
		parser.RegisterKeyword("database", LoadTreasuresFromDatabase);
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(treasureManagerReader);
	}
	
	private void LoadTreasuresFromDatabase(BufferedReader treasureDatabaseReader) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			AddOrReplace(new Treasure(ulong.Parse(idStr), reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(treasureDatabaseReader);
	}
}