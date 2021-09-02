using System.Collections.Generic;
using System.IO;
using ImperatorToCK3.CK3.Characters;
using commonItems;

namespace ImperatorToCK3.Outputter {
	public static class CharactersOutputter {
		public static void OutputCharacters(string outputModName, Dictionary<string, Character> characters) {
			// dumping all into one file
			var path = "output/" + outputModName + "/history/characters/fromImperator.txt";
			using var output = new StreamWriter(path);
			output.Write(CommonFunctions.UTF8BOM);
			foreach (var character in characters.Values) {
				CharacterOutputter.OutputCharacter(output, character);
			}
		}
	}
}
