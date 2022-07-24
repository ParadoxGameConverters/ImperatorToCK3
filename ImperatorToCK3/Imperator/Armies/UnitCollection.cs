using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
		});
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(subunitsReader);
		Logger.Debug($"Ignored subunit tokens: {string.Join(',', Subunit.IgnoredTokens)}");
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
		});
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(unitsReader);
		Logger.Debug($"Ignored unit tokens: {string.Join(',', Unit.IgnoredTokens)}");
	}

	public IDictionary<string, int> GetMenPerUnitType(Unit unit) {
		return subunits.Where(s => unit.CohortIds.Contains(s.Id))
			.GroupBy(s=>s.Type)
			.ToDictionary(g => g.Key, g => (int)g.Sum(s => 500 * s.Strength)); // TODO: instead of assuming 500, read COHORT_SIZE from Imperator defines
	}
}