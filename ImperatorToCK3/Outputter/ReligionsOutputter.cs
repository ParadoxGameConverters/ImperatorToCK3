using commonItems.Serialization;
using ImperatorToCK3.CK3.Religions;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter; 

public static class ReligionsOutputter {
	public static void OutputHolySites(string outputModName, ReligionCollection ck3ReligionCollection) {
		var outputPath = Path.Combine("output", outputModName, "common", "religions", "holy_sites", "IRtoCK3_sites.txt");

		using var outputStream = File.OpenWrite(outputPath);
		using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);

		foreach (var site in ck3ReligionCollection.HolySites.Where(s=>s.IsGeneratedByConverter)) {
			output.WriteLine($"{site.Id}={PDXSerializer.Serialize(site)}");
		}
	}
}