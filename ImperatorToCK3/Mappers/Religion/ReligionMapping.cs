using commonItems;
using ImperatorToCK3.Mappers.Region;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Religion {
	public class ReligionMapping {
		private readonly SortedSet<string> imperatorReligions = new();
		public string? CK3FaithId { get; private set; }

		private readonly SortedSet<ulong> imperatorProvinces = new();
		private readonly SortedSet<ulong> ck3Provinces = new();

		private readonly SortedSet<string> imperatorRegions = new();
		private readonly SortedSet<string> ck3Regions = new();

		private bool? heresiesInHistoricalAreas;
		private bool warnWhenMissing = true; // whether to log a warning when the CK3 faith is not found

		private static readonly Parser parser = new();
		private static ReligionMapping mappingToReturn = new();
		static ReligionMapping() {
			parser.RegisterKeyword("ck3", reader => mappingToReturn.CK3FaithId = reader.GetString());
			parser.RegisterKeyword("imp", reader => mappingToReturn.imperatorReligions.Add(reader.GetString()));
			parser.RegisterKeyword("ck3Region", reader => mappingToReturn.ck3Regions.Add(reader.GetString()));
			parser.RegisterKeyword("impRegion", reader => mappingToReturn.imperatorRegions.Add(reader.GetString()));
			parser.RegisterKeyword("ck3Province", reader => mappingToReturn.ck3Provinces.Add(reader.GetULong()));
			parser.RegisterKeyword("impProvince", reader => mappingToReturn.imperatorProvinces.Add(reader.GetULong()));
			parser.RegisterKeyword("heresiesInHistoricalAreas", reader => mappingToReturn.heresiesInHistoricalAreas = reader.GetPDXBool());
			parser.RegisterKeyword("warnWhenMissing", reader => mappingToReturn.warnWhenMissing = reader.GetPDXBool());
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public static ReligionMapping Parse(BufferedReader reader) {
			mappingToReturn = new ReligionMapping();
			parser.ParseStream(reader);
			return mappingToReturn;
		}

		public string? Match(string impReligion,
			ulong ck3ProvinceId,
			ulong impProvinceId,
			Configuration config,
			ImperatorRegionMapper imperatorRegionMapper,
			CK3RegionMapper ck3RegionMapper
		) {
			// We need at least a viable Imperator religion
			if (string.IsNullOrEmpty(impReligion)) {
				return null;
			}

			if (!imperatorReligions.Contains(impReligion)) {
				return null;
			}

			if (heresiesInHistoricalAreas is not null &&
			    config.HeresiesInHistoricalAreas != heresiesInHistoricalAreas.Value) {
				return null;
			}

			// simple religion-religion match
			if (ck3Provinces.Count == 0 && imperatorProvinces.Count == 0 && ck3Regions.Count == 0 && imperatorRegions.Count == 0) {
				return CK3FaithId;
			}

			// ID 0 means no province
			if (ck3ProvinceId == 0 && impProvinceId == 0) {
				return null;
			}

			// This is a CK3 provinces check
			if (ck3Provinces.Contains(ck3ProvinceId)) {
				return CK3FaithId;
			}
			// This is a CK3 regions check, it checks if provided ck3Province is within the mapping's ck3Regions
			foreach (var region in ck3Regions) {
				if (!ck3RegionMapper.RegionNameIsValid(region)) {
					Logger.Warn($"Checking for religion {impReligion} inside invalid CK3 region: {region}! Fix the mapping rules!");
					// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
					// for the converter to explode across the logs with invalid names. So, continue.
					continue;
				}
				if (ck3RegionMapper.ProvinceIsInRegion(ck3ProvinceId, region)) {
					return CK3FaithId;
				}
			}

			// This is an Imperator provinces check
			if (imperatorProvinces.Contains(impProvinceId)) {
				return CK3FaithId;
			}
			// This is an Imperator regions check, it checks if provided impProvince is within the mapping's imperatorRegions
			foreach (var region in imperatorRegions) {
				if (!imperatorRegionMapper.RegionNameIsValid(region)) {
					Logger.Warn($"Checking for religion {impReligion} inside invalid Imperator region: {region}! Fix the mapping rules!");
					// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
					// for the converter to explode across the logs with invalid names. So, continue.
					continue;
				}
				if (imperatorRegionMapper.ProvinceIsInRegion(impProvinceId, region)) {
					return CK3FaithId;
				}
			}

			return null;
		}
	}
}
