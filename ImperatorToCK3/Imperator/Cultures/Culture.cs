using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Cultures;

public sealed class Culture : IIdentifiable<string> {
	public string Id { get; }
	private readonly Dictionary<string, string> familyNamesDict = new(); // <key, male form>

	public Culture(string id, BufferedReader reader) {
		Id = id;

		var parser = new Parser();
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
		parser.ParseStream(reader);
	}

	public string? GetMaleFamilyNameForm(string familyKey) {
		return familyNamesDict.TryGetValue(familyKey, out var maleForm) ? maleForm : null;
	}
}