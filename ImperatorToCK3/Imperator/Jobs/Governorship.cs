using commonItems;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImperatorToCK3.Imperator.Jobs;

internal sealed class Governorship {
	public Country Country { get; }
	public ulong CharacterId { get; private set; } = 0;
	public Date StartDate { get; private set; } = new(1, 1, 1);
	public ImperatorRegion Region { get; }

	internal Governorship(Country country, ulong characterId, Date startDate, ImperatorRegion region) {
		Country = country;
		CharacterId = characterId;
		StartDate = startDate;
		Region = region;
	}

	public Governorship(BufferedReader governorshipReader, CountryCollection countries, ImperatorRegionMapper irRegionMapper) {
		ulong? countryId = null;
		string? regionId = null;

		var parser = new Parser(implicitVariableHandling: false);
		parser.RegisterKeyword("who", reader => countryId = reader.GetULong());
		parser.RegisterKeyword("character", reader => CharacterId = reader.GetULong());
		parser.RegisterKeyword("start_date", reader => StartDate = new Date(reader.GetString(), AUC: true));
		parser.RegisterKeyword("governorship", reader => regionId = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		parser.ParseStream(governorshipReader);

		// Throw exception if country ID or region ID is missing.
		if (countryId is null) {
			throw new FormatException("Country ID missing!");
		}
		if (regionId is null) {
			throw new FormatException("Region ID missing!");
		}

		Country = countries[countryId.Value];
		if (irRegionMapper.Regions.TryGetValue(regionId, out var region)) {
			Region = region;
		} else {
			throw new KeyNotFoundException($"Region {regionId} does not exist!");
		}
	}

	public int GetIRProvinceCount(ProvinceCollection irProvinces) {
		int provinceCount = 0;
		foreach (var provinceId in GetRegionProvinceIds()) {
			if (irProvinces.TryGetValue(provinceId, out var province) && province.OwnerCountry == Country) {
				++provinceCount;
			}
		}

		return provinceCount;
	}

	public ImmutableArray<Province> GetIRProvinces(ProvinceCollection irProvinces) {
		var provinces = ImmutableArray.CreateBuilder<Province>();
		foreach (var provinceId in GetRegionProvinceIds()) {
			if (!irProvinces.TryGetValue(provinceId, out var province) || province.OwnerCountry != Country) {
				continue;
			}
			provinces.Add(province);
		}
		return provinces.ToImmutable();
	}

	public ImmutableArray<ulong> GetCK3ProvinceIds(ProvinceCollection irProvinces, ProvinceMapper provMapper) {
		var ck3ProvinceIds = ImmutableArray.CreateBuilder<ulong>();
		foreach (var provinceId in GetRegionProvinceIds()) {
			if (!irProvinces.TryGetValue(provinceId, out var irProvince) || irProvince.OwnerCountry != Country) {
				continue;
			}

			foreach (var ck3ProvinceId in provMapper.GetCK3ProvinceNumbers(irProvince.Id)) {
				ck3ProvinceIds.Add(ck3ProvinceId);
			}
		}
		return ck3ProvinceIds.ToImmutable();
	}

	private ImmutableArray<ulong> GetRegionProvinceIds() {
		if (!cachedRegionProvinceIds.IsDefault) {
			return cachedRegionProvinceIds;
		}

		var provinceIds = new SortedSet<ulong>();
		foreach (var area in Region.Areas) {
			provinceIds.UnionWith(area.ProvinceIds);
		}

		cachedRegionProvinceIds = [.. provinceIds];
		return cachedRegionProvinceIds;
	}

	private ImmutableArray<ulong> cachedRegionProvinceIds;
}