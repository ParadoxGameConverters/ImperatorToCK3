using commonItems;
using commonItems.Collections;
using System.ComponentModel;

namespace ImperatorToCK3.Imperator.Armies;

public class UnitCollection : IdObjectCollection<ulong, Unit> {
	private readonly IdObjectCollection<ulong, Subunit> subunits = new();

	public void LoadSubunits(BufferedReader subunitsReader) {
		Logger.Info("Loading subunits...");
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			var itemStr = reader.GetStringOfItem().ToString();
			if (itemStr == "none") {
				return;
			}

			var id = ulong.Parse(idStr);
			subunits.AddOrReplace(new Subunit(id, new BufferedReader(itemStr)));

			Logger.Notice(id.ToString()); // TODO: REMOVE DEBUG
		});
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(subunitsReader);
	}
	public void LoadUnits(BufferedReader unitsReader) {
		Logger.Info("Loading units...");
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			var itemStr = reader.GetStringOfItem().ToString();
			if (itemStr == "none") {
				return;
			}

			var id = ulong.Parse(idStr);
			AddOrReplace(new Unit(id, new BufferedReader(itemStr)));

			Logger.Notice(id.ToString()); // TODO: REMOVE DEBUG
		});
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(unitsReader);
	}
}