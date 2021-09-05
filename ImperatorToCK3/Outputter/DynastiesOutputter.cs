using System.IO;
using System.Collections.Generic;
using ImperatorToCK3.CK3.Dynasties;
using System.Text;

namespace ImperatorToCK3.Outputter {
	public static class DynastiesOutputter {
		private static void OutputDynasty(StreamWriter output, Dynasty dynasty) {
			// output ID, name and culture
			output.WriteLine($"{dynasty.ID} = {{");
			output.WriteLine($"\tname = \"{dynasty.Name}\"");
			output.WriteLine($"\tculture = {dynasty.Culture}");
			output.WriteLine("}");
		}

		public static void OutputDynasties(string outputModName, Dictionary<string, Dynasty> dynasties) {
			var outputPath = Path.Combine("output", outputModName, "common/dynasties/imp_dynasties.txt");

			using FileStream stream = File.OpenWrite(outputPath);
			using var output = new StreamWriter(stream, encoding: Encoding.UTF8); // dumping all into one file
			foreach (var dynasty in dynasties.Values) {
				OutputDynasty(output, dynasty);
			}
		}
	}
}
