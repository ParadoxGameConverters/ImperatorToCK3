using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;
using ZLinq;

namespace ImperatorToCK3.Mappers.Region;

internal sealed class CK3RegionMapper {
	public IReadOnlyDictionary<string, CK3Region> Regions => regions;

	public CK3RegionMapper() { }
	public CK3RegionMapper(ModFilesystem ck3ModFS, Title.LandedTitles landedTitles) {
		Logger.Info("Initializing Geography...");

		LoadRegions(ck3ModFS, landedTitles);

		Logger.IncrementProgress();
	}
	public void LoadRegions(ModFilesystem ck3ModFS, Title.LandedTitles landedTitles) {
		var parser = new Parser();
		RegisterRegionKeys(parser);

		var regionsFolderPath = Path.Combine("map_data", "geographical_regions");
		parser.ParseGameFolder(regionsFolderPath, ck3ModFS, "txt", recursive: true);

		var islandRegionFilePath = Path.Combine("map_data", "island_region.txt");
		parser.ParseGameFile(islandRegionFilePath, ck3ModFS);

		foreach (var title in landedTitles) {
			var titleRank = title.Rank;
			if (titleRank == TitleRank.county) {
				counties[title.Id] = title;
			} else if (titleRank == TitleRank.duchy) {
				duchies[title.Id] = title;
			} else if (titleRank == TitleRank.kingdom) {
				kingdoms[title.Id] = title;
			}
		}

		LinkRegions();
		
		// Log duchies that don't have any de jure counties.
		// Such duchies should probably be removed from the regions.
		var validDeJureDuchyIds = landedTitles.GetDeJureDuchies().AsValueEnumerable().Select(d => d.Id).ToFrozenSet();
		foreach (var region in regions.Values) {
			foreach (var regionDuchyId in region.Duchies.Keys) {
				if (!validDeJureDuchyIds.Contains(regionDuchyId)) {
					Logger.Debug($"Region {region.Name} contains duchy {regionDuchyId} which has no de jure counties!");
				}
			}
		}
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
		return counties.TryGetValue(regionName, out var county) && county.CountyProvinceIds.AsValueEnumerable().Contains(provinceId);
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
			if (county.CountyProvinceIds.AsValueEnumerable().Contains(provinceId)) {
				return countyName;
			}
		}
		Logger.Warn($"CK3 province ID {provinceId} has no parent county name!");
		return null;
	}
	public string? GetParentDuchyName(ulong provinceId) {
		foreach (var (duchyName, duchy) in duchies) {
			if (duchy.DuchyContainsProvince(provinceId)) {
				return duchyName;
			}
		}
		Logger.Warn($"CK3 province ID {provinceId} has no parent duchy name!");
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
			region.LinkRegions(regions, kingdoms, duchies, counties);
		}
	}
	private readonly Dictionary<string, CK3Region> regions = [];
	private readonly Dictionary<string, Title> kingdoms = [];
	private readonly Dictionary<string, Title> duchies = [];
	private readonly Dictionary<string, Title> counties = [];
}