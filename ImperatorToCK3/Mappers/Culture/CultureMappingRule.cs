using commonItems;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Culture;

public sealed class CultureMappingRule {
	public static CultureMappingRule Parse(BufferedReader reader) {
		mappingToReturn = new CultureMappingRule();
		parser.ParseStream(reader);
		return mappingToReturn;
	}

	public string? Match(
		string irCulture,
		ulong? ck3ProvinceId,
		ulong? irProvinceId,
		string? historicalTag,
		ImperatorRegionMapper irRegionMapper,
		CK3RegionMapper ck3RegionMapper
	) {
		// We need at least a viable irCulture.
		if (string.IsNullOrEmpty(irCulture)) {
			return null;
		}

		if (!cultures.Contains(irCulture)) {
			return null;
		}

		if (irHistoricalTags.Count > 0) {
			if (string.IsNullOrEmpty(historicalTag) || !irHistoricalTags.Contains(historicalTag)) {
				return null;
			}
		}

		// simple culture-culture match
		if (ck3Provinces.Count == 0 && irProvinces.Count == 0 && ck3Regions.Count == 0 && irRegions.Count == 0) {
			return destinationCulture;
		}

		if (ck3ProvinceId is null && irProvinceId is null) {
			return null;
		}

		// This is a CK3 provinces check
		if (ck3ProvinceId is not null) {
			if (ck3Provinces.Contains(ck3ProvinceId.Value)) {
				return destinationCulture;
			}
			// This is a CK3 regions check, it checks if provided ck3ProvinceId is within the mapping's ck3Regions.
			foreach (var region in ck3Regions) {
				if (!ck3RegionMapper.RegionNameIsValid(region)) {
					Logger.Warn($"Checking for culture {irCulture} inside invalid CK3 region: {region}! Fix the mapping rules!");
					// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
					// for the converter to explode across the logs with invalid names. So, continue.
					continue;
				}
				if (ck3RegionMapper.ProvinceIsInRegion(ck3ProvinceId.Value, region)) {
					return destinationCulture;
				}
			}
		}

		// This is an Imperator provinces check.
		if (irProvinceId is not null) {
			if (irProvinces.Contains(irProvinceId.Value)) {
				return destinationCulture;
			}
			// This is an Imperator regions check, it checks if provided irProvinceId is within the mapping's irRegions.
			foreach (var region in irRegions) {
				if (!irRegionMapper.RegionNameIsValid(region)) {
					continue;
				}
				if (irRegionMapper.ProvinceIsInRegion(irProvinceId.Value, region)) {
					return destinationCulture;
				}
			}
		}

		return null;
	}
	
	public string CK3CultureId => destinationCulture;

	private string destinationCulture = string.Empty;
	private readonly SortedSet<string> cultures = new();
	private readonly SortedSet<string> irHistoricalTags = new();
	private readonly SortedSet<ulong> irProvinces = new();
	private readonly SortedSet<ulong> ck3Provinces = new();
	private readonly SortedSet<string> irRegions = new();
	private readonly SortedSet<string> ck3Regions = new();

	static CultureMappingRule() {
		parser.RegisterKeyword("ck3", reader => mappingToReturn.destinationCulture = reader.GetString());
		parser.RegisterKeyword("ir", reader => mappingToReturn.cultures.Add(reader.GetString()));
		parser.RegisterKeyword("historicalTag", reader => mappingToReturn.irHistoricalTags.Add(reader.GetString()));
		parser.RegisterKeyword("ck3Region", reader => mappingToReturn.ck3Regions.Add(reader.GetString()));
		parser.RegisterKeyword("irRegion", reader => mappingToReturn.irRegions.Add(reader.GetString()));
		parser.RegisterKeyword("ck3Province", reader => mappingToReturn.ck3Provinces.Add(reader.GetULong()));
		parser.RegisterKeyword("irProvince", reader => mappingToReturn.irProvinces.Add(reader.GetULong()));
		parser.RegisterRegex(CommonRegexes.Variable, (reader, variableName) => {
			var variableValue = reader.ResolveVariable(variableName)?.ToString() ?? string.Empty;
			var variableReader = new BufferedReader(variableValue);
			variableReader.CopyVariables(reader);
			parser.ParseStream(variableReader);
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	private static readonly Parser parser = new();
	private static CultureMappingRule mappingToReturn = new();
}