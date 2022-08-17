using commonItems.Mods;
using ImperatorToCK3.Helpers;
using Newtonsoft.Json.Linq;

namespace ImperatorToCK3.Imperator; 

public class Defines {
	public int CohortSize { get; private set; } = 500;

	public void LoadDefines(ModFilesystem imperatorModFs) {
		var definesFiles = imperatorModFs.GetAllFilesInFolderRecursive("common/defines");
		foreach (var filePath in definesFiles) {
			var jsonString = RakalyCaller.GetJson(filePath);
			var jsonObject = JObject.Parse(jsonString);

			var cohortSize = (int?)jsonObject["NUnit"]?["COHORT_SIZE"];
			if (cohortSize is not null) {
				CohortSize = (int)cohortSize;
			}
		}
	}
}