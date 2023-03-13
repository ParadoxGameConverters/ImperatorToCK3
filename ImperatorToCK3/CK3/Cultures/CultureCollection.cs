using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;

namespace ImperatorToCK3.CK3.Cultures; 

public class CultureCollection : IdObjectCollection<string, Culture> {
	public CultureCollection(PillarCollection pillarCollection) {
		this.pillarCollection = pillarCollection;
	}
	
	public void LoadCultures(ModFilesystem ck3ModFS, ColorFactory colorFactory) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, cultureId) => {
			AddOrReplace(new Culture(cultureId, reader, pillarCollection, nameListCollection, colorFactory));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/cultures", ck3ModFS, "txt", true);
	}

	public void LoadNameLists(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, nameListId) => {
			nameListCollection.AddOrReplace(new NameList(nameListId, reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/name_lists", ck3ModFS, "txt", true);
	}
	
	private readonly PillarCollection pillarCollection;
	private readonly IdObjectCollection<string, NameList> nameListCollection = new();
}