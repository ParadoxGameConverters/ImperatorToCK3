using commonItems;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.Region {
	public class CK3RegionMapper : Parser {
		public CK3RegionMapper() { }
		public CK3RegionMapper(string ck3Path, LandedTitles landedTitles) {
			Logger.Info("Initializing Geography.");

			var regionFilePath = Path.Combine(ck3Path, "game/map_data/geographical_region.txt");
			var islandRegionFilePath = Path.Combine(ck3Path, "game/map_data/island_region.txt");

			LoadRegions(landedTitles, regionFilePath, islandRegionFilePath);
		}
		public void LoadRegions(LandedTitles landedTitles, string regionFilePath, string islandRegionFilePath) {
			RegisterRegionKeys();
			ParseFile(regionFilePath);
			ParseFile(islandRegionFilePath);
			ClearRegisteredRules();

			foreach (var (titleName, title) in landedTitles) {
				var titleRank = title.Rank;
				if (titleRank == TitleRank.county) {
					counties[titleName] = title;
				} else if (titleRank == TitleRank.duchy) {
					duchies[titleName] = title;
				}
			}

			LinkRegions();
		}
		public bool ProvinceIsInRegion(ulong provinceId, string regionName) {
			if (regions.TryGetValue(regionName, out var region) && region is not null) {
				return region.ContainsProvince(provinceId);
			}

			// "Regions" are such a fluid term.
			if (duchies.TryGetValue(regionName, out var duchy) && duchy is not null) {
				return duchy.DuchyContainsProvince(provinceId);
			}

			// And sometimes they don't mean what people think they mean at all.
			return counties.TryGetValue(regionName, out var county) &&
				county?.CountyProvinces.Contains(provinceId) == true;
		}
		public bool RegionNameIsValid(string regionName) {
			if (regions.ContainsKey(regionName)) {
				return true;
			}

			// Who knows what the mapper needs. All kinds of stuff.
			if (duchies.ContainsKey(regionName)) {
				return true;
			}

			if (counties.ContainsKey(regionName)) {
				return true;
			}

			return false;
		}
		public string? GetParentCountyName(ulong provinceId) {
			foreach (var (countyName, county) in counties) {
				if (county?.CountyProvinces.Contains(provinceId) == true) {
					return countyName;
				}
			}
			Logger.Warn($"Province ID {provinceId} has no parent county name!");
			return null;
		}
		public string? GetParentDuchyName(ulong provinceId) {
			foreach (var (duchyName, duchy) in duchies) {
				if (duchy?.DuchyContainsProvince(provinceId) == true) {
					return duchyName;
				}
			}
			Logger.Warn($"Province ID {provinceId} has no parent duchy name!");
			return null;
		}
		public string? GetParentRegionName(ulong provinceId) {
			foreach (var (regionName, region) in regions) {
				if (region?.ContainsProvince(provinceId) == true) {
					return regionName;
				}
			}
			Logger.Warn($"Province ID {provinceId} has no parent region name!");
			return null;
		}

		private void RegisterRegionKeys() {
			RegisterRegex(@"[\w_&]+", (reader, regionName) => {
				var newRegion = CK3Region.Parse(reader);
				regions[regionName] = newRegion;
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private void LinkRegions() {
			foreach (var (regionName, region) in regions) {
				if (region is null) {
					Logger.Warn($"LinkRegions: {regionName} is null!");
					continue;
				}
				// regions
				foreach (var requiredRegionName in region.Regions.Keys) {
					if (regions.TryGetValue(requiredRegionName, out var regionToLink) && regionToLink is not null) {
						region.LinkRegion(requiredRegionName, regionToLink);
					} else {
						throw new KeyNotFoundException($"Region's {regionName} region {requiredRegionName} does not exist!");
					}
				}

				// duchies
				foreach (var requiredDuchyName in region.Duchies.Keys) {
					if (duchies.TryGetValue(requiredDuchyName, out var duchyToLink) && duchyToLink is not null) {
						region.LinkDuchy(duchyToLink);
					} else {
						throw new KeyNotFoundException($"Region's {regionName} duchy {requiredDuchyName} does not exist!");
					}
				}

				// counties
				foreach (var requiredCountyName in region.Counties.Keys) {
					if (counties.TryGetValue(requiredCountyName, out var countyToLink) && countyToLink is not null) {
						region.LinkCounty(countyToLink);
					} else {
						throw new KeyNotFoundException($"Region's {regionName} county {requiredCountyName} does not exist!");
					}
				}
			}
		}
		private readonly Dictionary<string, CK3Region?> regions = new();
		private readonly Dictionary<string, Title?> duchies = new();
		private readonly Dictionary<string, Title?> counties = new();
	}
}
