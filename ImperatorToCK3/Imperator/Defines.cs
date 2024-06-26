using commonItems;
using commonItems.Mods;
using ImperatorToCK3.Helpers;
using System;
using System.Text.Json;

namespace ImperatorToCK3.Imperator;

public sealed class Defines {
	public int CohortSize { get; private set; } = 500;

	public void LoadDefines(ModFilesystem imperatorModFs) {
		Logger.Info("Loading Imperator defines...");

		var definesFiles = imperatorModFs.GetAllFilesInFolderRecursive("common/defines");
		foreach (var filePath in definesFiles) {
			string jsonString = string.Empty;
			try {
				jsonString = RakalyCaller.GetJson(filePath);
				var jsonRoot = JsonDocument.Parse(jsonString).RootElement;

				if (jsonRoot.TryGetProperty("NUnit", out var unitProp) && unitProp.TryGetProperty("COHORT_SIZE", out var cohortSizeProp)) {
					CohortSize = cohortSizeProp.GetInt32();
				}
			} catch (Exception e) {
				Logger.Warn($"Failed to read defines from {filePath}:\n\tJSON string: {jsonString}\n\texception: {e}");
			}
		}

		Logger.IncrementProgress();
	}
}