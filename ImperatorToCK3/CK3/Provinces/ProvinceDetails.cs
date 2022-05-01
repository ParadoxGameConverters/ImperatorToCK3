using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Provinces;

public class ProvinceDetails {
	// These values are open to ease management.
	// This is a storage container for CK3::Province.
	public string Culture { get; set; } = string.Empty;
	public string Religion { get; set; } = string.Empty;
	public string Holding { get; set; } = "none";
	public List<string> Buildings { get; } = new();

	public ProvinceDetails() { }
	public ProvinceDetails(ProvinceDetails otherDetails) {
		Culture = otherDetails.Culture;
		Religion = otherDetails.Religion;
		Holding = otherDetails.Holding;
		Buildings = new(otherDetails.Buildings);
	}
	public ProvinceDetails(BufferedReader reader, Date ck3BookmarkDate) {
		var history = historyFactory.GetHistory(reader);

		var cultureOpt = history.GetFieldValue("culture", ck3BookmarkDate);
		if (cultureOpt is string cultureStr) {
			Culture = cultureStr;
		}
		var religionOpt = history.GetFieldValue("religion", ck3BookmarkDate);
		if (religionOpt is string religionStr) {
			Religion = religionStr;
		}
		switch (history.GetFieldValue("holding", ck3BookmarkDate)) {
			case null:
				Logger.Warn("Province's holding can't be null!");
				break;
			case string holdingStr:
				Holding = holdingStr;
				break;
			default:
				Logger.Warn("Wrong province holding value!");
				break;
		}

		var buildingsValue = history.GetFieldValue("buildings", ck3BookmarkDate);
		switch (buildingsValue) {
			case null:
				Logger.Warn("Province's buildings list can't be null!");
				break;
			case IList<object> buildingsList:
				Buildings = new List<string>(buildingsList.Select(b => b.ToString()!));
				break;
			case IList<string> buildingsList:
				Buildings = (List<string>)buildingsList;
				break;
			default:
				Logger.Warn($"Wrong province buildings value: {buildingsValue}");
				break;
		}
	}

	private static readonly HistoryFactory historyFactory = new HistoryFactory.HistoryFactoryBuilder()
		.WithSimpleField("culture", "culture", null)
		.WithSimpleField("religion", "religion", null)
		.WithSimpleField("holding", "holding", "none")
		.WithSimpleField("buildings", "buildings", new List<string>())
		.Build();
}