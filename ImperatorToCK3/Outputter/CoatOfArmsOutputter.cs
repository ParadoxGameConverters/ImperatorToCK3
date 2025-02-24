using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.CoA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;
public static class CoatOfArmsOutputter {
	internal static async Task OutputCoas(string outputModPath, Title.LandedTitles titles, IEnumerable<Dynasty> dynasties, CoaMapper ck3CoaMapper) {
		Logger.Info("Outputting coats of arms...");
		
		// Output variables (like "@smCastleX = 0.27" in vanilla CK3).
		var sb = new System.Text.StringBuilder();
		foreach (var (variableName, variableValue) in ck3CoaMapper.VariablesToOutput) {
			sb.AppendLine($"@{variableName}={variableValue}");
		}

		// Output CoAs for titles.
		foreach (var title in titles) {
			var coa = title.CoA;
			if (coa is null) {
				continue;
			}
			
			// If the title's CoA is the same as in CoaMapper, we don't need to output the CoA (because it's already in the CK3 mod filesystem).
			if (ck3CoaMapper.GetCoaForFlagName(title.Id, warnIfMissing: false) == coa) {
				continue;
			}

			sb.AppendLine($"{title.Id}={coa}");
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
		foreach (var fileInfo in filePaths) {
			var destPath = Path.Combine(outputPath, relativePatternsPath, fileInfo.RelativePath);
			SystemUtils.TryCopyFile(fileInfo.AbsolutePath, destPath);
		}

		Logger.IncrementProgress();
	}
}