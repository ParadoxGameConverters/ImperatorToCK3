using commonItems;
using ImperatorToCK3.CK3.Characters;
using System.IO;

namespace ImperatorToCK3.Outputter {
	public static class CharactersOutputter {
		public static void OutputCharacters(string outputModName, CharacterCollection characters, Date conversionDate) {
			// dumping all into one file
			var path = $"output/{outputModName}/history/characters/fromImperator.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, System.Text.Encoding.UTF8);
			foreach (var character in characters) {
				CharacterOutputter.OutputCharacter(output, character, conversionDate);
			}

			OutputCharactersDNA(outputModName, characters);
		}

		private static void OutputCharactersDNA(string outputModName, CharacterCollection characters) {
			Logger.Info("Outputting DNA...");
			// dumping all into one file
			var path = "output/" + outputModName + "/common/dna_data/ir_dna_data.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, System.Text.Encoding.UTF8);
			foreach (var character in characters) {
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
