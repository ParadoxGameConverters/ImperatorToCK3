using commonItems;
using commonItems.Collections;
using commonItems.Mods;

namespace ImperatorToCK3.CK3.Cultures; 

public class CultureCollection : IdObjectCollection<string, Culture> {
	public void LoadCultures(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, cultureId) => {
			var newCulture = new Culture(cultureId, reader);
			AddOrReplace(newCulture);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/cultures", ck3ModFS, "txt", true);
	}
}