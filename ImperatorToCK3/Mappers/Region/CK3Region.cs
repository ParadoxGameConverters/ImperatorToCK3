using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Collections.Generic;
using System.Text;
using ZLinq;

namespace ImperatorToCK3.Mappers.Region;

internal sealed class CK3Region {
	public string Name { get; }
	private readonly HashSet<string> parsedRegionIds = [];
	public Dictionary<string, CK3Region> Regions { get; } = [];
	private readonly HashSet<string> parsedDuchyIds = [];
	private readonly HashSet<string> parsedKingdomIds = [];
	public Dictionary<string, Title> Duchies { get; } = [];
	private readonly HashSet<string> parsedCountyIds = [];
	public Dictionary<string, Title> Counties { get; } = [];
	public SortedSet<ulong> Provinces { get; } = [];
	private readonly List<KeyValuePair<string, StringOfItem>> attributes = [];

	public CK3Region(string name) => Name = name;

	public void LinkRegions(Dictionary<string, CK3Region> regions, Dictionary<string, Title> kingdoms, Dictionary<string, Title> duchies, Dictionary<string, Title> counties) {
		// regions
		foreach (var requiredRegionName in parsedRegionIds) {
			if (regions.TryGetValue(requiredRegionName, out var regionToLink)) {
				LinkRegion(regionToLink);
			} else {
				Logger.Warn($"Region's {Name} region {requiredRegionName} does not exist!");
			}
		}

		// kingdoms
		foreach (var requiredKingdomName in parsedKingdomIds) {
			// We can't just keep the kingdom because the converter changes the de jure kingdoms setup.
			// So get all the de jure vassals of the kingdom and link them instead.
			if (kingdoms.TryGetValue(requiredKingdomName, out var kingdom)) {
				foreach (var deJureVassal in kingdom.DeJureVassals) {
					if (deJureVassal.Rank == TitleRank.duchy) {
						LinkDuchy(deJureVassal);
					} else if (deJureVassal.Rank == TitleRank.county) {
						LinkCounty(deJureVassal);
					} else if (deJureVassal is {Rank: TitleRank.barony, ProvinceId: not null}) {
						Provinces.Add(deJureVassal.ProvinceId.Value);
					}
				}
			} else {
				Logger.Warn($"Region's {Name} kingdom {requiredKingdomName} does not exist!");
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
		parser.RegisterKeyword("kingdoms", reader => {
			foreach (var id in reader.GetStrings()) {
				regionToReturn.parsedKingdomIds.Add(id);
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
		parser.RegisterRegex(CommonRegexes.String, (reader, keyword) => {
			regionToReturn.attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
	}
	public static CK3Region Parse(string name, BufferedReader reader) {
		regionToReturn = new CK3Region(name);
		parser.ParseStream(reader);
		return regionToReturn;
	}

	public override string ToString() {
		var sb = new StringBuilder();
		sb.Append(Name).AppendLine(" = {");
		if (Regions.Count > 0) {
			sb.Append("\tregions = { ");
			foreach (string regionId in Regions.Keys) {
				sb.Append(regionId).Append(' ');
			}
			sb.AppendLine("}");
		}

		if (Duchies.Count > 0) {
			sb.Append("\tduchies = { ");
			foreach (var duchyId in Duchies.Values) {
				sb.Append(duchyId).Append(' ');
			}
			sb.AppendLine("}");
		}

		if (Counties.Count > 0) {
			sb.Append("\tcounties = { ");
			foreach (var countyId in Counties.Values) {
				sb.Append(countyId).Append(' ');
			}
			sb.AppendLine("}");
		}

		if (Provinces.Count > 0) {
			sb.Append("\tprovinces = { ");
			foreach (var provinceId in Provinces) {
				sb.Append(provinceId).Append(' ');
			}
			sb.AppendLine("}");
		}

		if (attributes.Count > 0) {
			sb.AppendLine(PDXSerializer.Serialize(attributes, indent: "\t", withBraces: false));
		}

		sb.Append('}');
		return sb.ToString();
	}
}