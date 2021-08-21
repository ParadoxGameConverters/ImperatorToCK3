using System.Collections.Generic;
using System.IO;
using commonItems;

namespace ImperatorToCK3.Mappers.Region {
	public class ImperatorRegionMapper : Parser {
		private readonly Dictionary<string, ImperatorRegion> regions = new();
		private readonly Dictionary<string, ImperatorArea> areas = new();

		public ImperatorRegionMapper() { }
		public ImperatorRegionMapper(string imperatorPath) {
			Logger.Info("Initializing Imperator Geography");
			var areaFilePath = Path.Combine(imperatorPath, "game/map_data/areas.txt");
			var regionFilePath = Path.Combine(imperatorPath, "game/map_data/regions.txt");
			using var areaFileStream = new FileStream(areaFilePath, FileMode.Open);
			using var regionFileStream = new FileStream(regionFilePath, FileMode.Open);
			var areaReader = new BufferedReader(areaFileStream);
			var regionReader = new BufferedReader(regionFileStream);
			AbsorbBOM(areaReader);
			AbsorbBOM(regionReader);
			LoadRegions(areaReader, regionReader);
		}
		private void RegisterRegionKeys() {
			RegisterRegex(@"[\w_&]+", (reader, regionName) => {
				regions[regionName] = new ImperatorRegion(reader);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private void RegisterAreaKeys() {
			RegisterRegex(@"[\w_&]+", (reader, areaName) => {
				areas[areaName] = new ImperatorArea(reader);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LoadRegions(BufferedReader areaReader, BufferedReader regionReader) {
			RegisterAreaKeys();
			ParseStream(areaReader);
			ClearRegisteredRules();

			RegisterRegionKeys();
			ParseStream(regionReader);
			ClearRegisteredRules();

			LinkRegions();
		}
		public bool ProvinceIsInRegion(ulong provinceId, string regionName) {
			if (regions.TryGetValue(regionName, out var region) && region is not null) {
				return region.ContainsProvince(provinceId);
			}
			// "Regions" are such a fluid term.
			if (areas.TryGetValue(regionName, out var area) && area is not null) {
				return area.ContainsProvince(provinceId);
			}
			return false;
		}
		public bool RegionNameIsValid(string regionName) {
			if (regions.ContainsKey(regionName)) {
				return true;
			}
			// Who knows what the mapper needs. All kinds of stuff.
			if (areas.ContainsKey(regionName)) {
				return true;
			}
			return false;
		}
		public string? GetParentRegionName(ulong provinceId) {
			foreach (var (regionName, region) in regions) {
				if (region is not null && region.ContainsProvince(provinceId)) {
					return regionName;
				}
			}
			Logger.Warn("Province ID " + provinceId + " has no parent region name!");
			return null;
		}
		public string? GetParentAreaName(ulong provinceId) {
			foreach (var (areaName, area) in areas) {
				if (area is not null && area.ContainsProvince(provinceId)) {
					return areaName;
				}
			}
			Logger.Warn("Province ID " + provinceId + " has no parent area name!");
			return null;
		}
		private void LinkRegions() {
			foreach (var (regionName, region) in regions) {
				foreach (var requiredAreaName in region.Areas.Keys) {
					if (areas.TryGetValue(requiredAreaName, out var area)) {
						region.LinkArea(requiredAreaName, area);
					} else {
						throw new KeyNotFoundException("Region's " + regionName + " area " + requiredAreaName + " does not exist!");
					}
				}
			}
		}
	}
}
