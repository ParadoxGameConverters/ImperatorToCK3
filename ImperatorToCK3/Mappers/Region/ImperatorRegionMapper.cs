using commonItems;
using commonItems.Collections;
using System.IO;

namespace ImperatorToCK3.Mappers.Region {
	public class ImperatorRegionMapper {
		private readonly IdObjectCollection<string, ImperatorRegion> regions = new();
		private readonly IdObjectCollection<string, ImperatorArea> areas = new();

		public ImperatorRegionMapper() { }
		public ImperatorRegionMapper(string imperatorPath) {
			Logger.Info("Initializing Imperator Geography");
			var areaFilePath = Path.Combine(imperatorPath, "game/map_data/areas.txt");
			var regionFilePath = Path.Combine(imperatorPath, "game/map_data/regions.txt");
			using var areaFileStream = new FileStream(areaFilePath, FileMode.Open);
			using var regionFileStream = new FileStream(regionFilePath, FileMode.Open);
			var areaReader = new BufferedReader(areaFileStream);
			var regionReader = new BufferedReader(regionFileStream);
			Parser.AbsorbBOM(areaReader);
			Parser.AbsorbBOM(regionReader);
			LoadRegions(areaReader, regionReader);
		}
		private void RegisterRegionKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.String, (reader, regionName) => regions.Add(new ImperatorRegion(regionName, reader)));
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private void RegisterAreaKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.String, (reader, areaName) => areas.Add(new ImperatorArea(areaName, reader)));
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LoadRegions(BufferedReader areaReader, BufferedReader regionReader) {
			var parser = new Parser();
			RegisterAreaKeys(parser);
			parser.ParseStream(areaReader);
			parser.ClearRegisteredRules();

			RegisterRegionKeys(parser);
			parser.ParseStream(regionReader);

			LinkRegions();
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
