using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3;

public static class ParserExtensions {
	public static void RegisterModDependentBloc(this Parser parser, IEnumerable<string> ck3ModFlags) {
		parser.RegisterKeyword("MOD_DEPENDENT", reader => { // TODO: test this
			var modDependentParser = new Parser();
			foreach (var activeModFlag in ck3ModFlags) {
				modDependentParser.RegisterKeyword(activeModFlag, parser.ParseStream);
			}
			modDependentParser.IgnoreAndLogUnregisteredItems(); // MAYBE DON'T LOG THE IGNORED MOD FLAGS
			modDependentParser.ParseStream(reader);
		});
	}
}