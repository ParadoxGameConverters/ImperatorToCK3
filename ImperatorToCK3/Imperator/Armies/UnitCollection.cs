using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace ImperatorToCK3.Imperator.Armies;

public class UnitCollection : IdObjectCollection<ulong, Unit> {
	private readonly IdObjectCollection<ulong, Subunit> subunits = new();
	private readonly HashSet<string> ignoredSubunitTokens = new();
	private readonly HashSet<string> ignoredUnitTokens = new();

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
		parser.IgnoreAndStoreUnregisteredItems(ignoredSubunitTokens);

		parser.ParseStream(subunitsReader);
		Logger.Debug($"Ignored subunit tokens: {string.Join(',', ignoredSubunitTokens)}");
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
		parser.IgnoreAndStoreUnregisteredItems(ignoredUnitTokens);

		parser.ParseStream(unitsReader);
		Logger.Debug($"Ignored unit tokens: {string.Join(',', ignoredUnitTokens)}");
	}
}