using commonItems;
using ImperatorToCK3.Mappers.Region;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Religion {
	public class ReligionMapping {
		private readonly SortedSet<string> imperatorReligions = new();
		private string ck3Religion = string.Empty;

		private readonly SortedSet<ulong> imperatorProvinces = new();
		private readonly SortedSet<ulong> ck3Provinces = new();

		private readonly SortedSet<string> imperatorRegions = new();
		private readonly SortedSet<string> ck3Regions = new();

		public ImperatorRegionMapper? ImperatorRegionMapper { get; set; }
		public CK3RegionMapper? CK3RegionMapper { get; set; }

		private static readonly Parser parser = new();
		private static ReligionMapping mappingToReturn = new();
		static ReligionMapping() {
			parser.RegisterKeyword("ck3", reader => {
				mappingToReturn.ck3Religion = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("imp", reader => {
				mappingToReturn.imperatorReligions.Add(ParserHelpers.GetString(reader));
			});
			parser.RegisterKeyword("ck3Region", reader => {
				mappingToReturn.ck3Regions.Add(ParserHelpers.GetString(reader));
			});
			parser.RegisterKeyword("impRegion", reader => {
				mappingToReturn.imperatorRegions.Add(ParserHelpers.GetString(reader));
			});
			parser.RegisterKeyword("ck3Province", reader => {
				mappingToReturn.ck3Provinces.Add(ParserHelpers.GetULong(reader));
			});
			parser.RegisterKeyword("impProvince", reader => {
				mappingToReturn.imperatorProvinces.Add(ParserHelpers.GetULong(reader));
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public static ReligionMapping Parse(BufferedReader reader) {
			mappingToReturn = new ReligionMapping();
			parser.ParseStream(reader);
			return mappingToReturn;
		}

		public string? Match(string impReligion, ulong ck3ProvinceId, ulong impProvinceId) {
			if (ImperatorRegionMapper is null) {
				throw new InvalidOperationException("ImperatorRegionMapper is null!");
			}
			if (CK3RegionMapper is null) {
				throw new InvalidOperationException("CK3RegionMapper is null!");
			}

			// We need at least a viable Imperator religion
			if (string.IsNullOrEmpty(impReligion)) {
				return null;
			}

			if (!imperatorReligions.Contains(impReligion)) {
				return null;
			}

			// simple religion-religion match
			if (ck3Provinces.Count == 0 && imperatorProvinces.Count == 0 && ck3Regions.Count == 0 && imperatorRegions.Count == 0) {
				return ck3Religion;
			}

			// ID 0 means no province
			if (ck3ProvinceId == 0 && impProvinceId == 0) {
				return null;
			}

			// This is a CK3 provinces check
			if (ck3Provinces.Contains(ck3ProvinceId)) {
				return ck3Religion;
			}
			// This is a CK3 regions check, it checks if provided ck3Province is within the mapping's ck3Regions
			foreach (var region in ck3Regions) {
				if (!CK3RegionMapper.RegionNameIsValid(region)) {
					Logger.Warn($"Checking for religion {impReligion} inside invalid CK3 region: {region}! Fix the mapping rules!");
					// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
					// for the converter to explode across the logs with invalid names. So, continue.
					continue;
				}
				if (CK3RegionMapper.ProvinceIsInRegion(ck3ProvinceId, region)) {
					return ck3Religion;
				}
			}

			// This is an Imperator provinces check
			if (imperatorProvinces.Contains(impProvinceId)) {
				return ck3Religion;
			}
			// This is an Imperator regions check, it checks if provided impProvince is within the mapping's imperatorRegions
			foreach (var region in imperatorRegions) {
				if (!ImperatorRegionMapper.RegionNameIsValid(region)) {
					Logger.Warn($"Checking for religion {impReligion} inside invalid Imperator region: {region}! Fix the mapping rules!");
					// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
					// for the converter to explode across the logs with invalid names. So, continue.
					continue;
				}
				if (ImperatorRegionMapper.ProvinceIsInRegion(impProvinceId, region)) {
					return ck3Religion;
				}
			}

			return null;
		}
	}
}
