using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Helpers;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.TagTitle;

public class TagTitleMapper {
	public TagTitleMapper() { }
	public TagTitleMapper(string tagTitleMappingsPath, string governorshipTitleMappingsPath) {
		Logger.Info("Parsing Title mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(tagTitleMappingsPath);
		parser.ParseFile(governorshipTitleMappingsPath);
		Logger.Info($"{mappings.Count} title mappings loaded.");

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
		foreach (var mapping in mappings) {
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
		var generatedTitleId = GenerateNewTitleId(country, localizedTitleName);
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
		foreach (var mapping in mappings) {
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
		foreach (var county in titles.Where(t => t.Rank == TitleRank.county)) {
			ulong capitalBaronyProvinceId = (ulong)county.CapitalBaronyProvinceId!;
			if (capitalBaronyProvinceId == 0) {
				// title's capital province has an invalid ID (0 is not a valid province in CK3)
				continue;
			}

			if (!ck3Provinces.ContainsKey(capitalBaronyProvinceId)) {
				Logger.Warn($"Capital barony province not found: {capitalBaronyProvinceId}");
				continue;
			}

			var ck3CapitalBaronyProvince = ck3Provinces[capitalBaronyProvinceId];
			var impProvince = ck3CapitalBaronyProvince.PrimaryImperatorProvince;
			if (impProvince is null) { // probably outside of Imperator map
				continue;
			}

			var ck3Country = country.CK3Title;
			var ck3CapitalCounty = ck3Country?.CapitalCounty;
			if (ck3CapitalCounty is null) {
				continue;
			}
			// if title belongs to country ruler's capital's de jure duchy, it needs to be directly held by the ruler
			var countryCapitalDuchy = ck3CapitalCounty.DeJureLiege;
			var deJureDuchyOfCounty = county.DeJureLiege;
			if (countryCapitalDuchy is not null && deJureDuchyOfCounty is not null && countryCapitalDuchy.Id == deJureDuchyOfCounty.Id) {
				continue;
			}

			if (governorship.Region.Id != imperatorRegionMapper.GetParentRegionName(impProvince.Id)) {
				continue;
			}

			RegisterGovernorship(governorship.Region.Id, country.Tag, county.Id);
			return county.Id;
		}

		return null;
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => mappings.Add(Mapping.Parse(reader)));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	private static TitleRank GetCK3TitleRank(Country country, string localizedTitleName) {
		if (localizedTitleName.Contains("Empire", StringComparison.Ordinal)) {
			return TitleRank.empire;
		}

		if (localizedTitleName.Contains("Kingdom", StringComparison.Ordinal)) {
			return TitleRank.kingdom;
		}
		
		// Major power rank is very broad (from 100 to 499 territories). Consider 300+ territories as empire material.
		if (country is {Rank: CountryRank.majorPower, TerritoriesCount: >= 300}) {
			return TitleRank.empire;
		}
		
		switch (country.Rank) {
			case CountryRank.migrantHorde:
			case CountryRank.cityState:
				return TitleRank.duchy;
			case CountryRank.localPower:
			case CountryRank.regionalPower:
			case CountryRank.majorPower:
				return TitleRank.kingdom;
			case CountryRank.greatPower:
				return TitleRank.empire;
			default:
				return TitleRank.duchy;
		}
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
	private static string GenerateNewTitleId(Country country, string localizedTitleName) {
		var ck3Rank = GetCK3TitleRank(country, localizedTitleName);
		
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

	private readonly List<Mapping> mappings = new();
	private readonly Dictionary<ulong, string> registeredCountryTitles = new(); // We store already mapped countries here.
	private readonly Dictionary<string, string> registeredGovernorshipTitles = new(); // We store already mapped governorships here.
	private readonly SortedSet<string> usedTitles = new();

	private const string GeneratedCK3TitlePrefix = "IRTOCK3_";
}