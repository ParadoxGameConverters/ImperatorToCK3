using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using System.Linq;

namespace ImperatorToCK3.Imperator.Armies;

public sealed class UnitCollection : IdObjectCollection<ulong, Unit> {
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
		if (Subunit.IgnoredTokens.Any()) {
			Logger.Debug($"Ignored subunit tokens: {Subunit.IgnoredTokens}");
		}
		Logger.IncrementProgress();
	}
	public void LoadUnits(BufferedReader unitsReader, LocDB irLocDB, Defines defines) {
		Logger.Info("Loading units...");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, idStr) => {
			var itemStr = reader.GetStringOfItem().ToString();
			if (itemStr == "none") {
				return;
			}

			var id = ulong.Parse(idStr);
			AddOrReplace(new Unit(id, new BufferedReader(itemStr), this, irLocDB, defines));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(unitsReader);

		if (Unit.IgnoredTokens.Any()) {
			Logger.Debug($"Ignored unit tokens: {Unit.IgnoredTokens}");
		}
		Logger.IncrementProgress();
	}
}