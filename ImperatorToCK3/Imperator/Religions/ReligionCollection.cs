using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Religions; 

public class ReligionCollection : IdObjectCollection<string, Religion> {
	public IdObjectCollection<string, Deity> Deities { get; }

	public ReligionCollection(ScriptValueCollection scriptValues) {
		IDictionary<string, float> parsedReligionModifiers;
		Parser religionParser = new();
		religionParser.RegisterKeyword("modifier", reader => {
			var modifiersAssignments = reader.GetAssignments();
			parsedReligionModifiers = modifiersAssignments
				.ToDictionary(kvp => kvp.Key, kvp => scriptValues.GetModifierValue(kvp.Value));
		});
		religionParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		
		religionsParser = new Parser();
		religionsParser.RegisterRegex(CommonRegexes.String, (reader, religionId) => {
			parsedReligionModifiers = new Dictionary<string, float>();
			
			religionParser.ParseStream(reader);
			AddOrReplace(new Religion(religionId, parsedReligionModifiers));
		});
		religionsParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	

	public void LoadReligions(ModFilesystem imperatorModFS) {
		Logger.Info("Loading Imperator religions...");
		religionsParser.ParseGameFolder("common/religions", imperatorModFS, "txt", true);
	}

	public void LoadDeities(ModFilesystem imperatorModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (deityReader, deityId) => {
			
		});
	}

	private readonly Parser religionsParser;
}