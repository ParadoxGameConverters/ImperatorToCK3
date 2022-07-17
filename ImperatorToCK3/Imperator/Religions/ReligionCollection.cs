using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Religions; 

public class ReligionCollection : IdObjectCollection<string, Religion> {
	public IdObjectCollection<string, Deity> Deities { get; } = new();

	public ReligionCollection(ScriptValueCollection scriptValues) {
		IDictionary<string, double> parsedReligionModifiers;
		var religionParser = new Parser();
		religionParser.RegisterKeyword("modifier", reader => {
			var modifiersAssignments = reader.GetAssignments();
			parsedReligionModifiers = modifiersAssignments
				.ToDictionary(kvp => kvp.Key, kvp => scriptValues.GetModifierValue(kvp.Value));
		});
		religionParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		
		religionsParser = new Parser();
		religionsParser.RegisterRegex(CommonRegexes.String, (reader, religionId) => {
			parsedReligionModifiers = new Dictionary<string, double>();
			
			religionParser.ParseStream(reader);
			AddOrReplace(new Religion(religionId, parsedReligionModifiers));
		});
		religionsParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		deitiesParser = new Parser();
		deitiesParser.RegisterRegex(CommonRegexes.String, (deityReader, deityId) => {
			var deity = new Deity(deityId, deityReader, scriptValues);
			Deities.AddOrReplace(deity);
		});
	}

	public void LoadReligions(ModFilesystem imperatorModFS) {
		Logger.Info("Loading Imperator religions...");
		religionsParser.ParseGameFolder("common/religions", imperatorModFS, "txt", true);
	}

	public void LoadDeities(ModFilesystem imperatorModFS) {
		Logger.Info("Loading Imperator deities...");
		deitiesParser.ParseGameFolder("common/deities", imperatorModFS, "txt", true);
	}

	private readonly Parser religionsParser;
	private readonly Parser deitiesParser;
}