using commonItems;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Culture {
	public class CultureMappingRule {
		public static CultureMappingRule Parse(BufferedReader reader) {
			mappingToReturn = new CultureMappingRule();
			parser.ParseStream(reader);
			return mappingToReturn;
		}

		public ImperatorRegionMapper? ImperatorRegionMapper { private get; set; }
		public CK3RegionMapper? CK3RegionMapper { private get; set; }

		public string? Match(
			string impCulture,
			string ck3Religion,
			ulong ck3ProvinceId,
			ulong impProvinceId,
			string ck3OwnerTitle
		) {
			// We need at least a viable impCulture.
			if (string.IsNullOrEmpty(impCulture)) {
				return null;
			}

			if (!cultures.Contains(impCulture)) {
				return null;
			}

			if (owners.Count > 0) {
				if (string.IsNullOrEmpty(ck3OwnerTitle) || !owners.Contains(ck3OwnerTitle)) {
					return null;
				}
			}

			if (religions.Count > 0) {
				if (string.IsNullOrEmpty(ck3Religion) || !religions.Contains(ck3Religion)) { // (CK3 religion empty) or (CK3 religion not empty but not found in religions)
					return null;
				}
			}

			// simple culture-culture match
			if (ck3Provinces.Count == 0 && imperatorProvinces.Count == 0 && ck3Regions.Count == 0 && imperatorRegions.Count == 0) {
				return destinationCulture;
			}

			if (ck3ProvinceId == 0 && impProvinceId == 0) {
				return null;
			}

			// This is a CK3 provinces check
			if (ck3Provinces.Contains(ck3ProvinceId)) {
				return destinationCulture;
			}
			// This is a CK3 regions check, it checks if provided ck3Province is within the mapping's ck3Regions
			foreach (var region in ck3Regions) {
				if (!CK3RegionMapper.RegionNameIsValid(region)) {
					Logger.Warn($"Checking for culture {impCulture} inside invalid CK3 region: {region}! Fix the mapping rules!");
					// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
					// for the converter to explode across the logs with invalid names. So, continue.
					continue;
				}
				if (CK3RegionMapper.ProvinceIsInRegion(ck3ProvinceId, region)) {
					return destinationCulture;
				}
			}

			// This is an Imperator provinces check
			if (imperatorProvinces.Contains(impProvinceId)) {
				return destinationCulture;
			}
			// This is an Imperator regions check, it checks if provided impProvince is within the mapping's imperatorRegions
			foreach (var region in imperatorRegions) {
				if (!ImperatorRegionMapper.RegionNameIsValid(region)) {
					Logger.Warn($"Checking for religion {impCulture} inside invalid Imperator region: {region}! Fix the mapping rules!");
					// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
					// for the converter to explode across the logs with invalid names. So, continue.
					continue;
				}
				if (ImperatorRegionMapper.ProvinceIsInRegion(impProvinceId, region)) {
					return destinationCulture;
				}
			}

			return null;
		}
		public string? NonReligiousMatch(
			string impCulture,
			string ck3Religion,
			ulong ck3ProvinceId,
			ulong impProvinceId,
			string ck3OwnerTitle
		) {
			// This is a non religious match. We need a mapping without any religion, so if the
			// mapping rule has any religious qualifiers it needs to fail.
			if (religions.Count > 0) {
				return null;
			}

			// Otherwise, as usual.
			return Match(impCulture, ck3Religion, ck3ProvinceId, impProvinceId, ck3OwnerTitle);
		}

		private string destinationCulture = string.Empty;
		private readonly SortedSet<string> cultures = new();
		private readonly SortedSet<string> religions = new();
		private readonly SortedSet<string> owners = new();
		private readonly SortedSet<ulong> imperatorProvinces = new();
		private readonly SortedSet<ulong> ck3Provinces = new();
		private readonly SortedSet<string> imperatorRegions = new();
		private readonly SortedSet<string> ck3Regions = new();

		static CultureMappingRule() {
			parser.RegisterKeyword("ck3", reader => {
				mappingToReturn.destinationCulture = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("imp", reader => {
				mappingToReturn.cultures.Add(ParserHelpers.GetString(reader));
			});
			parser.RegisterKeyword("religion", reader => {
				mappingToReturn.religions.Add(ParserHelpers.GetString(reader));
			});
			parser.RegisterKeyword("owner", reader => {
				mappingToReturn.owners.Add(ParserHelpers.GetString(reader));
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
		private static readonly Parser parser = new();
		private static CultureMappingRule mappingToReturn = new();
	}
}
