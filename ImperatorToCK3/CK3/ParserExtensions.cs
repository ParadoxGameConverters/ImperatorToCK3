using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3;

public static class ParserExtensions {
	public static void RegisterModDependentBloc(this Parser parser, IDictionary<string, bool> ck3ModFlags) {
		parser.RegisterKeyword("MOD_DEPENDENT", blocReader => { // TODO: test this
			// elseMode changes to true when IF condition is false.
			// Changes back to false when an ELSE_IF or ELSE block is entered.
			// Also changes to false when an IF block is encountered.
			bool elseMode = false;
			
			foreach (var (modFlagName, value) in ck3ModFlags) {
				blocReader.Variables[modFlagName] = value;
			}
			
			var modDependentParser = new Parser();
			modDependentParser.RegisterKeyword("IF", reader => {
				elseMode = false;
				
				var conditionToken = Parser.GetNextTokenWithoutMatching(reader) ?? string.Empty;
				bool conditionValue;
				if (CommonRegexes.InterpolatedExpression.IsMatch(conditionToken)) {
					conditionValue = (bool)reader.EvaluateExpression(conditionToken);
				} else {
					conditionValue = ck3ModFlags[conditionToken];
				}
				if (conditionValue == false) {
					elseMode = true;
					ParserHelpers.IgnoreItem(reader);
				} else {
					parser.ParseStream(reader);
				}

			});
			
			modDependentParser.RegisterKeyword("ELSE_IF", reader => {
				var interpolatedExpressionToken = Parser.GetNextTokenWithoutMatching(reader) ?? string.Empty;
				
				// If not in elseMode, skip the block.
				if (!elseMode) {
					ParserHelpers.IgnoreItem(reader);
				} else {
					var conditionValue = reader.EvaluateExpression(interpolatedExpressionToken);
					if ((bool)conditionValue == false) {
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
			modDependentParser.IgnoreAndLogUnregisteredItems(); // TODO: MAYBE DON'T LOG THE IGNORED MOD FLAGS
			modDependentParser.ParseStream(blocReader);
		});
	}
}