using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Characters;

public partial class CharacterCollection {
	public void LoadCK3Characters(ModFilesystem ck3ModFS) {
		Logger.Info("Loading characters from CK3...");

		var ignoredKeywords = new IgnoredKeywordsSet();
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, characterId) => {
			var character = new Character(characterId, reader, this);
			AddOrReplace(character);
		});
		parser.IgnoreAndStoreUnregisteredItems(ignoredKeywords);
		parser.ParseGameFolder("history/characters", ck3ModFS, "txt", recursive: true);
		
		Logger.Debug($"Ignored CK3 character keywords: {ignoredKeywords}");
	}
}