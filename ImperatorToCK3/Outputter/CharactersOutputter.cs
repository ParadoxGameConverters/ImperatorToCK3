using commonItems;
using ImperatorToCK3.CK3.Characters;
using System.IO;

namespace ImperatorToCK3.Outputter;
public static class CharactersOutputter {
	public static void OutputCharacters(string outputModName, CharacterCollection characters, Date conversionDate) {
		// dumping all into one file
		var path = $"output/{outputModName}/history/characters/fromImperator.txt";
		using var stream = File.OpenWrite(path);
		using var output = new StreamWriter(stream, System.Text.Encoding.UTF8);
		foreach (var character in characters) {
			CharacterOutputter.OutputCharacter(output, character, conversionDate);
		}
	}
}
