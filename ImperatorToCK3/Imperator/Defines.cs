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
		foreach (var filePath in definesFiles) {
			string jsonString = string.Empty;
			try {
				jsonString = RakalyCaller.GetJson(filePath);
				var jsonObject = JObject.Parse(jsonString);

				var cohortSize = (int?)jsonObject["NUnit"]?["COHORT_SIZE"];
				if (cohortSize is not null) {
					CohortSize = (int)cohortSize;
				}
			} catch (Exception e) {
				Logger.Warn($"Failed to read defines from {filePath}:\n\tJSON string: {jsonString}\n\texception: {e}");
			}
		}
		
		Logger.IncrementProgress();
	}
}