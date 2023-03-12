using commonItems;
using commonItems.Mods;

namespace ImperatorToCK3.CK3.Characters;

public partial class CharacterCollection {
	public void LoadCK3Characters(ModFilesystem ck3ModFS) {
		Logger.Info("Loading characters from CK3...");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, characterId) => {
			var character = new Character(characterId, reader, this);
			if (character.Id == "145666") {
				Logger.Error($"LOL FOUND HIM, {character.GetName("2000.1.1")}"); // TODO: remove this
			}
			AddOrReplace(character);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("history/characters", ck3ModFS, "txt", recursive: true);
	}
}