using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Helpers;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using Open.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.TagTitle;

public class TagTitleMapper {
	public TagTitleMapper() { }
	public TagTitleMapper(string tagTitleMappingsPath, string governorshipTitleMappingsPath, string rankMappingsPath) {
		Logger.Info("Parsing title mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(tagTitleMappingsPath);
		parser.ParseFile(governorshipTitleMappingsPath);
		Logger.Info($"{titleMappings.Count} title mappings loaded.");
		
		LoadRankMappings(rankMappingsPath);

		Logger.IncrementProgress();
	}
	public void RegisterCountry(ulong countryId, string ck3Title) {
		registeredCountryTitles.Add(countryId, ck3Title);
		usedTitles.Add(ck3Title);
	}
	public void RegisterGovernorship(string imperatorRegion, string imperatorCountryTag, string ck3Title) {
		registeredGovernorshipTitles.Add($"{imperatorCountryTag}_{imperatorRegion}", ck3Title);
		usedTitles.Add(ck3Title);
	}
	public string? GetTitleForTag(Country country, string localizedTitleName, TitleRank maxTitleRank) {
		// If country has an origin (e.g. rebelled from another country), the historical tag probably points to the original country.
		string tagForMapping = country.OriginCountry is not null ? country.Tag : country.HistoricalTag;

		// The only case where we fail is on invalid invocation. Otherwise, failure is not an option!
		if (string.IsNullOrEmpty(tagForMapping)) {
			return null;
		}

		// Look up register.
		if (registeredCountryTitles.TryGetValue(country.Id, out var titleToReturn)) {
			return titleToReturn;
		}

		// Attempt a title match.
		var rank = EnumHelper.Min(GetCK3TitleRank(country, localizedTitleName), maxTitleRank);
		foreach (var mapping in titleMappings) {
			var match = mapping.RankMatch(tagForMapping, rank, maxTitleRank);
			if (match is not null) {
				if (usedTitles.Contains(match)) {
					continue;
				}

				RegisterCountry(country.Id, match);
				return match;
			}
		}

		// Generate a new title ID.
		var generatedTitleId = GenerateNewTitleId(country, localizedTitleName, maxTitleRank);
		RegisterCountry(country.Id, generatedTitleId);
		return generatedTitleId;
	}
	public string? GetTitleForTag(Country country) {
		return GetTitleForTag(country, localizedTitleName: string.Empty, maxTitleRank: TitleRank.empire);
	}

	public string? GetTitleForSubject(Country subject, string localizedTitleName, Country overlord) {
		TitleRank maxTitleRank;
		var ck3OverlordTitle = overlord.CK3Title;
		if (ck3OverlordTitle is null) {
			Logger.Warn($"Country {overlord.Tag} has no associated CK3 title!");
			maxTitleRank = TitleRank.empire; // If overlord doesn't exist in CK3, allow the subject to be independent.
		} else {
			maxTitleRank = ck3OverlordTitle.Rank - 1;
		}
		
		return GetTitleForTag(subject, localizedTitleName, maxTitleRank);
	}
	
	public string? GetTitleForGovernorship(Governorship governorship, Title.LandedTitles titles, Imperator.Provinces.ProvinceCollection irProvinces, ProvinceCollection ck3Provinces, ImperatorRegionMapper imperatorRegionMapper, ProvinceMapper provMapper) {
		var country = governorship.Country;
		if (country.CK3Title is null) {
			Logger.Warn($"Country {country.Tag} has no associated CK3 title!");
			return null;
		}
		var ck3LiegeTitle = country.CK3Title.Id;

		var rank = GetCK3GovernorshipRank(ck3LiegeTitle);

		// Look up register
		if (registeredGovernorshipTitles.TryGetValue($"{country.Tag}_{governorship.Region.Id}", out var titleToReturn)) {
			return titleToReturn;
		}

		if (rank == TitleRank.county) {
			return GetCountyForGovernorship(governorship, country, titles, ck3Provinces, imperatorRegionMapper);
		}

		// Attempt a title match
		foreach (var mapping in titleMappings) {
			var match = mapping.GovernorshipMatch(rank, titles, governorship, provMapper, irProvinces);
			if (match is null) {
				continue;
			}

			if (usedTitles.Contains(match)) {
				continue;
			}
			RegisterGovernorship(governorship.Region.Id, country.Tag, match);
			return match;
		}

		// Generate a new title
		var generatedTitle = GenerateNewTitleId(governorship.Region.Id, country.Tag, ck3LiegeTitle);
		RegisterGovernorship(governorship.Region.Id, country.Tag, generatedTitle);
		return generatedTitle;
	}

	private string? GetCountyForGovernorship(Governorship governorship, Country country, Title.LandedTitles titles, ProvinceCollection ck3Provinces, ImperatorRegionMapper imperatorRegionMapper) {
		var ck3Country = country.CK3Title;
		if (ck3Country is null) {
			return null;
		}
		
		var ck3CapitalCounty = ck3Country.CapitalCounty;
		if (ck3CapitalCounty is null) {
			Logger.Warn($"{ck3Country.Id} has no capital county!");
			return null;
		}
		
		var countryCapitalDuchy = ck3CapitalCounty.DeJureLiege;
		
		foreach (var county in titles.Where(t => t.Rank == TitleRank.county)) {
			if (!county.CapitalBaronyProvinceId.HasValue) {
				// Title has no capital barony province.
				continue;
			}
			ulong capitalBaronyProvinceId = county.CapitalBaronyProvinceId.Value;
			if (capitalBaronyProvinceId == 0) {
				// Title's capital province has an invalid ID (0 is not a valid province in CK3)
				continue;
			}

			if (!ck3Provinces.ContainsKey(capitalBaronyProvinceId)) {
				Logger.Warn($"Capital barony province not found: {capitalBaronyProvinceId}");
				continue;
			}

			var ck3CapitalBaronyProvince = ck3Provinces[capitalBaronyProvinceId];
			var irProvince = ck3CapitalBaronyProvince.PrimaryImperatorProvince;
			if (irProvince is null) { // probably outside of Imperator map
				continue;
			}
			
			// if title belongs to country ruler's capital's de jure duchy, it needs to be directly held by the ruler
			var deJureDuchyOfCounty = county.DeJureLiege;
			if (countryCapitalDuchy is not null && deJureDuchyOfCounty is not null && countryCapitalDuchy.Id == deJureDuchyOfCounty.Id) {
				continue;
			}
			
			if (governorship.Region.Id != imperatorRegionMapper.GetParentRegionName(irProvince.Id)) {
				continue;
			}

			RegisterGovernorship(governorship.Region.Id, country.Tag, county.Id);
			return county.Id;
		}

		return null;
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => titleMappings.Add(TitleMapping.Parse(reader)));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	
	private void LoadRankMappings(string rankMappingsPath) {
		Logger.Info("Parsing country rank mappings...");
		var parser = new Parser();
		parser.RegisterKeyword("empire_keywords", reader => empireKeywords.AddRange(reader.GetStrings()));
		parser.RegisterKeyword("kingdom_keywords", reader => kingdomKeywords.AddRange(reader.GetStrings()));
		parser.RegisterKeyword("duchy_keywords", reader => duchyKeywords.AddRange(reader.GetStrings()));
		parser.RegisterKeyword("link", reader => rankMappings.Add(new RankMapping(reader)));

		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(rankMappingsPath);
		Logger.Info($"{rankMappings.Count} rank mappings loaded.");
	}
	
	private TitleRank GetCK3TitleRank(Country country, string localizedTitleName) {
		// Split the name into words.
		var words = localizedTitleName.Split(' ');
		
		if (empireKeywords.Any(kw => words.Contains(kw, StringComparer.OrdinalIgnoreCase))) {
			return TitleRank.empire;
		}
		if (kingdomKeywords.Any(kw => words.Contains(kw, StringComparer.OrdinalIgnoreCase))) {
			return TitleRank.kingdom;
		}
		if (duchyKeywords.Any(kw => words.Contains(kw, StringComparer.OrdinalIgnoreCase))) {
			return TitleRank.duchy;
		}

		var countryRankStr = country.Rank switch {
			CountryRank.migrantHorde => "migrant_horde",
			CountryRank.cityState => "city_power",
			CountryRank.localPower => "local_power",
			CountryRank.regionalPower => "regional_power",
			CountryRank.majorPower => "major_power",
			CountryRank.greatPower => "great_power",
			_ => throw new ArgumentOutOfRangeException($"Invalid country rank: {country.Rank}!")
		};

		foreach (var mapping in rankMappings) {
			var match = mapping.Match(countryRankStr, country.TerritoriesCount);
			if (match is not null) {
				return match.Value;
			}
		}
		
		Logger.Warn($"No rank mapping found for country rank: {countryRankStr} with {country.TerritoriesCount} territories! Defaulting to duchy.");
		return TitleRank.duchy;
	}
	private static TitleRank GetCK3GovernorshipRank(string ck3LiegeTitleId) {
		var ck3LiegeRank = Title.GetRankForId(ck3LiegeTitleId);

		return ck3LiegeRank switch {
			TitleRank.empire => TitleRank.kingdom,
			TitleRank.kingdom => TitleRank.duchy,
			TitleRank.duchy => TitleRank.county,
			_ => throw new ArgumentException($"Title {ck3LiegeTitleId} has invalid rank to have governorships!", nameof(ck3LiegeTitleId))
		};
	}
	private string GenerateNewTitleId(Country country, string localizedTitleName, TitleRank maxTitleRank) {
		var ck3Rank = EnumHelper.Min(GetCK3TitleRank(country, localizedTitleName), maxTitleRank);
		
		var ck3TitleId = GetTitlePrefixForRank(ck3Rank);
		ck3TitleId += GeneratedCK3TitlePrefix;
		ck3TitleId += country.Tag;

		return ck3TitleId;
	}
	private static string GenerateNewTitleId(string imperatorRegion, string imperatorCountryTag, string ck3LiegeTitle) {
		var ck3Rank = GetCK3GovernorshipRank(ck3LiegeTitle);

		var ck3TitleId = GetTitlePrefixForRank(ck3Rank);
		ck3TitleId += GeneratedCK3TitlePrefix;
		ck3TitleId += imperatorCountryTag;
		ck3TitleId += "_";
		ck3TitleId += imperatorRegion;

		if (ck3Rank < TitleRank.duchy) {
			Logger.Warn($"Governorship title rank is too low: {ck3TitleId}!");
		}

		return ck3TitleId;
	}

	private static string GetTitlePrefixForRank(TitleRank titleRank) {
		return titleRank switch {
			TitleRank.empire => "e_",
			TitleRank.kingdom => "k_",
			TitleRank.duchy => "d_",
			TitleRank.county => "c_",
			TitleRank.barony => "b_",
			_ => throw new ArgumentOutOfRangeException(nameof(titleRank))
		};
	}

	private readonly List<TitleMapping> titleMappings = new();
	private readonly Dictionary<ulong, string> registeredCountryTitles = new(); // We store already mapped countries here.
	private readonly Dictionary<string, string> registeredGovernorshipTitles = new(); // We store already mapped governorships here.
	private readonly SortedSet<string> usedTitles = new();

	private readonly HashSet<string> empireKeywords = ["empire"];
	private readonly HashSet<string> kingdomKeywords = ["kingdom"];
	private readonly HashSet<string> duchyKeywords = ["duchy"];
	private readonly List<RankMapping> rankMappings = [];

	private const string GeneratedCK3TitlePrefix = "IRTOCK3_";
}