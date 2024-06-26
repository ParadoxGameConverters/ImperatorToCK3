using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Cultures;

public sealed class CultureGroup : IdObjectCollection<string, Culture>, IIdentifiable<string> {
	public string Id { get; }
	private readonly Dictionary<string, string> familyNamesDict = new(); // <key, male form>

	public CultureGroup(string id, BufferedReader groupReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("culture", culturesReader => {
			var culturesParser = new Parser();
			culturesParser.RegisterRegex(CommonRegexes.String, (cultureReader, cultureId) => {
				AddOrReplace(new Culture(cultureId, cultureReader));
			});
			culturesParser.IgnoreAndLogUnregisteredItems();
			culturesParser.ParseStream(culturesReader);
		});
		parser.RegisterKeyword("family", familyNamesReader => {
			var names = familyNamesReader.GetStrings();
			foreach (var nameEntry in names) {
				var parts = nameEntry.Split('.');
				switch(parts.Length) {
					case 1:
						var key = parts[0];
						familyNamesDict[key] = key;
						break;
					case 4:
						var maleFormPart = parts[0];
						var keyPart = parts[2];
						familyNamesDict[keyPart] = maleFormPart;
						break;
					default:
						Logger.Warn($"Unknown family name format: {nameEntry}");
						break;
				}
			}
		});
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(groupReader);
	}

	public string? GetMaleFamilyNameForm(string familyKey) {
		if (familyNamesDict.TryGetValue(familyKey, out var maleForm)) {
			return maleForm;
		}
		foreach (var culture in this) {
			maleForm = culture.GetMaleFamilyNameForm(familyKey);
			if (maleForm is not null) {
				return maleForm;
			}
		}

		return null;
	}
}