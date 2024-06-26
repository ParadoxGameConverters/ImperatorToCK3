using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.CK3.Provinces;

public sealed partial class Province {
	public string? GetFaithId(Date date) {
		var historyValue = History.GetFieldValue("faith", date);
		return historyValue switch {
			StringOfItem stringOfItem => stringOfItem.ToString(),
			string cultureStr => cultureStr,
			_ => null
		};
	}

	public void SetFaithIdAndOverrideExistingEntries(string faithId) {
		var faithHistoryField = History.Fields["faith"];
		faithHistoryField.RemoveAllEntries();
		faithHistoryField.AddEntryToHistory(null, "faith", faithId);
	}
	
	public void SetFaithId(string faithId, Date? date) {
		History.AddFieldValue(date, "faith", "faith", faithId);
	}

	public string? GetCultureId(Date date) {
		var historyValue = History.GetFieldValue("culture", date);
		return historyValue switch {
			StringOfItem stringOfItem => stringOfItem.ToString().RemQuotes(),
			string cultureStr => cultureStr.RemQuotes(),
			_ => null
		};
	}
	public void SetCultureId(string cultureId, Date? date) {
		History.AddFieldValue(date, "culture", "culture", cultureId);
	}
	
	public Culture? GetCulture(Date date, CultureCollection cultures) {
		var cultureId = GetCultureId(date);
		if (cultureId is null) {
			return null;
		}
		if (cultures.TryGetValue(cultureId, out var culture)) {
			return culture;
		}
		Logger.Warn($"Culture with ID {cultureId} not found!");
		return null;
	}

	public string? GetHoldingType(Date date) {
		var historyValue = History.GetFieldValue("holding", date);
		return historyValue switch {
			StringOfItem stringOfItem => stringOfItem.ToString(),
			string cultureStr => cultureStr,
			_ => null
		};
	}
	public void SetHoldingType(string holdingType, Date? date) {
		History.AddFieldValue(date, "holding", "holding", holdingType);
	}

	public IReadOnlyCollection<string> GetBuildings(Date date) {
		var buildingsValue = History.GetFieldValue("buildings", date);
		switch (buildingsValue) {
			case IList<object> buildingsList:
				return buildingsList.Select(b => b.ToString()!).ToImmutableList();
			case IList<string> buildingsList:
				return buildingsList.ToImmutableList();
			case IList<StringOfItem> buildingsList:
				return buildingsList.Select(b=>b.ToString()).ToImmutableList();
			default:
				Logger.Warn($"Wrong province buildings value: {buildingsValue}");
				return ImmutableList<string>.Empty;
		}
	}
	public void SetBuildings(IEnumerable<string> buildings, Date? date) {
		History.AddFieldValue(date, "buildings", "buildings", buildings.ToImmutableList());
	}

	public History History { get; }

	private static readonly HistoryFactory historyFactory = new HistoryFactory.HistoryFactoryBuilder()
		.WithSimpleField("culture", "culture", null)
		.WithSimpleField("faith", new OrderedSet<string> {"faith", "religion"}, null)
		.WithSimpleField("holding", "holding", "none")
		.WithSimpleField("buildings", "buildings", new List<string>())
		.WithSimpleField("special_building_slot", "special_building_slot", null)
		.WithSimpleField("special_building", "special_building", null)
		.WithSimpleField("duchy_capital_building", "duchy_capital_building", null)
		.WithSimpleField("terrain", "terrain", null)
		.Build();
}