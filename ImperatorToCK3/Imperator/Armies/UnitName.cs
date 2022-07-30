using commonItems;
using commonItems.Localization;

namespace ImperatorToCK3.Imperator.Armies; 

public class UnitName {
	private string? name;
	private int ordinal = 1;
	private UnitName? baseName = null;
	
	public UnitName(BufferedReader unitNameReader, LocDB locDB) {
		var parser = new Parser();
		parser.RegisterKeyword("name", reader => name = reader.GetString());
		parser.RegisterKeyword("ordinal", reader => ordinal = reader.GetInt());
		parser.RegisterKeyword("base", reader => baseName = new UnitName(reader, locDB));
		parser.IgnoreAndLogUnregisteredItems();
		
		parser.ParseStream(unitNameReader);
		
		
		// GET LOCALIZED NAME
		if (name is null) {
			return;
		}
		var nameLocBlock = locDB.GetLocBlockForKey(name);
		if (nameLocBlock is null) {
			return;
		}
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$NUM$", ordinal.ToString()));
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$ORDER$", ordinal.ToOrdinalSuffix(language)));
	}
}