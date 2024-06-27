using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;

namespace ImperatorToCK3.Imperator.States;

public sealed class StateCollection : IdObjectCollection<ulong, State> {
	public void LoadStates(BufferedReader statesDbReader, IdObjectCollection<string, Area> areas, CountryCollection countries) {
		stateDataParser.RegisterKeyword("capital", reader => stateData.CapitalProvinceId = reader.GetULong());
		stateDataParser.RegisterKeyword("area", reader => {
			var areaId = reader.GetString();
			if (!areas.TryGetValue(areaId, out var area)) {
				Logger.Warn($"Unrecognized area found when loading states: {areaId}");
				return;
			}
			stateData.Area = area;
		});
		stateDataParser.RegisterKeyword("country", reader => {
			var countryId = reader.GetULong();
			if (!countries.TryGetValue(countryId, out var country)) {
				Logger.Warn($"Unrecognized country found when loading states: {countryId}");
				return;
			}
			stateData.Country = country;
		});
		stateDataParser.IgnoreAndStoreUnregisteredItems(IgnoredStateKeywords);
		
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, stateIdStr) => {
			var strOfItem = reader.GetStringOfItem();
			if (!strOfItem.IsArrayOrObject()) {
				return;
			}
			
			var stateId = ulong.Parse(stateIdStr);
			stateData = new StateData();
			stateDataParser.ParseStream(new BufferedReader(strOfItem.ToString()));
			if (stateData.Area is null) {
				Logger.Warn($"State {stateId} has no area defined!");
				return;
			}
			if (stateData.Country is null) {
				Logger.Warn($"State {stateId} has no country defined!");
				return;
			}
			AddOrReplace(new State(stateId, stateData));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(statesDbReader);
	}

	private StateData stateData = new();
	private readonly Parser stateDataParser = new();

	public static IgnoredKeywordsSet IgnoredStateKeywords { get; } = new();
}