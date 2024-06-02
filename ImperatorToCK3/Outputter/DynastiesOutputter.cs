using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Outputter;

public static class DynastiesOutputter {
	public static void OutputDynasties(string outputModPath, DynastyCollection dynasties) {
		Logger.Info("Writing dynasties...");
		var outputPath = Path.Combine(outputModPath, "common/dynasties/irtock3_all_dynasties.txt");
	
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8);
		foreach (var dynasty in dynasties.OrderBy(d => d.Id)) {
			output.WriteLine($"{dynasty.Id}={PDXSerializer.Serialize(dynasty, string.Empty)}");
		}
	}

	public static void OutputHouses(string outputModPath, HouseCollection houses) {
		Logger.Info("Writing dynasty houses...");
		var outputPath = Path.Combine(outputModPath, "common/dynasty_houses/irtock3_all_houses.txt");
		
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8);
		foreach (var house in houses.OrderBy(h => h.Id)) {
			output.WriteLine($"{house.Id}={PDXSerializer.Serialize(house, string.Empty)}");
		}
	}
}
