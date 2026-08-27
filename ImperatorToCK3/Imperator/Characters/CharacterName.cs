using System;
using commonItems;

namespace ImperatorToCK3.Imperator.Characters;

internal sealed class CharacterName {
	public string Name { get; private set; } = string.Empty; // key for localization or literal name
	public string? CustomName { get; private set; } // localized

	public CharacterName(BufferedReader reader) {
		var parser = new Parser(implicitVariableHandling: false);
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("name", reader => {
			var nameStr = reader.GetString();
			// Mods such as Reanimāta use "<name>_TEXT" keys as placeholders for characters whose names are assigned dynamically
			// (e.g., female Romans named after their birth order among their sisters). Strip the suffix so they get actual names.
			if (nameStr.EndsWith("_TEXT", StringComparison.Ordinal)) {
				nameStr = nameStr[..^"_TEXT".Length];
			}
			Name = nameStr;
		});
		parser.RegisterKeyword("custom_name", reader => CustomName = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}