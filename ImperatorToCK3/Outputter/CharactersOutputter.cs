using System.Collections.Generic;
using System.IO;
using ImperatorToCK3.CK3.Characters;
using commonItems;

namespace ImperatorToCK3.Outputter {
	public static class CharactersOutputter {
		public static void OutputCharacters(string outputModName, Dictionary<string, Character> characters, Date ck3BookmarkDate) {
			// dumping all into one file
			var path = "output/" + outputModName + "/history/characters/fromImperator.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, System.Text.Encoding.UTF8);
			foreach (var character in characters.Values) {
				CharacterOutputter.OutputCharacter(output, character, ck3BookmarkDate);
			}

			OutputCharactersDNA(outputModName, characters);
		}

		private static void OutputCharactersDNA(string outputModName, Dictionary<string, Character> characters) {
			Logger.Info("Outputting DNA...");
			// dumping all into one file
			var path = "output/" + outputModName + "/common/dna_data/ir_dna_data.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, System.Text.Encoding.UTF8);
			foreach(var character in characters.Values) {
				var dna = character.DNA;
				if (dna is null) {
					continue;
				}
				output.WriteLine($"{dna.Id}={{");
				output.WriteLine("\tportrait_info={");

				dna.OutputGenes(output);

				output.WriteLine("\t}");
				output.WriteLine("\tenabled=yes");
				output.WriteLine("}");
			}
		}
	}
}
