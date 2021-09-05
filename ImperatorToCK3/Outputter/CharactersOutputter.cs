using System.Collections.Generic;
using System.IO;
using ImperatorToCK3.CK3.Characters;

namespace ImperatorToCK3.Outputter {
	public static class CharactersOutputter {
		public static void OutputCharacters(string outputModName, Dictionary<string, Character> characters) {
			// dumping all into one file
			var path = "output/" + outputModName + "/history/characters/fromImperator.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, System.Text.Encoding.UTF8);
			foreach (var character in characters.Values) {
				CharacterOutputter.OutputCharacter(output, character);
			}
		}
	}
}
