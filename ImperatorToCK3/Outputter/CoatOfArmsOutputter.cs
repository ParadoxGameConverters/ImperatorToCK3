using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;
public static class CoatOfArmsOutputter {
	public static async Task OutputCoas(string outputModPath, Title.LandedTitles titles, IEnumerable<Dynasty> dynasties) {
		Logger.Info("Outputting coats of arms...");

		// Output CoAs for titles.
		var sb = new System.Text.StringBuilder();
		foreach (var title in titles) {
			var coa = title.CoA;
			if (coa is not null) {
				sb.AppendLine($"{title.Id}={coa}");
			}
		}

		// Output CoAs for dynasties.
		foreach (var dynasty in dynasties.Where(d => d.CoA is not null)) {
			sb.AppendLine($"{dynasty.Id}={dynasty.CoA}");
		}

		var coasPath = Path.Combine(outputModPath, "common/coat_of_arms/coat_of_arms");
		var path = Path.Combine(coasPath, "zzz_IRToCK3_coas.txt");
		await using var coasWriter = new StreamWriter(path);
		await coasWriter.WriteAsync(sb.ToString());

		Logger.IncrementProgress();
	}

	public static void CopyCoaPatterns(ModFilesystem irModFS, string outputPath) {
		Logger.Info("Copying coats of arms patterns...");
		const string relativePatternsPath = "gfx/coat_of_arms/patterns";

		var filePaths = irModFS.GetAllFilesInFolderRecursive(relativePatternsPath);
		foreach (var filePath in filePaths) {
			var index = filePath.IndexOf(relativePatternsPath, StringComparison.Ordinal);
			var relativeFileOutputPath = filePath[index..];
			SystemUtils.TryCopyFile(filePath, Path.Combine(outputPath, relativeFileOutputPath));
		}

		Logger.IncrementProgress();
	}
}