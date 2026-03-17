using commonItems;
using commonItems.Collections;
using DotLiquid;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ZLinq;

namespace ImperatorToCK3.CK3;

public static class ParserExtensions {
	private static bool GetConditionValue(BufferedReader reader, OrderedDictionary<string, bool> ck3ModFlags) {
		var conditionLexeme = Parser.GetNextLexeme(reader);
		if (CommonRegexes.Variable.IsMatch(conditionLexeme)) {
			var value = reader.ResolveVariable(conditionLexeme);
			if (value is null) {
				return false;
			}
			if (value is bool boolValue) {
				return boolValue;
			}
			return Convert.ToBoolean(value);
		} else if (CommonRegexes.InterpolatedExpression.IsMatch(conditionLexeme)) {
			// Interpolated expression.
			var value = reader.EvaluateExpression(conditionLexeme);
			if (value is bool boolValue) {
				return boolValue;
			}
			return Convert.ToBoolean(value);
		} else {
			// Otherwise the token is expected to be a mod flag name.
			return ck3ModFlags[conditionLexeme];
		}
	}
	public static void RegisterModDependentBloc(this Parser parser, OrderedDictionary<string, bool> ck3ModFlags) {
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
	
	public static void ParseLiquidFile(this Parser parser, string filePath, Hash liquidVariables) {
		// The file used the Liquid templating language, so convert it to text before parsing.
		var liquidText = File.ReadAllText(filePath);

		var template = Template.Parse(liquidText);
		var result = template.Render(liquidVariables, CultureInfo.InvariantCulture);

		parser.ParseStream(new BufferedReader(result));
	}

	public static void ParseFolderWithLiquidSupport(this Parser parser, string path, string extensions, bool recursive, Hash liquidVariables, bool logFilePaths = false) {
		var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
		var validExtensions = new HashSet<string>(
			extensions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
			StringComparer.OrdinalIgnoreCase
		);

		foreach (var file in Directory.EnumerateFiles(path, "*", searchOption)) {
			var extension = CommonFunctions.GetExtension(file);
			if (!validExtensions.Contains(extension)) {
				continue;
			}

			if (logFilePaths) {
				Logger.Debug($"Parsing file: {file}");
			}

			if (string.Equals(extension, "liquid", StringComparison.OrdinalIgnoreCase)) {
				parser.ParseLiquidFile(file, liquidVariables);
			} else {
				parser.ParseFile(file);
			}
		}
	}
}