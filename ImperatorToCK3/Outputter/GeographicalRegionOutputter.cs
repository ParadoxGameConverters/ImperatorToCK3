using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Mappers.Region;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal static class GeographicalRegionOutputter {
	public static async Task OutputRegions(string outputModPath, CK3RegionMapper ck3RegionMapper) {
		// We need to output all geographical regions, because we modify de jure kingdoms setup
		// and regions now reference de jure kingdoms.
		Logger.Info("Writing dynasties...");

		var sb = new StringBuilder();
		foreach (var region in ck3RegionMapper.Regions.Values) {
			sb.AppendLine(region.ToString());
		}

		var outputPath = Path.Combine(outputModPath, "map_data/geographical_regions/irtock3_all_regions.txt");
		var output = FileHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8);
		await using (output.ConfigureAwait(false)) {
			await output.WriteAsync(sb.ToString()).ConfigureAwait(false);
		}
	}
}