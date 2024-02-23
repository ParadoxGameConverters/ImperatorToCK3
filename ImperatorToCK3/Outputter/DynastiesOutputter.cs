using commonItems.Serialization;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Text;

namespace ImperatorToCK3.Outputter;
public static class DynastiesOutputter {
	public static void OutputDynasties(string outputModName, DynastyCollection dynasties) {
		var outputPath = Path.Combine("output", outputModName, "common/dynasties/ir_dynasties.txt");

		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8); // dumping all into one file
		foreach (var dynasty in dynasties) {
			output.WriteLine($"{dynasty.Id}={PDXSerializer.Serialize(dynasty, string.Empty)}");
		}
	}
}
