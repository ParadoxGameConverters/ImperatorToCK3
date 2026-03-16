using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.Mappers.Region;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.Religion;

internal sealed class ReligionMapper {
	public ReligionMapper(ReligionCollection ck3Religions, ImperatorRegionMapper imperatorRegionMapper, CK3RegionMapper ck3RegionMapper) {
		this.imperatorRegionMapper = imperatorRegionMapper;
		this.ck3RegionMapper = ck3RegionMapper;

		Logger.Info("Parsing religion mappings...");

		var parser = new Parser();
		RegisterKeys(parser);
		var mappingsPath = Path.Combine("configurables", "religion_map.txt");
		parser.ParseFile(mappingsPath);

		Logger.Info($"Loaded {religionMappings.Count} religious links.");

		RemoveMappingsWithNonexistentCK3Faiths(ck3Religions);
		BuildLookup();

		Logger.IncrementProgress();
	}
	public ReligionMapper(BufferedReader reader, ReligionCollection ck3Religions, ImperatorRegionMapper imperatorRegionMapper, CK3RegionMapper ck3RegionMapper) {
		this.imperatorRegionMapper = imperatorRegionMapper;
		this.ck3RegionMapper = ck3RegionMapper;

		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);

		RemoveMappingsWithNonexistentCK3Faiths(ck3Religions);
		BuildLookup();
	}

	private void RemoveMappingsWithNonexistentCK3Faiths(ReligionCollection ck3Religions) {
		religionMappings.RemoveWhere(m=>m.CK3FaithId is not null && ck3Religions.GetFaith(m.CK3FaithId) is null);
	}

	private void BuildLookup() {
		religionLookup.Clear();
		foreach (var mapping in religionMappings) {
			foreach (var irReligionId in mapping.IrReligionIds) {
				if (!religionLookup.TryGetValue(irReligionId, out var list)) {
					list = new List<ReligionMapping>();
					religionLookup[irReligionId] = list;
				}
				list.Add(mapping);
			}
		}
	}

	public string? Match(string irReligionId, string? ck3CultureId, ulong? ck3ProvinceId, ulong? irProvinceId, string? irHistoricalTag, Configuration config) {
		if (string.IsNullOrEmpty(irReligionId)) {
			return null;
		}

		if (!religionLookup.TryGetValue(irReligionId, out var candidates)) {
			return null;
		}

		foreach (var religionMapping in candidates) {
			var possibleMatch = religionMapping.Match(irReligionId, ck3CultureId, ck3ProvinceId, irProvinceId, irHistoricalTag, config, imperatorRegionMapper, ck3RegionMapper);
			if (possibleMatch is not null) {
				return possibleMatch;
			}
		}
		return null;
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			religionMappings.Add(ReligionMapping.Parse(reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	private readonly List<ReligionMapping> religionMappings = new();
	private readonly Dictionary<string, List<ReligionMapping>> religionLookup = new(StringComparer.Ordinal);
	private readonly ImperatorRegionMapper imperatorRegionMapper;
	private readonly CK3RegionMapper ck3RegionMapper;
}