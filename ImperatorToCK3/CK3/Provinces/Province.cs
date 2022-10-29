using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Religion;
using System.Linq;

namespace ImperatorToCK3.CK3.Provinces;

public partial class Province : IIdentifiable<ulong> {
	public ulong Id { get; } = 0;
	public ulong? BaseProvinceId { get; }

	public Imperator.Provinces.Province? ImperatorProvince { get; set; }
	
	public Province(ulong id) {
		Id = id;
		History = historyFactory.GetHistory();
	}
	public Province(ulong id, BufferedReader reader): this(id) {
		History = historyFactory.GetHistory(reader);
	}
	public Province(ulong id, Province sourceProvince): this(id) {
		// culture, faith and terrain can be copied from source province
		BaseProvinceId = sourceProvince.Id;

		var srcProvinceHistoryFields = sourceProvince.History.Fields;
		History.Fields.AddOrReplace(srcProvinceHistoryFields["culture"].Clone());
		History.Fields.AddOrReplace(srcProvinceHistoryFields["faith"].Clone());
		History.Fields.AddOrReplace(srcProvinceHistoryFields["terrain"].Clone());
	}

	public void InitializeFromImperator(
		Imperator.Provinces.Province impProvince,
		Title.LandedTitles landedTitles,
		CultureMapper cultureMapper,
		ReligionMapper religionMapper,
		Configuration config
	) {
		ImperatorProvince = impProvince;

		var fieldsToKeep = new[] {"culture", "faith", "terrain", "special_building_slot"};
		foreach (var field in History.Fields.Where(f=>!fieldsToKeep.Contains(f.Id))) {
			field.RemoveAllEntries();
		}
		
		History.RemoveHistoryPastDate(config.CK3BookmarkDate);

		// Religion first
		SetReligionFromImperator(religionMapper, config);

		// Then culture
		SetCultureFromImperator(cultureMapper, config);

		// Holding type
		SetHoldingFromImperator(landedTitles);
	}

	public void UpdateHistory(BufferedReader reader) {
		historyFactory.UpdateHistory(History, reader);
	}

	private void SetReligionFromImperator(ReligionMapper religionMapper, Configuration config) {
		var religionSet = false;
		if (ImperatorProvince is null) {
			Logger.Warn($"CK3 Province {Id}: can't set religion from null Imperator province!");
			return;
		}

		if (!string.IsNullOrEmpty(ImperatorProvince.ReligionId)) {
			var religionMatch = religionMapper.Match(ImperatorProvince.ReligionId, Id, ImperatorProvince.Id, config);
			if (religionMatch is not null) {
				SetFaithId(religionMatch, date: null);
				religionSet = true;
			}
		}
		// As fallback, attempt to use religion of country.
		if (!religionSet && ImperatorProvince.OwnerCountry?.Religion is not null) {
			var religionMatch = religionMapper.Match(ImperatorProvince.OwnerCountry.Religion, Id, ImperatorProvince.Id, config);
			if (religionMatch is not null) {
				Logger.Warn($"Using country religion for province {Id}");
				SetFaithId(religionMatch, date: null);
				religionSet = true;
			}
		}
		if (!religionSet) {
			//Use default CK3 religion.
			Logger.Warn($"Couldn't determine faith for province {Id} with source province {ImperatorProvince.Id} ({ImperatorProvince.ReligionId} religion), using vanilla religion!");
		}
	}
	private void SetCultureFromImperator(CultureMapper cultureMapper, Configuration config) {
		var bookmarkDate = config.CK3BookmarkDate;
		var faithId = GetFaithId(bookmarkDate) ?? string.Empty;
		var cultureSet = false;
		if (ImperatorProvince is null) {
			Logger.Warn($"CK3 Province {Id}: can't set culture from null Imperator Province!");
			return;
		}

		// do we even have a base culture?
		if (!string.IsNullOrEmpty(ImperatorProvince.Culture)) {
			var cultureMatch = cultureMapper.Match(ImperatorProvince.Culture, faithId, Id, ImperatorProvince.Id, ImperatorProvince.OwnerCountry?.HistoricalTag ?? string.Empty);
			if (cultureMatch is not null) {
				SetCultureId(cultureMatch, date: null);
				cultureSet = true;
			}
		}
		// As fallback, attempt to use primary culture of country.
		if (!cultureSet && ImperatorProvince.OwnerCountry?.PrimaryCulture is not null) {
			var cultureMatch = cultureMapper.Match(ImperatorProvince.OwnerCountry.PrimaryCulture, faithId, Id, ImperatorProvince.Id, ImperatorProvince.OwnerCountry?.HistoricalTag ?? string.Empty);
			if (cultureMatch is not null) {
				Logger.Warn($"Using country culture for province {Id}");
				SetCultureId(cultureMatch, date: null);
				cultureSet = true;
			}
		}
		if (!cultureSet) {
			//Use default CK3 culture.
			Logger.Warn($"Couldn't determine culture for province {Id} with source culture {ImperatorProvince.Culture}, using vanilla culture!");
		}
	}
	private void SetHoldingFromImperator(Title.LandedTitles landedTitles) {
		if (ImperatorProvince is null) {
			Logger.Warn($"CK3 Province {Id}: can't set holding from null Imperator Province!");
			return;
		}

		if (ImperatorProvince.OwnerCountry is null) {
			SetHoldingType("none", date: null);
			return;
		}

		var provinceRecord = new {
			ImperatorProvince.ProvinceRank,
			ImperatorProvince.OwnerCountry.GovernmentType,
			ImperatorProvince.IsHolySite,
			ImperatorProvince.Fort,
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
				GovernmentType: GovernmentType.monarchy or GovernmentType.tribal,
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
		var capitalProvIds = landedTitles
			.Where(t => t.CapitalBaronyProvince is not null)
			.Select(t => (ulong)t.CapitalBaronyProvince!);
		return capitalProvIds.Contains(Id);
	}
}