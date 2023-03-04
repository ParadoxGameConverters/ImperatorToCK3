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
			AddOrReplace(new Culture(cultureId, reader, pillarCollection, colorFactory));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/cultures", ck3ModFS, "txt", true);
	}
	
	private readonly PillarCollection pillarCollection;
}