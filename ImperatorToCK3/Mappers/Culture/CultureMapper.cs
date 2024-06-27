using commonItems;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Culture;

public sealed class CultureMapper {
	public CultureMapper(ImperatorRegionMapper irRegionMapper, CK3RegionMapper ck3RegionMapper, CultureCollection cultures) {
		this.irRegionMapper = irRegionMapper;
		this.ck3RegionMapper = ck3RegionMapper;

		Logger.Info("Parsing culture mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile("configurables/culture_map.txt");
		Logger.Info($"Loaded {cultureMappingRules.Count} cultural links.");
		
		RemoveInvalidRules(cultures);

		Logger.IncrementProgress();
	}
	public CultureMapper(BufferedReader reader, ImperatorRegionMapper irRegionMapper, CK3RegionMapper ck3RegionMapper, CultureCollection cultures) {
		this.irRegionMapper = irRegionMapper;
		this.ck3RegionMapper = ck3RegionMapper;

		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
		
		RemoveInvalidRules(cultures);
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => cultureMappingRules.Add(CultureMappingRule.Parse(reader)));
		parser.IgnoreAndLogUnregisteredItems();
	}
	private void RemoveInvalidRules(CultureCollection cultures) {
		var validCultureIds = cultures.Select(c => c.Id);
		var removedCount = cultureMappingRules.RemoveAll(rule => !validCultureIds.Contains(rule.CK3CultureId));
		
		if (removedCount > 0) {
			Logger.Debug($"{removedCount} culture mapping rules removed due to specified CK3 cultures not existing.");
		}
	}
	
	public string? Match(
		string irCulture,
		ulong? ck3ProvinceId,
		ulong? irProvinceId,
		string? historicalTag
	) {
		foreach (var cultureMappingRule in cultureMappingRules) {
			var possibleMatch = cultureMappingRule.Match(irCulture, ck3ProvinceId, irProvinceId, historicalTag, irRegionMapper, ck3RegionMapper);
			if (possibleMatch is not null) {
				return possibleMatch;
			}
		}
		return null;
	}

	private readonly List<CultureMappingRule> cultureMappingRules = new();
	private readonly ImperatorRegionMapper irRegionMapper;
	private readonly CK3RegionMapper ck3RegionMapper;
}