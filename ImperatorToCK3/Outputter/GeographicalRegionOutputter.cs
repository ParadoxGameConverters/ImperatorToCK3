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

		const string regionsFileName = "irtock3_all_regions.txt";
		var outputPath = Path.Combine(outputModPath, "map_data/geographical_regions", regionsFileName);
		var output = FileHelper.OpenWriteWithRetries(outputPath, encoding: Encoding.UTF8);
		await using (output.ConfigureAwait(false)) {
			await output.WriteAsync(sb.ToString()).ConfigureAwait(false);
		}

		// There may be other region files due to being in the removable_file_blocks*.txt, replaceable_file_blocks*.txt
		// or blankMod. Their contents are already in the irtock3_all_regions.txt file outputted above,
		// so we can remove the extra files.
		var regionsFiles = Directory.GetFiles(Path.Combine(outputModPath, "map_data/geographical_regions"), "*.txt");
		foreach (var file in regionsFiles) {
			if (Path.GetFileName(file) == regionsFileName) {
				continue;
			}
			File.Delete(file);
		}
	}
}