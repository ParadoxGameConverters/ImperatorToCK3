using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.Imperator.Provinces;
using System.Linq;

namespace ImperatorToCK3.Imperator.Geography;

public sealed class AreaCollection : IdObjectCollection<string, Area> {
	public void LoadAreas(ModFilesystem imperatorModFS, ProvinceCollection provinceCollection) {
		Logger.Info("Loading Imperator areas...");

		const string areasFilePath = "map_data/areas.txt";
		Logger.Debug($"Imperator areas file location: {imperatorModFS.GetActualFileLocation(areasFilePath)}");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, areaName) => AddOrReplace(new(areaName, reader, provinceCollection)));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFile(areasFilePath, imperatorModFS);

		if (Area.IgnoredKeywords.Any()) {
			Logger.Debug($"Ignored area keywords: {Area.IgnoredKeywords}");
		}
		Logger.IncrementProgress();
	}
}