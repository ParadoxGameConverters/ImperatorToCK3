using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.Region {
	public class ImperatorRegionMapper {
		private readonly IdObjectCollection<string, ImperatorRegion> regions = new();
		private readonly IdObjectCollection<string, ImperatorArea> areas = new();

		public ImperatorRegionMapper() { }
		public ImperatorRegionMapper(ModFilesystem imperatorModFS) {
			Logger.Info("Initializing Imperator Geography...");

			var parser = new Parser();

			RegisterAreaKeys(parser);
			parser.ParseGameFile(Path.Combine("map_data", "areas.txt"), imperatorModFS);

			parser.ClearRegisteredRules();
			RegisterRegionKeys(parser);
			parser.ParseGameFile(Path.Combine("map_data", "regions.txt"), imperatorModFS);

			LinkRegions();
		}
		private void RegisterRegionKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.String, (reader, regionName) => regions.AddOrReplace(new(regionName, reader)));
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private void RegisterAreaKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.String, (reader, areaName) => areas.AddOrReplace(new(areaName, reader)));
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		public bool ProvinceIsInRegion(ulong provinceId, string regionName) {
			if (regions.TryGetValue(regionName, out var region)) {
				return region.ContainsProvince(provinceId);
			}
			// "Regions" are such a fluid term.
			return areas.TryGetValue(regionName, out var area) && area.ContainsProvince(provinceId);
		}
		public bool RegionNameIsValid(string regionName) {
			// Who knows what the mapper needs. All kinds of stuff.
			return regions.ContainsKey(regionName) || areas.ContainsKey(regionName);
		}
		public string? GetParentRegionName(ulong provinceId) {
			foreach (var region in regions) {
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
			foreach (var region in regions) {
				region.LinkAreas(areas);
			}
		}
	}
}
