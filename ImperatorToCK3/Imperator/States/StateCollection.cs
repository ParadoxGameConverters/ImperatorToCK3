using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.States; 

public class StateCollection : IdObjectCollection<ulong, State> {
	public void LoadStates(BufferedReader statesDbReader) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, stateIdStr) => {
			AddOrReplace(new State(ulong.Parse(stateIdStr), reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(statesDbReader);
	}
}