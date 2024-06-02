using commonItems.Serialization;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Outputter;
public static class DynastiesOutputter {
	public static void OutputDynasties(string outputModName, DynastyCollection dynasties) {
		var outputPath = Path.Combine("output", outputModName, "common/dynasties/irtock3_all_dynasties.txt");
	
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8);
		foreach (var dynasty in dynasties.OrderBy(d => d.Id)) {
			output.WriteLine($"{dynasty.Id}={PDXSerializer.Serialize(dynasty, string.Empty)}");
		}
	}
}
