using commonItems.Serialization;
using ImperatorToCK3.CK3.Dynasties;
using System.IO;
using System.Text;

namespace ImperatorToCK3.Outputter;
public static class DynastiesOutputter {
	public static void OutputDynasties(string outputModName, DynastyCollection dynasties) {
		var outputPath = Path.Combine("output", outputModName, "common", "dynasties", "imp_dynasties.txt");

		using FileStream stream = File.OpenWrite(outputPath);
		using var output = new StreamWriter(stream, encoding: Encoding.UTF8); // dumping all into one file
		foreach (var dynasty in dynasties) {
			output.WriteLine($"{dynasty.Id}={PDXSerializer.Serialize(dynasty, string.Empty)}");
		}
	}
}
