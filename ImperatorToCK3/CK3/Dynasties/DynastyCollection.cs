using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Titles;
using System.Linq;

namespace ImperatorToCK3.CK3.Dynasties;

public class DynastyCollection : IdObjectCollection<string, Dynasty> {
	public void ImportImperatorFamilies(Imperator.World impWorld, LocDB locDB) {
		Logger.Info("Importing Imperator Families...");

		var imperatorCharacters = impWorld.Characters;
		// The collection only holds dynasties converted from Imperator families, as vanilla ones aren't modified.
		foreach (var family in impWorld.Families) {
			if (family.Minor) {
				continue;
			}

			var newDynasty = new Dynasty(family, imperatorCharacters, locDB);
			Add(newDynasty);
		}
		Logger.Info($"{Count} total families imported.");
			
		Logger.IncrementProgress();
	}

	public void SetCoasForRulingDynasties(Title.LandedTitles titles) {
		Logger.Info("Setting dynasty CoAs from titles...");
		foreach (var title in titles.Where(t => t.CoA is not null && t.ImperatorCountry is not null)) {
			var dynastyId = title.ImperatorCountry!.Monarch?.CK3Character?.DynastyId;
			
			// Try to use title CoA for dynasty CoA.
			if (dynastyId is not null && TryGetValue(dynastyId, out var dynasty) && dynasty.CoA is null) {
				dynasty.CoA = new StringOfItem(title.Id);
			}
		}
		Logger.IncrementProgress();
	}
}