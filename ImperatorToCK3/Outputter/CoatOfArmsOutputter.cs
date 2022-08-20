using commonItems;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter;
public static class CoatOfArmsOutputter {
	public static void OutputCoas(string outputModName, Title.LandedTitles titles, IEnumerable<Dynasty> dynasties) {
		Logger.Info("Outputting coats of arms...");
		var coasPath = Path.Combine("output", outputModName, "common", "coat_of_arms", "coat_of_arms");
		
		var path = Path.Combine(coasPath, "IRToCK3_coas.txt");
		using var coasWriter = new StreamWriter(path);
		
		// Output CoAs for titles.
		foreach (var title in titles) {
			var coa = title.CoA;
			if (coa is not null) {
				coasWriter.WriteLine($"{title.Id}={coa}");
			}
		}
		
		// Output CoAs for dynasties.
		foreach (var dynasty in dynasties.Where(d=>d.CoA is not null)) {
			coasWriter.WriteLine($"{dynasty.Id}={dynasty.CoA}");
		}
		
		Logger.IncrementProgress();
	}

	public static void CopyCoaPatterns(string imperatorPath, string outputPath) {
		Logger.Info("Copying coats of arms patterns...");
		SystemUtils.TryCopyFolder(
			Path.Combine(imperatorPath, "game", "gfx", "coat_of_arms", "patterns"),
			Path.Combine(outputPath, "gfx", "coat_of_arms", "patterns")
		);
		Logger.IncrementProgress();
	}
}