using commonItems;
using commonItems.Mods;
namespace ImperatorToCK3.Imperator;

internal sealed class ImperatorDefines : Defines {
	public int CohortSize { get; private set; } = 500;

	public new void LoadDefines(ModFilesystem modFS) {
		Logger.Info("Loading Imperator defines...");
		base.LoadDefines(modFS);
		
		var cohortSizeStr = GetValue("NUnit", "COHORT_SIZE");
		if (cohortSizeStr is not null) {
			CohortSize = int.Parse(cohortSizeStr);
		}
		
		Logger.IncrementProgress();
	}
}