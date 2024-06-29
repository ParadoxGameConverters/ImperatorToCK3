using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Geography;

namespace ImperatorToCK3.Mappers.Region;

public sealed class ImperatorRegionMapper(AreaCollection areaCollection, MapData irMapData) {
	public IdObjectCollection<string, ImperatorRegion> Regions { get; } = [];

	public void LoadRegions(ModFilesystem imperatorModFS, ColorFactory colorFactory) {
		Logger.Info("Initializing Imperator geography...");

		const string regionsFilePath = "map_data/regions.txt";
		Logger.Debug($"Imperator regions file location: {imperatorModFS.GetActualFileLocation(regionsFilePath)}");

		var parser = new Parser();
		RegisterRegionKeys(parser, colorFactory);
		parser.ParseGameFile(regionsFilePath, imperatorModFS);

		Logger.IncrementProgress();
	}

	private void RegisterRegionKeys(Parser parser, ColorFactory colorFactory) {
		parser.RegisterRegex(CommonRegexes.String, (reader, regionName) =>
			Regions.AddOrReplace(new ImperatorRegion(regionName, reader, areaCollection, colorFactory)));
		parser.IgnoreAndLogUnregisteredItems();
	}

	public bool ProvinceIsInRegion(ulong provinceId, string regionName) {
		if (Regions.TryGetValue(regionName, out var region)) {
			return region.ContainsProvince(provinceId);
		}
		// "Regions" are such a fluid term.
		return areaCollection.TryGetValue(regionName, out var area) && area.ContainsProvince(provinceId);
	}
	public bool RegionNameIsValid(string regionName) {
		// Who knows what the mapper needs. All kinds of stuff.
		return Regions.ContainsKey(regionName) || areaCollection.ContainsKey(regionName);
	}
	public string? GetParentRegionName(ulong provinceId) {
		foreach (var region in Regions) {
			if (region.ContainsProvince(provinceId)) {
				return region.Id;
			}
		}

		if (!irMapData.IsImpassable(provinceId)) {
			Logger.Warn($"I:R province ID {provinceId} has no parent region name!");
		}
		return null;
	}
	public string? GetParentAreaName(ulong provinceId) {
		foreach (var area in areaCollection) {
			if (area.ContainsProvince(provinceId)) {
				return area.Id;
			}
		}
		Logger.Warn($"I:R province ID {provinceId} has no parent area name!");
		return null;
	}
}