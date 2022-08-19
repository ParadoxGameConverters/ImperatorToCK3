using commonItems;
using commonItems.Mods;
using ImperatorToCK3.Helpers;
using Newtonsoft.Json.Linq;
using System;

namespace ImperatorToCK3.Imperator; 

public class Defines {
	public int CohortSize { get; private set; } = 500;

	public void LoadDefines(ModFilesystem imperatorModFs) {
		Logger.Info("Loading Imperator defines...");
		
		var definesFiles = imperatorModFs.GetAllFilesInFolderRecursive("common/defines");
		Logger.Debug($"Defines files: {string.Join("; ", definesFiles)}");
		foreach (var filePath in definesFiles) {
			try {
				var jsonString = RakalyCaller.GetJson(filePath);
				var jsonObject = JObject.Parse(jsonString);

				var cohortSize = (int?)jsonObject["NUnit"]?["COHORT_SIZE"];
				if (cohortSize is not null) {
					CohortSize = (int)cohortSize;
				}
			} catch (Exception e) {
				Logger.Warn($"Failed to read defines from {filePath}: {e}");
			}
		}
		
		Logger.IncrementProgress();
	}
}