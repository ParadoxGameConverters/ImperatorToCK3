﻿using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.Religion;

public class ReligionMapper {
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
		
		Logger.IncrementProgress();
	}
	public ReligionMapper(BufferedReader reader, ReligionCollection ck3Religions, ImperatorRegionMapper imperatorRegionMapper, CK3RegionMapper ck3RegionMapper) {
		this.imperatorRegionMapper = imperatorRegionMapper;
		this.ck3RegionMapper = ck3RegionMapper;
		
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);

		RemoveMappingsWithNonexistentCK3Faiths(ck3Religions);
	}

	private void RemoveMappingsWithNonexistentCK3Faiths(ReligionCollection ck3Religions) {
		religionMappings.RemoveWhere(m=>m.CK3FaithId is not null && ck3Religions.GetFaith(m.CK3FaithId) is null);
	}
	
	public string? Match(string imperatorReligion, ulong ck3ProvinceId, ulong imperatorProvinceId, Configuration config) {
		foreach (var religionMapping in religionMappings) {
			var possibleMatch = religionMapping.Match(imperatorReligion, ck3ProvinceId, imperatorProvinceId, config, imperatorRegionMapper, ck3RegionMapper);
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
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	private readonly List<ReligionMapping> religionMappings = new();
	private readonly ImperatorRegionMapper imperatorRegionMapper;
	private readonly CK3RegionMapper ck3RegionMapper;
}