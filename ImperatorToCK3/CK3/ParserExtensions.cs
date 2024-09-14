using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3;

public static class ParserExtensions {
	private static bool GetConditionValue(BufferedReader reader, IDictionary<string, bool> ck3ModFlags) {
		var conditionToken = Parser.GetNextTokenWithoutMatching(reader) ?? string.Empty;
		
		if (conditionToken == "True" || conditionToken == "False") {
			// If the conditionToken is "True" or "False" it means the parser has already evaluated the expression.
			return bool.Parse(conditionToken);
		} else {
			// Otherwise the token is expected to be a mod flag name.
			return ck3ModFlags[conditionToken];
		}
	}
	public static void RegisterModDependentBloc(this Parser parser, IDictionary<string, bool> ck3ModFlags) {
		parser.RegisterKeyword("MOD_DEPENDENT", blocReader => {
			// elseMode changes to true when IF condition is false.
			// Changes back to false when an ELSE_IF or ELSE block is entered.
			// Also changes to false when an IF block is encountered.
			bool elseMode = false;
			
			foreach (var (modFlagName, value) in ck3ModFlags) {
				blocReader.Variables[modFlagName] = value;
			}
			
			var modDependentParser = new Parser();
			modDependentParser.RegisterKeyword("IF", reader => {
				bool conditionValue = GetConditionValue(reader, ck3ModFlags);
				if (!conditionValue) {
					elseMode = true;
					ParserHelpers.IgnoreItem(reader);
				} else {
					elseMode = false;
					parser.ParseStream(reader);
				}
			});
			
			modDependentParser.RegisterKeyword("ELSE_IF", reader => {
				// If not in elseMode, skip the block.
				if (!elseMode) {
					// Skip the condition first.
					Parser.GetNextLexeme(reader);
					// Skip the block.
					ParserHelpers.IgnoreItem(reader);
				} else {
					bool conditionValue = GetConditionValue(reader, ck3ModFlags);
					if (!conditionValue) {
						// If condition is false, skip the block.
						elseMode = true;
						ParserHelpers.IgnoreItem(reader);
					} else {
						elseMode = false;
						parser.ParseStream(reader);
					}
				}
			});
			
			modDependentParser.RegisterKeyword("ELSE", reader => {
				// If not in elseMode, skip the block.
				if (!elseMode) {
					ParserHelpers.IgnoreItem(reader);
				} else {
					elseMode = false; // There should be no more ELSE_IF or ELSE blocks after ELSE.
					parser.ParseStream(reader);
				}
			});
			modDependentParser.IgnoreAndLogUnregisteredItems();
			modDependentParser.ParseStream(blocReader);
		});
	}
}