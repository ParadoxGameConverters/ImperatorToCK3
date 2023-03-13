using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Culture;
using System.Linq;

namespace ImperatorToCK3.CK3.Dynasties;

public class DynastyCollection : IdObjectCollection<string, Dynasty> {
	public void ImportImperatorFamilies(Imperator.World irWorld, CultureMapper cultureMapper, LocDB locDB, Date date) {
		Logger.Info("Importing Imperator Families...");

		var imperatorCharacters = irWorld.Characters;
		// The collection only holds dynasties converted from Imperator families, as vanilla ones aren't modified.
		foreach (var family in irWorld.Families) {
			if (family.Minor) {
				continue;
			}

			var newDynasty = new Dynasty(family, imperatorCharacters, irWorld.CulturesDB, cultureMapper, locDB, date);
			Add(newDynasty);
		}
		Logger.Info($"{Count} total families imported.");

		Logger.IncrementProgress();
	}

	public void SetCoasForRulingDynasties(Title.LandedTitles titles, Date date) {
		Logger.Info("Setting dynasty CoAs from titles...");
		foreach (var title in titles.Where(t => t.CoA is not null && t.ImperatorCountry is not null)) {
			var dynastyId = title.ImperatorCountry!.Monarch?.CK3Character?.GetDynastyId(date);

			// Try to use title CoA for dynasty CoA.
			if (dynastyId is not null && TryGetValue(dynastyId, out var dynasty) && dynasty.CoA is null) {
				dynasty.CoA = new StringOfItem(title.Id);
			}
		}
		Logger.IncrementProgress();
	}
}