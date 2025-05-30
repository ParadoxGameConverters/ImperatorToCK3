﻿using commonItems;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using ZLinq;

namespace ImperatorToCK3.Mappers.Region;

internal sealed class CK3Region {
	public string Name { get; }
	private readonly HashSet<string> parsedRegionIds = new();
	public Dictionary<string, CK3Region> Regions { get; } = new();
	private readonly HashSet<string> parsedDuchyIds = new();
	public Dictionary<string, Title> Duchies { get; } = new();
	private readonly HashSet<string> parsedCountyIds = new();
	public Dictionary<string, Title> Counties { get; } = new();
	public SortedSet<ulong> Provinces { get; } = new();

	public CK3Region(string name) => Name = name;

	public void LinkRegions(Dictionary<string, CK3Region> regions, Dictionary<string, Title> duchies, Dictionary<string, Title> counties) {
		// regions
		foreach (var requiredRegionName in parsedRegionIds) {
			if (regions.TryGetValue(requiredRegionName, out var regionToLink)) {
				LinkRegion(regionToLink);
			} else {
				Logger.Warn($"Region's {Name} region {requiredRegionName} does not exist!");
			}
		}

		// duchies
		foreach (var requiredDuchyName in parsedDuchyIds) {
			if (duchies.TryGetValue(requiredDuchyName, out var duchyToLink)) {
				LinkDuchy(duchyToLink);
			} else {
				Logger.Warn($"Region's {Name} duchy {requiredDuchyName} does not exist!");
			}
		}

		// counties
		foreach (var requiredCountyName in parsedCountyIds) {
			if (counties.TryGetValue(requiredCountyName, out var countyToLink)) {
				LinkCounty(countyToLink);
			} else {
				Logger.Warn($"Region's {Name} county {requiredCountyName} does not exist!");
			}
		}
	}
	public void LinkRegion(CK3Region region) {
		Regions[region.Name] = region;
	}
	public void LinkDuchy(Title theDuchy) {
		Duchies[theDuchy.Id] = theDuchy;
	}
	public void LinkCounty(Title theCounty) {
		Counties[theCounty.Id] = theCounty;
	}
	public bool ContainsProvince(ulong provinceId) {
		if (Regions.Values.AsValueEnumerable().Any(region => region.ContainsProvince(provinceId))) {
			return true;
		}
		if (Duchies.Values.AsValueEnumerable().Any(duchy => duchy.DuchyContainsProvince(provinceId))) {
			return true;
		}
		if (Counties.Values.AsValueEnumerable().Any(county => county.CountyProvinceIds.AsValueEnumerable().Contains(provinceId))) {
			return true;
		}
		return Provinces.Contains(provinceId);
	}

	private static readonly Parser parser = new();
	private static CK3Region regionToReturn = new(string.Empty);
	static CK3Region() {
		parser.RegisterKeyword("regions", reader => {
			foreach (var id in reader.GetStrings()) {
				regionToReturn.parsedRegionIds.Add(id);
			}
		});
		parser.RegisterKeyword("duchies", reader => {
			foreach (var id in reader.GetStrings()) {
				regionToReturn.parsedDuchyIds.Add(id);
			}
		});
		parser.RegisterKeyword("counties", reader => {
			foreach (var id in reader.GetStrings()) {
				regionToReturn.parsedCountyIds.Add(id);
			}
		});
		parser.RegisterKeyword("provinces", reader => {
			foreach (var id in reader.GetULongs()) {
				regionToReturn.Provinces.Add(id);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public static CK3Region Parse(string name, BufferedReader reader) {
		regionToReturn = new CK3Region(name);
		parser.ParseStream(reader);
		return regionToReturn;
	}
}