using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Exceptions;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Religions; 

public sealed class Treasure : IIdentifiable<ulong> {
	public ulong Id { get; }
	public string Key { get; private set; }
	public string IconName { get; private set; }
	
	private Dictionary<string, double> stateModifiers = new();
	private Dictionary<string, double> characterModifiers = new();

	public Treasure(ulong id, BufferedReader treasureReader) {
		Id = id;

		string? key = null;
		string? iconName = null;
		
		var parser = new Parser();
		parser.RegisterKeyword("key", reader => key = reader.GetString());
		parser.RegisterKeyword("icon", reader => iconName = reader.GetString());
		parser.RegisterKeyword("state_modifier", reader => {
			var stateModifierParser = new Parser();
			stateModifierParser.RegisterKeyword("name", ParserHelpers.IgnoreItem);
			stateModifierParser.RegisterRegex(CommonRegexes.String, (modifierReader, name) => {
				stateModifiers[name] = modifierReader.GetDouble();
			});
			stateModifierParser.IgnoreAndLogUnregisteredItems();
			stateModifierParser.ParseStream(reader);
		});
		parser.RegisterKeyword("character_modifier", reader => {
			var characterModifierParser = new Parser();
			characterModifierParser.RegisterKeyword("name", ParserHelpers.IgnoreItem);
			characterModifierParser.RegisterRegex(CommonRegexes.String, (modifierReader, name) => {
				characterModifiers[name] = modifierReader.GetDouble();
			});
			characterModifierParser.IgnoreAndLogUnregisteredItems();
			characterModifierParser.ParseStream(reader);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(treasureReader);
		
		Key = key ?? throw new ConverterException($"key was not defined for treasure {id}!");
		IconName = iconName ?? throw new ConverterException($"icon was not defined for treasure {id}!");
	}
}