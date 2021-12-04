using commonItems;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.Region {
	public class CK3RegionMapper {
		public CK3RegionMapper() { }
		public CK3RegionMapper(string ck3Path, Title.LandedTitles landedTitles) {
			Logger.Info("Initializing Geography.");

			var regionFilePath = Path.Combine(ck3Path, "game/map_data/geographical_region.txt");
			var islandRegionFilePath = Path.Combine(ck3Path, "game/map_data/island_region.txt");

			LoadRegions(landedTitles, regionFilePath, islandRegionFilePath);
		}
		public void LoadRegions(Title.LandedTitles landedTitles, string regionFilePath, string islandRegionFilePath) {
			var parser = new Parser();
			RegisterRegionKeys(parser);
			parser.ParseFile(regionFilePath);
			parser.ParseFile(islandRegionFilePath);

			foreach (var title in landedTitles) {
				var titleRank = title.Rank;
				if (titleRank == TitleRank.county) {
					counties[title.Id] = title;
				} else if (titleRank == TitleRank.duchy) {
					duchies[title.Id] = title;
				}
			}

			LinkRegions();
		}
		public bool ProvinceIsInRegion(ulong provinceId, string regionName) {
			if (regions.TryGetValue(regionName, out var region)) {
				return region.ContainsProvince(provinceId);
			}

			// "Regions" are such a fluid term.
			if (duchies.TryGetValue(regionName, out var duchy)) {
				return duchy.DuchyContainsProvince(provinceId);
			}

			// And sometimes they don't mean what people think they mean at all.
			return counties.TryGetValue(regionName, out var county) && county.CountyProvinces.Contains(provinceId);
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
				if (county.CountyProvinces.Contains(provinceId)) {
					return countyName;
				}
			}
			Logger.Warn($"Province ID {provinceId} has no parent county name!");
			return null;
		}
		public string? GetParentDuchyName(ulong provinceId) {
			foreach (var (duchyName, duchy) in duchies) {
				if (duchy.DuchyContainsProvince(provinceId)) {
					return duchyName;
				}
			}
			Logger.Warn($"Province ID {provinceId} has no parent duchy name!");
			return null;
		}
		public string? GetParentRegionName(ulong provinceId) {
			foreach (var (regionName, region) in regions) {
				if (region.ContainsProvince(provinceId)) {
					return regionName;
				}
			}
			Logger.Warn($"Province ID {provinceId} has no parent region name!");
			return null;
		}

		private void RegisterRegionKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.String, (reader, regionName) => {
				regions[regionName] = CK3Region.Parse(regionName, reader);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private void LinkRegions() {
			foreach (var region in regions.Values) {
				region.LinkRegions(regions, duchies, counties);
			}
		}
		private readonly Dictionary<string, CK3Region> regions = new();
		private readonly Dictionary<string, Title> duchies = new();
		private readonly Dictionary<string, Title> counties = new();
	}
}
