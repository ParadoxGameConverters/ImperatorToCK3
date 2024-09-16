using commonItems;
using commonItems.Collections;
using commonItems.Mods;

namespace ImperatorToCK3.Imperator.Cultures;

public sealed class CulturesDB : IdObjectCollection<string, CultureGroup> {
	public void Load(ModFilesystem irModFS) {
		Logger.Info("Loading cultures database...");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (groupReader, groupId) => {
			AddOrReplace(new CultureGroup(groupId, groupReader));
		});
		parser.ParseGameFolder("common/cultures", irModFS, "txt", recursive: true);

		Logger.IncrementProgress();
	}

	public string? GetMaleFamilyNameForm(string familyKey) {
		foreach (var cultureGroup in this) {
			var maleForm = cultureGroup.GetMaleFamilyNameForm(familyKey);
			if (maleForm is not null) {
				return maleForm;
			}
		}

		return null;
	}
}