using commonItems;
using commonItems.Collections;
using commonItems.Localization;

namespace ImperatorToCK3.Imperator.Armies;

public class UnitCollection : IdObjectCollection<ulong, Unit> {
	public IdObjectCollection<ulong, Subunit> Subunits { get; } = new();

	public void LoadSubunits(BufferedReader subunitsReader) {
		Logger.Info("Loading subunits...");
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			var itemStr = reader.GetStringOfItem().ToString();
			if (itemStr == "none") {
				return;
			}

			var id = ulong.Parse(idStr);
			Subunits.AddOrReplace(new Subunit(id, new BufferedReader(itemStr)));
		});
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(subunitsReader);
		Logger.Debug($"Ignored subunit tokens: {string.Join(',', Subunit.IgnoredTokens)}");
	}
	public void LoadUnits(BufferedReader unitsReader, LocDB locDB, Defines defines) {
		Logger.Info("Loading units...");
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			var itemStr = reader.GetStringOfItem().ToString();
			if (itemStr == "none") {
				return;
			}

			var id = ulong.Parse(idStr);
			AddOrReplace(new Unit(id, new BufferedReader(itemStr), this, locDB, defines));
		});
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(unitsReader);
		Logger.Debug($"Ignored unit tokens: {string.Join(',', Unit.IgnoredTokens)}");
	}
}