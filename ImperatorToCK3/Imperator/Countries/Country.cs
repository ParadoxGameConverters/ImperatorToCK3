using commonItems.Collections;
using commonItems.Colors;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Imperator.Inventions;
using ImperatorToCK3.Imperator.Provinces;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Countries;

public enum CountryType { rebels, pirates, barbarians, mercenaries, real }
public enum CountryRank { migrantHorde, cityState, localPower, regionalPower, majorPower, greatPower }
public enum GovernmentType { monarchy, republic, tribal }
public partial class Country : IIdentifiable<ulong> {
	public ulong Id { get; } = 0;
	public bool PlayerCountry { get; set; }
	private ulong? monarchId;  // >=0 are valid
	public Character? Monarch { get; private set; }
	public string? PrimaryCulture { get; private set; }
	public string? Religion { get; private set; }
	public IList<RulerTerm> RulerTerms { get; set; } = new List<RulerTerm>();
	public IDictionary<string, int> HistoricalRegnalNumbers { get; private set; } = new Dictionary<string, int>();
	public string Tag { get; private set; } = "";
	private string? historicalTag;
	public string HistoricalTag {
		get => historicalTag ?? Tag;
		private set => historicalTag = value;
	}

	private ulong? parsedOriginCountryId;
	public Country? OriginCountry { get; private set; } = null;

	public string Name => CountryName.Name;
	public CountryName CountryName { get; private set; } = new();
	public string Flag { get; private set; } = "";
	public CountryType CountryType { get; private set; } = CountryType.real;
	public ulong? CapitalProvinceId { get; private set; }
	public string? Government { get; private set; }
	public GovernmentType GovernmentType { get; private set; } = GovernmentType.monarchy;
	private readonly SortedSet<string> monarchyLaws = new();
	private readonly SortedSet<string> republicLaws = new();
	private readonly SortedSet<string> tribalLaws = new();
	public Color? Color1 { get; private set; }
	public Color? Color2 { get; private set; }
	public Color? Color3 { get; private set; }
	public CountryCurrencies Currencies { get; private set; } = new();
	private readonly HashSet<ulong> parsedFamilyIds = [];
	public IDictionary<ulong, Family> Families { get; private set; } = new Dictionary<ulong, Family>();
	private readonly HashSet<Province> ownedProvinces = [];
	private readonly List<bool> inventionBooleans = [];

	public CK3.Titles.Title? CK3Title { get; set; }

	public Country(ulong id) {
		Id = id;
	}
	public SortedSet<string> GetLaws() {
		return GovernmentType switch {
			GovernmentType.monarchy => monarchyLaws,
			GovernmentType.republic => republicLaws,
			GovernmentType.tribal => tribalLaws,
			_ => monarchyLaws,
		};
	}
	public int TerritoriesCount => ownedProvinces.Count;
	public CountryRank Rank {
		get {
			return TerritoriesCount switch {
				0 => CountryRank.migrantHorde,
				1 => CountryRank.cityState,
				<= 24 => CountryRank.localPower,
				<= 99 => CountryRank.regionalPower,
				<= 499 => CountryRank.majorPower,
				_ => CountryRank.greatPower
			};
		}
	}
	public void RegisterProvince(Province province) {
		ownedProvinces.Add(province);
	}

	// Returns counter of families linked to the country
	public int LinkFamilies(FamilyCollection families, SortedSet<ulong> idsWithoutDefinition) {
		var counter = 0;
		foreach (var familyId in parsedFamilyIds) {
			if (families.TryGetValue(familyId, out var familyToLink)) {
				Families.Add(familyId, familyToLink);
				++counter;
			} else {
				idsWithoutDefinition.Add(familyId);
			}
		}

		return counter;
	}

	public void TryLinkMonarch(Character character) {
		if (monarchId == character.Id) {
			Monarch = character;
		}
	}
	
	public void LinkOriginCountry(CountryCollection countries) {
		if (parsedOriginCountryId != null && countries.TryGetValue((ulong)parsedOriginCountryId, out var originCountry)) {
			OriginCountry = originCountry;
		}
	}

	public IEnumerable<string> GetActiveInventionIds(InventionsDB inventionsDB) {
		return inventionsDB.GetActiveInventionIds(inventionBooleans);
	}
}