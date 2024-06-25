using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class DynastiesOutputter {
	public static async Task OutputDynasties(string outputModPath, DynastyCollection dynasties) {
		Logger.Info("Writing dynasties...");

		var sb = new StringBuilder();
		foreach (var dynasty in dynasties.OrderBy(d => d.Id)) {
			sb.AppendLine($"{dynasty.Id}={PDXSerializer.Serialize(dynasty, string.Empty)}");
		}
		
		var outputPath = Path.Combine(outputModPath, "common/dynasties/irtock3_all_dynasties.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
	}

	public static async Task OutputHouses(string outputModPath, HouseCollection houses) {
		Logger.Info("Writing dynasty houses...");

		var sb = new StringBuilder();
		foreach (var house in houses.OrderBy(h => h.Id)) {
			sb.AppendLine($"{house.Id}={PDXSerializer.Serialize(house, string.Empty)}");
		}
		
		var outputPath = Path.Combine(outputModPath, "common/dynasty_houses/irtock3_all_houses.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
	}
	
	public static async Task OutputDynastiesAndHouses(string outputModPath, DynastyCollection dynasties, HouseCollection houses) {
		await Task.WhenAll(
			OutputDynasties(outputModPath, dynasties),
			Task.Run(() => OutputHouses(outputModPath, houses))
		);
		
		Logger.IncrementProgress();
	}
}
