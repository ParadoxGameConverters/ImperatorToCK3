using commonItems;
using ImperatorToCK3.CK3.Characters;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter;
public static class CharactersOutputter {
	public static void OutputCharacters(string outputModName, CharacterCollection characters, Date conversionDate) {
		var charactersFromIR = characters.Where(c => c.FromImperator).ToImmutableList();
		var charactersFromCK3 = characters.Except(charactersFromIR).ToImmutableList();
		
		var pathForCharactersFromIR = $"output/{outputModName}/history/characters/fromImperator.txt";
		using var stream = File.OpenWrite(pathForCharactersFromIR);
		using var output = new StreamWriter(stream, System.Text.Encoding.UTF8);
		foreach (var character in charactersFromIR) {
			CharacterOutputter.OutputCharacter(output, character, conversionDate);
		}
		
		var pathForCharactersFromCK3 = $"output/{outputModName}/history/characters/fromCK3.txt";
		using var stream2 = File.OpenWrite(pathForCharactersFromCK3);
		using var output2 = new StreamWriter(stream2, System.Text.Encoding.UTF8);
		foreach (var character in charactersFromCK3) {
			CharacterOutputter.OutputCharacter(output2, character, conversionDate);
		}
	}
}
