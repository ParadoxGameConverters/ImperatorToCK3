using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Armies;

public class LegionCollection : IdObjectCollection<ulong, Legion> {
	public void LoadUnits(BufferedReader reader) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			var id = ulong.Parse(idStr);
			dict[id] = new Legion(id, reader);

			Logger.Notice(id.ToString());
		});

		parser.ParseStream(reader);
	}
}