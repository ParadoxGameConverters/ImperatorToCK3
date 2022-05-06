using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Armies;

public class UnitCollection : IdObjectCollection<ulong, Unit> {
	public void LoadUnits(BufferedReader unitsReader) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			var itemStr = reader.GetStringOfItem().ToString();
			if (itemStr == "none") {
				return;
			}

			var id = ulong.Parse(idStr);
			dict[id] = new Unit(id, new BufferedReader(itemStr));

			Logger.Notice(id.ToString());
		});

		parser.ParseStream(unitsReader);
	}
}