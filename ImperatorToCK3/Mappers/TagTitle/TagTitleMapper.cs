﻿using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
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
	public string? GetTitleForTag(Country country, string localizedTitleName) {
		// If country has an origin (e.g. rebelled from another country), the historical tag probably points to the original country.
		string tagForMapping = country.OriginCountry is not null ? country.Tag : country.HistoricalTag;

		// The only case where we fail is on invalid invocation. Otherwise, failure is not an option!
		if (string.IsNullOrEmpty(tagForMapping)) {
			return null;
		}

		// look up register
		if (registeredCountryTitles.TryGetValue(country.Id, out var titleToReturn)) {
			return titleToReturn;
		}

		// Attempt a title match
		foreach (var mapping in mappings) {
			var match = mapping.RankMatch(tagForMapping, GetCK3TitleRank(country, localizedTitleName));
			if (match is not null) {
				if (usedTitles.Contains(match)) {
					continue;
				}

				RegisterCountry(country.Id, match);
				return match;
			}
		}

		// Generate a new title
		var generatedTitle = GenerateNewTitle(country, localizedTitleName);
		RegisterCountry(country.Id, generatedTitle);
		return generatedTitle;
	}
	public string? GetTitleForTag(Country country) {
		return GetTitleForTag(country, string.Empty);
	}
	public string? GetTitleForGovernorship(Governorship governorship, Title.LandedTitles titles, Imperator.Provinces.ProvinceCollection irProvinces, ProvinceCollection ck3Provinces, ImperatorRegionMapper imperatorRegionMapper, ProvinceMapper provMapper) {
		var country = governorship.Country;
		if (country.CK3Title is null) {
			Logger.Warn($"Country {country.Tag} has no associated CK3 title!");
			return null;
		}
		var ck3LiegeTitle = country.CK3Title.Id;

		var rank = GetCK3GovernorshipRank(ck3LiegeTitle);
		if (rank is null) {
			return null;
		}

		// Look up register
		if (registeredGovernorshipTitles.TryGetValue($"{country.Tag}_{governorship.Region.Id}", out var titleToReturn)) {
			return titleToReturn;
		}

		if (rank == "c") {
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
		var generatedTitle = GenerateNewTitle(governorship.Region.Id, country.Tag, ck3LiegeTitle);
		RegisterGovernorship(governorship.Region.Id, country.Tag, generatedTitle);
		return generatedTitle;
	}

	private string? GetCountyForGovernorship(Governorship governorship, Country country, Title.LandedTitles titles, ProvinceCollection ck3Provinces, ImperatorRegionMapper imperatorRegionMapper) {
		foreach (var county in titles.Where(t => t.Rank == TitleRank.county)) {
			ulong capitalBaronyProvinceId = (ulong)county.CapitalBaronyProvince!;
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
	private static string GetCK3TitleRank(Country country, string localizedTitleName) {
		if (localizedTitleName.Contains("Empire", System.StringComparison.Ordinal)) {
			return "e";
		}

		if (localizedTitleName.Contains("Kingdom", System.StringComparison.Ordinal)) {
			return "k";
		}

		switch (country.Rank) {
			case CountryRank.migrantHorde:
			case CountryRank.cityState:
				return "d";
			case CountryRank.localPower:
			case CountryRank.regionalPower:
			case CountryRank.majorPower:
				return "k";
			case CountryRank.greatPower:
				return "e";
			default:
				return "d";
		}
	}
	private static string? GetCK3GovernorshipRank(string ck3LiegeTitle) {
		if (ck3LiegeTitle.StartsWith('e')) {
			return "k";
		}
		if (ck3LiegeTitle.StartsWith('k')) {
			return "d";
		}
		if (ck3LiegeTitle.StartsWith('d')) {
			return "c";
		}
		return null;
	}
	private static string GenerateNewTitle(Country country, string localizedTitleName) {
		var ck3Tag = GetCK3TitleRank(country, localizedTitleName);
		ck3Tag += "_";
		ck3Tag += GeneratedCK3TitlePrefix;
		ck3Tag += country.Tag;

		return ck3Tag;
	}
	private static string GenerateNewTitle(string imperatorRegion, string imperatorCountryTag, string ck3LiegeTitle) {
		var ck3Tag = GetCK3GovernorshipRank(ck3LiegeTitle);
		ck3Tag += "_";
		ck3Tag += GeneratedCK3TitlePrefix;
		ck3Tag += imperatorCountryTag;
		ck3Tag += "_";
		ck3Tag += imperatorRegion;

		return ck3Tag;
	}

	private readonly List<Mapping> mappings = new();
	private readonly Dictionary<ulong, string> registeredCountryTitles = new(); // We store already mapped countries here.
	private readonly Dictionary<string, string> registeredGovernorshipTitles = new(); // We store already mapped governorships here.
	private readonly SortedSet<string> usedTitles = new();

	private const string GeneratedCK3TitlePrefix = "IRTOCK3_";
}