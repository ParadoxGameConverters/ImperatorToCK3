using commonItems;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Culture;

public class CultureMapper {
	public CultureMapper(ImperatorRegionMapper imperatorRegionMapper, CK3RegionMapper ck3RegionMapper) {
		this.imperatorRegionMapper = imperatorRegionMapper;
		this.ck3RegionMapper = ck3RegionMapper;

		Logger.Info("Parsing culture mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile("configurables/culture_map.txt");
		Logger.Info($"Loaded {cultureMappingRules.Count} cultural links.");

		Logger.IncrementProgress();
	}
	public CultureMapper(BufferedReader reader, ImperatorRegionMapper imperatorRegionMapper, CK3RegionMapper ck3RegionMapper) {
		this.imperatorRegionMapper = imperatorRegionMapper;
		this.ck3RegionMapper = ck3RegionMapper;

		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => cultureMappingRules.Add(CultureMappingRule.Parse(reader)));
		parser.IgnoreAndLogUnregisteredItems();
	}
	public string? Match(
		string irCulture,
		string ck3Religion,
		ulong ck3ProvinceId,
		ulong irProvinceId,
		string historicalTag
	) {
		foreach (var cultureMappingRule in cultureMappingRules) {
			var possibleMatch = cultureMappingRule.Match(irCulture, ck3Religion, ck3ProvinceId, irProvinceId, historicalTag, imperatorRegionMapper, ck3RegionMapper);
			if (possibleMatch is not null) {
				return possibleMatch;
			}
		}
		return null;
	}

	public string? NonReligiousMatch(
		string irCulture,
		string ck3Religion,
		ulong ck3ProvinceId,
		ulong irProvinceId,
		string historicalTag
	) {
		foreach (var cultureMappingRule in cultureMappingRules) {
			var possibleMatch = cultureMappingRule.NonReligiousMatch(irCulture, ck3Religion, ck3ProvinceId, irProvinceId, historicalTag, imperatorRegionMapper, ck3RegionMapper);
			if (possibleMatch is not null) {
				return possibleMatch;
			}
		}
		return null;
	}

	private readonly List<CultureMappingRule> cultureMappingRules = new();
	private readonly ImperatorRegionMapper imperatorRegionMapper;
	private readonly CK3RegionMapper ck3RegionMapper;
}