using commonItems;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Religion;

internal sealed class ReligionMapping {
	private readonly SortedSet<string> irReligionIds = [];
	public IReadOnlySet<string> IrReligionIds => irReligionIds;
	public string? CK3FaithId { get; private set; }
	private readonly SortedSet<string> ck3CultureIds = [];

	private readonly SortedSet<ulong> irProvinceIds = [];
	private readonly SortedSet<ulong> ck3Provinces = [];

	private readonly SortedSet<string> imperatorRegions = [];
	private readonly SortedSet<string> ck3Regions = [];

	private Date? dateGreaterOrEqual = null;

	private readonly SortedSet<string> irHistoricalTags = [];

	private bool? heresiesInHistoricalAreas;

	private static readonly Parser parser = new(implicitVariableHandling: false);
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
		if (!BasicCriteriaMatch(irReligion, irHistoricalTag, config) || !CultureMatches(ck3CultureId)) {
			return null;
		}

		// Simple religion-religion match.
		if (ck3Provinces.Count == 0 && irProvinceIds.Count == 0 && ck3Regions.Count == 0 && imperatorRegions.Count == 0) {
			return CK3FaithId;
		}

		if (ck3ProvinceId is null && irProvinceId is null) {
			return null;
		}

		if (Ck3ProvinceOrRegionMatches(irReligion, ck3ProvinceId, ck3RegionMapper)) {
			return CK3FaithId;
		}

		if (ImperatorProvinceOrRegionMatches(irProvinceId, imperatorRegionMapper)) {
			return CK3FaithId;
		}
		
		return null;
	}

	private bool BasicCriteriaMatch(string irReligion, string? irHistoricalTag, Configuration config) {
		if (string.IsNullOrEmpty(irReligion) || !irReligionIds.Contains(irReligion)) {
			return false;
		}

		if (dateGreaterOrEqual is not null && config.CK3BookmarkDate < dateGreaterOrEqual) {
			return false;
		}

		if (heresiesInHistoricalAreas is not null &&
		    config.HeresiesInHistoricalAreas != heresiesInHistoricalAreas.Value) {
			return false;
		}

		if (irHistoricalTags.Count > 0 && (string.IsNullOrEmpty(irHistoricalTag) || !irHistoricalTags.Contains(irHistoricalTag))) {
			return false;
		}

		return true;
	}

	private bool CultureMatches(string? ck3CultureId) {
		// If a mapping expects one of a given set of CK3 cultures, the provided one must match them.
		if (ck3CultureIds.Count == 0) {
			return true;
		}

		return !string.IsNullOrEmpty(ck3CultureId) && ck3CultureIds.Contains(ck3CultureId);
	}

	private bool Ck3ProvinceOrRegionMatches(string irReligion, ulong? ck3ProvinceId, CK3RegionMapper ck3RegionMapper) {
		if (ck3ProvinceId is null) {
			return false;
		}

		// This is a CK3 provinces check.
		if (ck3Provinces.Contains(ck3ProvinceId.Value)) {
			return true;
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
				return true;
			}
		}

		return false;
	}

	private bool ImperatorProvinceOrRegionMatches(ulong? irProvinceId, ImperatorRegionMapper imperatorRegionMapper) {
		if (irProvinceId is null) {
			return false;
		}

		// This is an Imperator provinces check.
		if (irProvinceIds.Contains(irProvinceId.Value)) {
			return true;
		}

		// This is an Imperator regions check, it checks if provided irProvinceId is within the mapping's imperatorRegions.
		foreach (var region in imperatorRegions) {
			if (!imperatorRegionMapper.RegionNameIsValid(region)) {
				continue;
			}
			if (imperatorRegionMapper.ProvinceIsInRegion(irProvinceId.Value, region)) {
				return true;
			}
		}

		return false;
	}
}