using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Mappers.Localization;

namespace ImperatorToCK3.CK3.Dynasties {
	public class DynastyCollection : IdObjectCollection<string, Dynasty> {
		public void ImportImperatorFamilies(Imperator.World impWorld, LocalizationMapper localizationMapper) {
			Logger.Info("Importing Imperator Families.");

			// the collection only holds dynasties converted from Imperator families, as vanilla ones aren't modified
			foreach (var family in impWorld.Families) {
				if (family.Minor) {
					continue;
				}

				var newDynasty = new Dynasty(family, localizationMapper);
				Add(newDynasty);
			}
			Logger.Info($"{Count} total families imported.");
		}
	}
}
