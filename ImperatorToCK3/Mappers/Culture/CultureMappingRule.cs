using commonItems;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Culture;

public class CultureMappingRule {
	public static CultureMappingRule Parse(BufferedReader reader) {
		mappingToReturn = new CultureMappingRule();
		parser.ParseStream(reader);
		return mappingToReturn;
	}

	public string? Match(
		string impCulture,
		string ck3Religion,
		ulong ck3ProvinceId,
		ulong impProvinceId,
		string historicalTag,
		ImperatorRegionMapper imperatorRegionMapper,
		CK3RegionMapper ck3RegionMapper
	) {
		// We need at least a viable impCulture.
		if (string.IsNullOrEmpty(impCulture)) {
			return null;
		}

		if (!cultures.Contains(impCulture)) {
			return null;
		}

		if (tags.Count > 0) {
			if (string.IsNullOrEmpty(historicalTag) || !tags.Contains(historicalTag)) {
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
			if (!ck3RegionMapper.RegionNameIsValid(region)) {
				Logger.Warn($"Checking for culture {impCulture} inside invalid CK3 region: {region}! Fix the mapping rules!");
				// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
				// for the converter to explode across the logs with invalid names. So, continue.
				continue;
			}
			if (ck3RegionMapper.ProvinceIsInRegion(ck3ProvinceId, region)) {
				return destinationCulture;
			}
		}

		// This is an Imperator provinces check
		if (imperatorProvinces.Contains(impProvinceId)) {
			return destinationCulture;
		}
		// This is an Imperator regions check, it checks if provided impProvince is within the mapping's imperatorRegions
		foreach (var region in imperatorRegions) {
			if (!imperatorRegionMapper.RegionNameIsValid(region)) {
				continue;
			}
			if (imperatorRegionMapper.ProvinceIsInRegion(impProvinceId, region)) {
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
		string historicalTag,
		ImperatorRegionMapper imperatorRegionMapper,
		CK3RegionMapper ck3RegionMapper
	) {
		// This is a non religious match. We need a mapping without any religion, so if the
		// mapping rule has any religious qualifiers it needs to fail.
		if (religions.Count > 0) {
			return null;
		}

		// Otherwise, as usual.
		return Match(impCulture, ck3Religion, ck3ProvinceId, impProvinceId, historicalTag, imperatorRegionMapper, ck3RegionMapper);
	}

	private string destinationCulture = string.Empty;
	private readonly SortedSet<string> cultures = new();
	private readonly SortedSet<string> religions = new();
	private readonly SortedSet<string> tags = new();
	private readonly SortedSet<ulong> imperatorProvinces = new();
	private readonly SortedSet<ulong> ck3Provinces = new();
	private readonly SortedSet<string> imperatorRegions = new();
	private readonly SortedSet<string> ck3Regions = new();

	static CultureMappingRule() {
		parser.RegisterKeyword("ck3", reader => mappingToReturn.destinationCulture = reader.GetString());
		parser.RegisterKeyword("imp", reader => mappingToReturn.cultures.Add(reader.GetString()));
		parser.RegisterKeyword("religion", reader => mappingToReturn.religions.Add(reader.GetString()));
		parser.RegisterKeyword("tag", reader => mappingToReturn.tags.Add(reader.GetString()));
		parser.RegisterKeyword("ck3Region", reader => mappingToReturn.ck3Regions.Add(reader.GetString()));
		parser.RegisterKeyword("impRegion", reader => mappingToReturn.imperatorRegions.Add(reader.GetString()));
		parser.RegisterKeyword("ck3Province", reader => mappingToReturn.ck3Provinces.Add(reader.GetULong()));
		parser.RegisterKeyword("impProvince", reader => mappingToReturn.imperatorProvinces.Add(reader.GetULong()));
		parser.RegisterRegex(CommonRegexes.Variable, (reader, variableName) => {
			var variableValue = reader.ResolveVariable(variableName).ToString() ?? string.Empty;
			var variableReader = new BufferedReader(variableValue);
			variableReader.CopyVariables(reader);
			parser.ParseStream(variableReader);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	private static readonly Parser parser = new();
	private static CultureMappingRule mappingToReturn = new();
}