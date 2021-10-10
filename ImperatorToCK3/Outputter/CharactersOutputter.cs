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

			OutputDNA(outputModName, characters);
		}

		private static void OutputDNA(string outputModName, Dictionary<string, Character> characters) {
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
				output.WriteLine("\t\tgenes={");

				var hairCoords1 = dna.HairCoordinates;
				var hairCoords2 = dna.HairCoordinates2;
				output.WriteLine($"\t\t\thair_color={{{hairCoords1.X} {hairCoords1.Y} {hairCoords2.X} {hairCoords2.Y}}}");
				var skinCoords1 = dna.SkinCoordinates;
				var skinCoords2 = dna.SkinCoordinates2;
				output.WriteLine($"\t\t\tskin_color={{{skinCoords1.X} {skinCoords1.Y} {skinCoords2.X} {skinCoords2.Y}}}");
				var eyeCoords1 = dna.EyeCoordinates;
				var eyeCoords2 = dna.EyeCoordinates2;
				output.WriteLine($"\t\t\teye_color={{{eyeCoords1.X} {eyeCoords1.Y} {eyeCoords2.X} {eyeCoords2.Y}}}");
				foreach (var line in dna.DNALines) {
					output.WriteLine(line);
				}

				output.WriteLine("\t\t}");
				output.WriteLine("\t}");
				output.WriteLine("\tenabled=yes");
				output.WriteLine("}");
			}
		}
	}
}
