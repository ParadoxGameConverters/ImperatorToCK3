using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using ImperatorProvince = ImperatorToCK3.Imperator.Provinces.Province;

namespace ImperatorToCK3.CK3.Provinces;

internal sealed partial class Province : IIdentifiable<ulong> {
	public ulong Id { get; } = 0;
	public ulong? BaseProvinceId { get; private set; }

	public ImperatorProvince? PrimaryImperatorProvince { get; set; } = null;
	private readonly OrderedSet<ImperatorProvince> secondaryImperatorProvinces = [];
	public IReadOnlySet<ImperatorProvince> SecondaryImperatorProvinces => secondaryImperatorProvinces;

	public ImmutableHashSet<ImperatorProvince> ImperatorProvinces {
		get => field ??= (PrimaryImperatorProvince is null
			? [.. secondaryImperatorProvinces]
			: [PrimaryImperatorProvince, .. secondaryImperatorProvinces]);
	}

	public Province(ulong id) {
		Id = id;
		History = historyFactory.GetHistory();
	}
	public Province(ulong id, BufferedReader reader): this(id) {
		History = historyFactory.GetHistory(reader);
	}
	public void CopyEntriesFromProvince(Province sourceProvince) {
		// Culture, faith and terrain can be copied from source province.
		BaseProvinceId = sourceProvince.Id;

		var srcProvinceHistoryFields = sourceProvince.History.Fields;

		var fieldsToCopy = new[] {"culture", "faith", "terrain"};
		foreach (var fieldName in fieldsToCopy) {
			if (History.Fields.TryGetValue(fieldName, out var field)) {
				if (field.DateToEntriesDict.Count != 0) {
					continue;
				}

				if (field.InitialEntries.Any()) {
					continue;
				}
			}

			History.Fields.AddOrReplace(srcProvinceHistoryFields[fieldName].Clone());
		}
	}

	public void InitializeFromImperator(
		ImperatorProvince primarySourceProvince,
		ICollection<ImperatorProvince> secondarySourceProvinces,
		Title.LandedTitles landedTitles,
		CultureMapper cultureMapper,
		ReligionMapper religionMapper,
		Date conversionDate,
		Configuration config
	) {
		secondaryImperatorProvinces.Clear();
		secondaryImperatorProvinces.UnionWith(secondarySourceProvinces);
		PrimaryImperatorProvince = primarySourceProvince;

		var fieldsToKeep = new[] {"culture", "faith", "terrain", "special_building_slot"};
		foreach (var field in History.Fields.Where(f=>!fieldsToKeep.Contains(f.Id))) {
			field.RemoveAllEntries();
		}

		History.RemoveHistoryPastDate(conversionDate);

		// Religion first
		SetReligionFromImperator(religionMapper, conversionDate, config);

		// Then culture
		SetCultureFromImperator(cultureMapper);

		// Holding type
		SetHoldingFromImperator(landedTitles);
	}

	public void UpdateHistory(BufferedReader reader) {
		historyFactory.UpdateHistory(History, reader);
	}

	private void SetReligionFromImperator(ReligionMapper religionMapper, Date conversionDate, Configuration config) {
		var cultureId = GetCultureId(conversionDate);

		var religionSet = false;
		if (PrimaryImperatorProvince is null) {
			Logger.Warn($"CK3 Province {Id}: can't set religion from null Imperator province!");
			return;
		}

		religionSet = TrySetReligionFromPrimarySource(PrimaryImperatorProvince, religionMapper, config, cultureId);
		if (!religionSet) {
			religionSet = TrySetReligionFromSecondarySources(PrimaryImperatorProvince, secondaryImperatorProvinces, religionMapper, config, cultureId);
		}
		// As fallback, attempt to use religions of source provinces' countries.
		var sourceProvincesWithCountryReligion = ImperatorProvinces
			.Where(p => p.OwnerCountry?.Religion is not null);
		if (!religionSet) {
			foreach (var sourceProvince in sourceProvincesWithCountryReligion) {
				var religionMatch = religionMapper.Match(
					irReligionId: sourceProvince.OwnerCountry!.Religion!,
					ck3CultureId: cultureId,
					ck3ProvinceId: Id,
					irProvinceId: sourceProvince.Id,
					irHistoricalTag: PrimaryImperatorProvince.OwnerCountry?.HistoricalTag,
					config: config
				);
				if (religionMatch is not null) {
					Logger.Warn($"Using country religion for province {Id}");
					SetFaithId(religionMatch, date: null);
					religionSet = true;
					break;
				}
			}
		}
		if (!religionSet) {
			// Use default CK3 religion.
			Logger.Warn(
				$"Couldn't determine faith for province {Id} with " +
				$"primary source religion {PrimaryImperatorProvince.ReligionId} " +
				$"and source provinces [{string.Join(", ", ImperatorProvinces.Select(p => p.Id))}], " +
				"using vanilla religion!");
		}
	}

	private bool TrySetReligionFromPrimarySource(ImperatorProvince irProvince, ReligionMapper religionMapper, Configuration config, string? cultureId) {
		// Try to use religion of primary source province.
		if (!string.IsNullOrEmpty(irProvince.ReligionId)) {
			var religionMatch = religionMapper.Match(
				irReligionId: irProvince.ReligionId,
				ck3CultureId: cultureId,
				ck3ProvinceId: Id,
				irProvinceId: irProvince.Id,
				irHistoricalTag: irProvince.OwnerCountry?.HistoricalTag,
				config: config
			);
			if (religionMatch is not null) {
				SetFaithId(religionMatch, date: null);
				return true;
			}
		}

		return false;
	}

	private bool TrySetReligionFromSecondarySources(ImperatorProvince primarySource, OrderedSet<ImperatorProvince> secondarySources, ReligionMapper religionMapper, Configuration config, string? cultureId) {
		// Try to use religion of secondary source province.
		foreach (var secondarySource in secondarySources) {
			var religionMatch = religionMapper.Match(
				irReligionId: secondarySource.ReligionId,
				ck3CultureId: cultureId,
				ck3ProvinceId: Id,
				irProvinceId: secondarySource.Id,
				irHistoricalTag: primarySource.OwnerCountry?.HistoricalTag,
				config: config
			);
			if (religionMatch is not null) {
				SetFaithId(religionMatch, date: null);
				return true;
			}
		}
		return false;
	}

	private void SetCultureFromImperator(CultureMapper cultureMapper) {
		if (PrimaryImperatorProvince is null) {
			Logger.Warn($"CK3 Province {Id}: can't set culture from null Imperator province!");
			return;
		}

		// Try to use culture of primary source province.
		bool cultureSet = TryToUseCultureOfPrimaryImperatorProvince(cultureMapper);
		// Try to use culture of secondary source province.
		if (!cultureSet) {
			cultureSet = TryToUseCultureOfSecondaryImperatorProvince(cultureMapper);
		}
		// As fallback, attempt to use primary cultures of source provinces' countries.
		if (!cultureSet) {
			cultureSet = TryToUsePrimaryCulturesOfSourceProvincesCountries(cultureMapper);
		}
		if (!cultureSet) {
			// Use default CK3 culture.
			Logger.Warn($"Couldn't determine culture for province {Id} with primary source culture " +
			            $"{PrimaryImperatorProvince.Culture} and source provinces" +
			            $"[{string.Join(", ", ImperatorProvinces.Select(p => p.Id))}], using vanilla culture!");
		}
	}

	private bool TryToUsePrimaryCulturesOfSourceProvincesCountries(CultureMapper cultureMapper) {
		bool cultureSet = false;
		var sourceProvincesWithCountryCultures = ImperatorProvinces
			.Select(p => new {
				Province = p, CultureId = p.OwnerCountry?.PrimaryCulture
			})
			.Where(obj => obj.CultureId is not null)
			.DistinctBy(obj=>obj.CultureId);
		foreach (var obj in sourceProvincesWithCountryCultures) {
			var cultureMatch = cultureMapper.Match(
				irCulture: obj.CultureId!,
				ck3ProvinceId: Id,
				irProvinceId: obj.Province.Id,
				historicalTag: obj.Province.OwnerCountry?.HistoricalTag ?? string.Empty
			);
			if (cultureMatch is not null) {
				Logger.Warn($"Using country culture for province {Id}");
				SetCultureId(cultureMatch, date: null);
				cultureSet = true;
				break;
			}
		}

		return cultureSet;
	}

	private bool TryToUseCultureOfSecondaryImperatorProvince(CultureMapper cultureMapper) {
		bool cultureSet = false;
		foreach (var secondarySource in SecondaryImperatorProvinces) {
			var cultureMatch = cultureMapper.Match(
				irCulture: secondarySource.Culture,
				ck3ProvinceId: Id,
				irProvinceId: secondarySource.Id,
				historicalTag: secondarySource.OwnerCountry?.HistoricalTag ?? string.Empty
			);
			if (cultureMatch is not null) {
				SetCultureId(cultureMatch, date: null);
				cultureSet = true;
				break;
			}
		}

		return cultureSet;
	}

	private bool TryToUseCultureOfPrimaryImperatorProvince(CultureMapper cultureMapper)
	{
		bool cultureSet = false;
		if (string.IsNullOrEmpty(PrimaryImperatorProvince?.Culture)) {
			return cultureSet;
		}

		var cultureMatch = cultureMapper.Match(
			irCulture: PrimaryImperatorProvince.Culture,
			ck3ProvinceId: Id,
			irProvinceId: PrimaryImperatorProvince.Id,
			historicalTag: PrimaryImperatorProvince.OwnerCountry?.HistoricalTag
		);
		if (cultureMatch is not null) {
			SetCultureId(cultureMatch, date: null);
			cultureSet = true;
		}

		return cultureSet;
	}

	private void SetHoldingFromImperator(Title.LandedTitles landedTitles) {
		if (PrimaryImperatorProvince is null) {
			Logger.Warn($"CK3 Province {Id}: can't set holding from null Imperator province!");
			return;
		}

		if (PrimaryImperatorProvince.OwnerCountry is null) {
			SetHoldingType("none", date: null);
			return;
		}

		var provinceRecord = new {
			PrimaryImperatorProvince.ProvinceRank,
			PrimaryImperatorProvince.OwnerCountry.GovernmentType,
			PrimaryImperatorProvince.IsHolySite,
			PrimaryImperatorProvince.Fort,
			// CK3 holdings that are county capitals always match the government type.
			IsCountyCapital = IsCountyCapital(landedTitles)
		};

		var holdingType = provinceRecord switch {
			{
				IsCountyCapital: false,
				IsHolySite: true
			} => "church_holding",
			{
				IsCountyCapital: false,
				GovernmentType: GovernmentType.monarchy,
				Fort: true
			} => "castle_holding",
			{
				IsCountyCapital: false,
				ProvinceRank: ProvinceRank.city or ProvinceRank.city_metropolis
			} => "city_holding",
			{
				IsCountyCapital: false,
				GovernmentType: GovernmentType.republic,
				ProvinceRank: ProvinceRank.settlement,
				Fort: true
			} => "city_holding",
			{
				IsCountyCapital: false,
				ProvinceRank: ProvinceRank.settlement
			} => "none",
			{
				IsCountyCapital: true,
				GovernmentType: GovernmentType.monarchy,
			} => "castle_holding",
			{
				IsCountyCapital: true,
				GovernmentType: GovernmentType.republic,
			} => "city_holding",
			{
				IsCountyCapital: true,
				GovernmentType: GovernmentType.tribal,
			} => "tribal_holding",
			_ => "none"
		};
		SetHoldingType(holdingType, null);
	}

	public bool IsCountyCapital(Title.LandedTitles landedTitles) {
		return landedTitles.CapitalBaronyProvinceIds.Contains(Id);
	}
}