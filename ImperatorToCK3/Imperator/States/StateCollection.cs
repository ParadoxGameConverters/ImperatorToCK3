using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;

namespace ImperatorToCK3.Imperator.States; 

public class StateCollection : IdObjectCollection<ulong, State> {
	public void LoadStates(BufferedReader statesDbReader, IdObjectCollection<string, Area> areas, CountryCollection countries) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, stateIdStr) => {
			var strOfItem = reader.GetStringOfItem();
			if (!strOfItem.IsArrayOrObject()) {
				return;
			}

			var stateReader = new BufferedReader(strOfItem.ToString());
			AddOrReplace(new State(ulong.Parse(stateIdStr), stateReader, areas, countries));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(statesDbReader);
	}
}