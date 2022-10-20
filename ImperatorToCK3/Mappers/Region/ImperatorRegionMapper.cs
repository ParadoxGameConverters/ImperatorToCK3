using commonItems;
using commonItems.Collections;
using commonItems.Mods;

namespace ImperatorToCK3.Mappers.Region; 

public class ImperatorRegionMapper {
	public IdObjectCollection<string, ImperatorRegion> Regions { get; } = new();
	private readonly IdObjectCollection<string, ImperatorArea> areas = new();

	public ImperatorRegionMapper() { }
	public ImperatorRegionMapper(ModFilesystem imperatorModFS) {
		Logger.Info("Initializing Imperator geography...");

		var parser = new Parser();
			
		const string areasFilePath = "map_data/areas.txt";
		Logger.Debug($"Imperator areas file location: {imperatorModFS.GetActualFileLocation(areasFilePath)}");
			
		RegisterAreaKeys(parser);
		parser.ParseGameFile(areasFilePath, imperatorModFS);
		parser.ClearRegisteredRules();

		const string regionsFilePath = "map_data/regions.txt";
		Logger.Debug($"Imperator regions file location: {imperatorModFS.GetActualFileLocation(regionsFilePath)}");
			
		RegisterRegionKeys(parser);
		parser.ParseGameFile(regionsFilePath, imperatorModFS);

		LinkRegions();
		
		Logger.IncrementProgress();
	}
	private void RegisterRegionKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.String, (reader, regionName) => Regions.AddOrReplace(new(regionName, reader)));
		parser.IgnoreAndLogUnregisteredItems();
	}
	private void RegisterAreaKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.String, (reader, areaName) => areas.AddOrReplace(new(areaName, reader)));
		parser.IgnoreAndLogUnregisteredItems();
	}

	public bool ProvinceIsInRegion(ulong provinceId, string regionName) {
		if (Regions.TryGetValue(regionName, out var region)) {
			return region.ContainsProvince(provinceId);
		}
		// "Regions" are such a fluid term.
		return areas.TryGetValue(regionName, out var area) && area.ContainsProvince(provinceId);
	}
	public bool RegionNameIsValid(string regionName) {
		// Who knows what the mapper needs. All kinds of stuff.
		return Regions.ContainsKey(regionName) || areas.ContainsKey(regionName);
	}
	public string? GetParentRegionName(ulong provinceId) {
		foreach (var region in Regions) {
			if (region.ContainsProvince(provinceId)) {
				return region.Id;
			}
		}
		Logger.Warn($"Province ID {provinceId} has no parent region name!");
		return null;
	}
	public string? GetParentAreaName(ulong provinceId) {
		foreach (var area in areas) {
			if (area.ContainsProvince(provinceId)) {
				return area.Id;
			}
		}
		Logger.Warn($"Province ID {provinceId} has no parent area name!");
		return null;
	}
	private void LinkRegions() {
		foreach (var region in Regions) {
			region.LinkAreas(areas);
		}
	}
}