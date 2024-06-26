using commonItems;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Religion;

public sealed class ReligionMapping {
	private readonly SortedSet<string> irReligionIds = [];
	public string? CK3FaithId { get; private set; }
	private readonly SortedSet<string> ck3CultureIds = [];

	private readonly SortedSet<ulong> irProvinceIds = [];
	private readonly SortedSet<ulong> ck3Provinces = [];

	private readonly SortedSet<string> imperatorRegions = [];
	private readonly SortedSet<string> ck3Regions = [];

	private Date? dateGreaterOrEqual = null;

	private readonly SortedSet<string> irHistoricalTags = [];

	private bool? heresiesInHistoricalAreas;

	private static readonly Parser parser = new();
	private static ReligionMapping mappingToReturn = new();
	static ReligionMapping() {
		parser.RegisterKeyword("ck3", reader => mappingToReturn.CK3FaithId = reader.GetString());
		parser.RegisterKeyword("ir", reader => mappingToReturn.irReligionIds.Add(reader.GetString()));
		parser.RegisterKeyword("ck3Culture", reader => mappingToReturn.ck3CultureIds.Add(reader.GetString()));
		parser.RegisterKeyword("ck3Region", reader => mappingToReturn.ck3Regions.Add(reader.GetString()));
		parser.RegisterKeyword("irRegion", reader => mappingToReturn.imperatorRegions.Add(reader.GetString()));
		parser.RegisterKeyword("ck3Province", reader => mappingToReturn.ck3Provinces.Add(reader.GetULong()));
		parser.RegisterKeyword("irProvince", reader => mappingToReturn.irProvinceIds.Add(reader.GetULong()));
		parser.RegisterKeyword("date_gte", reader => mappingToReturn.dateGreaterOrEqual = new Date(reader.GetString()));
		parser.RegisterKeyword("historicalTag", reader => mappingToReturn.irHistoricalTags.Add(reader.GetString()));
		parser.RegisterKeyword("heresiesInHistoricalAreas", reader => mappingToReturn.heresiesInHistoricalAreas = reader.GetBool());
		parser.RegisterRegex(CommonRegexes.Variable, (reader, variableName) => {
			var variableValue = reader.ResolveVariable(variableName)?.ToString() ?? string.Empty;
			var variableReader = new BufferedReader(variableValue);
			variableReader.CopyVariables(reader);
			parser.ParseStream(variableReader);
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	public static ReligionMapping Parse(BufferedReader reader) {
		mappingToReturn = new ReligionMapping();
		parser.ParseStream(reader);
		return mappingToReturn;
	}

	public string? Match(
		string irReligion,
		string? ck3CultureId,
		ulong? ck3ProvinceId,
		ulong? irProvinceId,
		string? irHistoricalTag,
		Configuration config,
		ImperatorRegionMapper imperatorRegionMapper,
		CK3RegionMapper ck3RegionMapper) {
		// We need at least a viable Imperator religion.
		if (string.IsNullOrEmpty(irReligion)) {
			return null;
		}

		if (!irReligionIds.Contains(irReligion)) {
			return null;
		}
		
		if (dateGreaterOrEqual is not null && config.CK3BookmarkDate < dateGreaterOrEqual) {
			return null;
		}

		if (heresiesInHistoricalAreas is not null &&
		    config.HeresiesInHistoricalAreas != heresiesInHistoricalAreas.Value) {
			return null;
		}

		if (irHistoricalTags.Count > 0) {
			if (string.IsNullOrEmpty(irHistoricalTag) || !irHistoricalTags.Contains(irHistoricalTag)) {
				return null;
			}
		}

		// If a mapping expects one of a given set of CK3 cultures, the provided one must match them.
		if (ck3CultureIds.Count > 0) {
			if (string.IsNullOrEmpty(ck3CultureId)) {
				return null;
			}
			if (!ck3CultureIds.Contains(ck3CultureId)) {
				return null;
			}
		}

		// Simple religion-religion match.
		if (ck3Provinces.Count == 0 && irProvinceIds.Count == 0 && ck3Regions.Count == 0 && imperatorRegions.Count == 0) {
			return CK3FaithId;
		}

		if (ck3ProvinceId is null && irProvinceId is null) {
			return null;
		}

		// This is a CK3 provinces check.
		if (ck3ProvinceId is not null) {
			if (ck3Provinces.Contains(ck3ProvinceId.Value)) {
				return CK3FaithId;
			}
			// This is a CK3 regions check, it checks if provided ck3Province is within the mapping's ck3Regions.
			foreach (var region in ck3Regions) {
				if (!ck3RegionMapper.RegionNameIsValid(region)) {
					Logger.Warn($"Checking for religion {irReligion} inside invalid CK3 region: {region}! Fix the mapping rules!");
					// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
					// for the converter to explode across the logs with invalid names. So, continue.
					continue;
				}
				if (ck3RegionMapper.ProvinceIsInRegion(ck3ProvinceId.Value, region)) {
					return CK3FaithId;
				}
			}
		}

		// This is an Imperator provinces check.
		if (irProvinceId is not null) {
			if (irProvinceIds.Contains(irProvinceId.Value)) {
				return CK3FaithId;
			}
			// This is an Imperator regions check, it checks if provided irProvinceId is within the mapping's imperatorRegions.
			foreach (var region in imperatorRegions) {
				if (!imperatorRegionMapper.RegionNameIsValid(region)) {
					continue;
				}
				if (imperatorRegionMapper.ProvinceIsInRegion(irProvinceId.Value, region)) {
					return CK3FaithId;
				}
			}
		}
		
		return null;
	}
}