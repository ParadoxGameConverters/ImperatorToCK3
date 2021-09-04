using System.IO;
using System.Collections.Generic;
using ImperatorToCK3.CK3.Dynasties;
using commonItems;

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
			using var output = new StreamWriter(outputPath); // dumping all into one file
			output.Write(CommonFunctions.UTF8BOM);
			foreach (var dynasty in dynasties.Values) {
				OutputDynasty(output, dynasty);
			}
		}
	}
}
