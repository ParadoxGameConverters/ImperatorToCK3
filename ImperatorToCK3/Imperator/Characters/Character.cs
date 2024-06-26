using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.CommonUtils.Map;
using Open.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.Imperator.Characters; 

public sealed class Character : IIdentifiable<ulong> {
	public Character(ulong id) {
		Id = id;
	}
	public ulong Id { get; } = 0;

	private ulong? parsedCountryId;
	public Country? Country { get; set; }
	private ulong? parsedHomeCountryId;
	public Country? HomeCountry { get; set; }
	private ulong? parsedPrisonerHomeId;
	public Country? PrisonerHome { get; private set; }

	private string culture = string.Empty;
	public string Culture {
		get {
			if (!string.IsNullOrEmpty(culture)) {
				return culture;
			}
			if (family is not null && !string.IsNullOrEmpty(family.Culture)) {
				return family.Culture;
			}
			return culture;
		}
		set => culture = value;
	}
	public string Religion { get; set; } = string.Empty;
	public double? Health { get; private set; }
	public string Name { get; set; } = string.Empty;
	public string? CustomName { get; set; }

	// Returned value indicates whether a family was linked
	public bool LinkFamily(FamilyCollection families, SortedSet<ulong>? missingDefinitionsSet = null) {
		if (parsedFamilyId is null) {
			return false;
		}
		var familyId = (ulong)parsedFamilyId;
		if (families.TryGetValue(familyId, out var familyToLink)) {
			Family = familyToLink;
			familyToLink.AddMember(this);
			return true;
		}

		missingDefinitionsSet?.Add(familyId);
		return false;
	}

	public string? Nickname { get; set; }
	public ulong? ProvinceId { get; private set; }
	public Date BirthDate { get; private set; } = new(1, 1, 1);
	public Date? DeathDate { get; set; }
	public bool IsDead => DeathDate is not null;
	public string? DeathReason { get; set; }
	private HashSet<ulong> parsedSpouseIds = new();
	public IDictionary<ulong, Character> Spouses { get; set; } = new Dictionary<ulong, Character>();
	public OrderedSet<ulong> FriendIds { get; } = new();
	public OrderedSet<ulong> RivalIds { get; } = new();
	private HashSet<ulong> parsedChildrenIds = new();
	public IDictionary<ulong, Character> Children { get; set; } = new Dictionary<ulong, Character>();
	private ulong? parsedMotherId;
	public Character? Mother { get; set; }
	private ulong? parsedFatherId;
	public Character? Father { get; set; }
	
	public string? FamilyName { get; private set; } // For characters from minor families, this contains their actual family name.
	private ulong? parsedFamilyId;
	private Family? family;
	public Family? Family {
		get => family;
		set {
			if (value is null) {
				Logger.Warn($"Setting null family to character {Id}!");
			}
			family = value;
		}
	}
	
	public IList<string> Traits { get; set; } = new List<string>();
	public CharacterAttributes Attributes { get; private set; } = new();
	public IReadOnlySet<string> Variables { get; private set; } = ImmutableHashSet<string>.Empty;
	public bool IsBald => Variables.Contains("bald");
	public uint Age { get; private set; } = new();
	public string? DNA { get; private set; }
	public PortraitData? PortraitData { get; private set; }
	public string AgeSex {
		get {
			if (Age >= 16) {
				return Female ? "female" : "male";
			}
			return Female ? "girl" : "boy";
		}
	}
	public bool Female { get; set; } = false;
	public double? Fertility { get; private set; }
	public double Wealth { get; set; } = 0;
	public ImmutableList<Unborn> Unborns { get; private set; } = ImmutableList<Unborn>.Empty;

	public CK3.Characters.Character? CK3Character { get; set; }
	public static ConcurrentIgnoredKeywordsSet IgnoredTokens { get; } = [];
	public static void RegisterCharacterKeywords(Parser parser, Character character) {
		parser.RegisterKeyword("first_name_loc", reader => {
			var characterName = new CharacterName(reader);
			character.Name = characterName.Name;
			character.CustomName = characterName.CustomName;
		});
		parser.RegisterKeyword("family_name", reader => character.FamilyName = reader.GetString());
		parser.RegisterKeyword("country", reader => character.parsedCountryId = reader.GetULong());
		parser.RegisterKeyword("home_country", reader => character.parsedHomeCountryId = reader.GetULong());
		parser.RegisterKeyword("province", reader => character.ProvinceId = reader.GetULong());
		parser.RegisterKeyword("culture", reader => character.culture = reader.GetString());
		parser.RegisterKeyword("religion", reader => character.Religion = reader.GetString());
		parser.RegisterKeyword("fertility", reader => character.Fertility = reader.GetDouble());
		parser.RegisterKeyword("health", reader => character.Health = reader.GetDouble());
		parser.RegisterKeyword("family", reader => character.parsedFamilyId = reader.GetULong());
		parser.RegisterKeyword("traits", reader => character.Traits = reader.GetStrings());
		parser.RegisterKeyword("female", reader => character.Female = reader.GetBool());
		parser.RegisterKeyword("children", reader => character.parsedChildrenIds = [.. reader.GetULongs()]);
		parser.RegisterKeyword("spouse", reader => character.parsedSpouseIds = [.. reader.GetULongs()]);
		parser.RegisterKeyword("friends", reader => {
			character.FriendIds.Clear();
			character.FriendIds.AddRange(reader.GetULongs());
		});
		parser.RegisterKeyword("rivals", reader => {
			character.RivalIds.Clear();
			character.RivalIds.AddRange(reader.GetULongs());
		});
		parser.RegisterKeyword("age", reader => character.Age = (uint)reader.GetInt());
		parser.RegisterKeyword("birth_date", reader => {
			var dateStr = reader.GetString();
			character.BirthDate = new Date(dateStr, true); // converted to AD
		});
		parser.RegisterKeyword("death_date", reader => {
			var dateStr = reader.GetString();
			character.DeathDate = new Date(dateStr, true); // converted to AD
		});
		parser.RegisterKeyword("death", reader => character.DeathReason = reader.GetString());
		parser.RegisterKeyword("attributes", reader => character.Attributes = CharacterAttributes.Parse(reader));
		parser.RegisterKeyword("nickname", reader => character.Nickname = reader.GetString());
		parser.RegisterKeyword("dna", reader => character.DNA = reader.GetString());
		parser.RegisterKeyword("mother", reader => character.parsedMotherId = reader.GetULong());
		parser.RegisterKeyword("father", reader => character.parsedFatherId = reader.GetULong());
		parser.RegisterKeyword("wealth", reader => character.Wealth = reader.GetDouble());
		parser.RegisterKeyword("unborn", reader => {
			var unborns = new List<Unborn>();
			foreach (var blob in new BlobList(reader).Blobs) {
				var blobReader = new BufferedReader(blob);
				var unborn = Unborn.Parse(blobReader);
				if (unborn is null) {
					continue;
				}
				unborns.Add(unborn);
			}
			character.Unborns = unborns.ToImmutableList();
		});
		parser.RegisterKeyword("prisoner_home", reader => character.parsedPrisonerHomeId = reader.GetULong());
		parser.RegisterKeyword("variables", reader => {
			var variables = new HashSet<string>();
			var variablesParser = new Parser();
			variablesParser.RegisterKeyword("data", dataReader => {
				var blobParser = new Parser();
				blobParser.RegisterKeyword("flag", blobReader => variables.Add(blobReader.GetString()));
				blobParser.IgnoreUnregisteredItems();
				
				foreach (var blob in new BlobList(dataReader).Blobs) {
					var blobReader = new BufferedReader(blob);
					blobParser.ParseStream(blobReader);
				}
			});
			variablesParser.RegisterKeyword("list", ParserHelpers.IgnoreItem);
			variablesParser.IgnoreAndLogUnregisteredItems();
			variablesParser.ParseStream(reader);
			character.Variables = variables.ToImmutableHashSet();
		});
		parser.IgnoreAndStoreUnregisteredItems(IgnoredTokens);
	}
	public static Character Parse(BufferedReader reader, string idString, GenesDB? genesDB) {
		var parser = new Parser();
		var parsedCharacter = new Character(ulong.Parse(idString));
		RegisterCharacterKeywords(parser, parsedCharacter);

		parser.ParseStream(reader);
		if (genesDB is null) {
			Logger.Warn($"GenesDB is null when parsing character {idString}!");
		} else if (parsedCharacter.DNA?.Length > 0) {
			parsedCharacter.PortraitData = new PortraitData(parsedCharacter.DNA, genesDB, parsedCharacter.AgeSex);
		}

		return parsedCharacter;
	}

	// Returns counter of linked spouses
	public int LinkSpouses(CharacterCollection characters) {
		var counter = 0;
		foreach (var spouseId in parsedSpouseIds) {
			if (characters.TryGetValue(spouseId, out var spouseToLink)) {
				Spouses.Add(spouseToLink.Id, spouseToLink);
				++counter;
			} else {
				Logger.Warn($"Spouse ID: {spouseId} has no definition!");
			}
		}

		return counter;
	}

	// Returns whether a country was linked
	public bool LinkCountry(CountryCollection countries) {
		if (parsedCountryId is null) {
			Logger.Warn($"Character {Id} has no country!");
			return false;
		}
		var countryId = (ulong)parsedCountryId;
		if (countries.TryGetValue(countryId, out var countryToLink)) {
			Country = countryToLink;
			Country.TryLinkMonarch(this);
			return true;
		}
		Logger.Warn($"Country with ID {countryId} has no definition!");
		return false;
	}

	// Returns whether a country was linked
	public bool LinkHomeCountry(CountryCollection countries) {
		if (parsedHomeCountryId is null) {
			return false;
		}
		var homeCountryId = (ulong)parsedHomeCountryId;
		if (countries.TryGetValue(homeCountryId, out var countryToLink)) {
			HomeCountry = countryToLink;
			return true;
		}
		Logger.Warn($"Country with ID {homeCountryId} has no definition!");
		return false;
	}

	// Returns whether a country was linked
	public bool LinkPrisonerHome(CountryCollection countries) {
		if (parsedPrisonerHomeId is null) {
			return false;
		}
		var prisonerHomeId = (ulong)parsedPrisonerHomeId;
		if (countries.TryGetValue(prisonerHomeId, out var countryToLink)) {
			PrisonerHome = countryToLink;
			return true;
		}
		Logger.Warn($"Country with ID {prisonerHomeId} has no definition!");
		return false;
	}

	// Returns whether a mother was linked
	public bool LinkMother(CharacterCollection characters) {
		if (parsedMotherId is null) {
			return false;
		}
		ulong motherId = (ulong)parsedMotherId;

		if (characters.TryGetValue(motherId, out var motherToLink)) {
			Mother = motherToLink;
			if (!motherToLink.parsedChildrenIds.Contains(Id)) {
				Logger.Warn($"Only one-sided link found between character {Id} and mother {motherId}!");
			}
			motherToLink.Children[Id] = this;
			return true;
		}
		Logger.Warn($"Mother ID: {motherId} has no definition!");
		return false;
	}

	// Returns whether a father was linked
	public bool LinkFather(CharacterCollection characters) {
		if (parsedFatherId is null) {
			return false;
		}
		ulong fatherId = (ulong)parsedFatherId;

		if (characters.TryGetValue(fatherId, out var fatherToLink)) {
			Father = fatherToLink;
			if (!fatherToLink.parsedChildrenIds.Contains(Id)) {
				Logger.Warn($"Only one-sided link found between character {Id} and father {fatherId}!");
			}
			fatherToLink.Children[Id] = this;
			return true;
		}
		Logger.Warn($"Father ID: {fatherId} has no definition!");
		return false;
	}

	/// <summary>
	/// Returns a land province that can be considered a "source" of this character.
	/// For instance, when a character is at sea, this method tries to use the country's capital,
	/// or even the location of the character's parents.
	/// </summary>
	/// <param name="irMapData">Imperator map data.</param>
	/// <returns></returns>
	public ulong? GetSourceLandProvince(MapData irMapData) {
		HashSet<ulong> rejectedProvinceIds = [];
		
		if (ProvinceId.HasValue) {
			if (!irMapData.ProvinceDefinitions.TryGetValue(ProvinceId.Value, out var provinceDef)) {
				Logger.Warn($"Potential source province {ProvinceId.Value} for character {Id} has no definition!");
			} else if (provinceDef.IsLand) {
				return ProvinceId;
			}
			rejectedProvinceIds.Add(ProvinceId.Value);
		}

		var homeCountryCapital = HomeCountry?.CapitalProvinceId;
		if (homeCountryCapital.HasValue && !rejectedProvinceIds.Contains(homeCountryCapital.Value)) {
			if (!irMapData.ProvinceDefinitions.TryGetValue(homeCountryCapital.Value, out var homeCountryCapitalDef)) {
				Logger.Warn($"Potential source province {homeCountryCapital.Value} for character {Id} has no definition!");
			} else if (homeCountryCapitalDef.IsLand) {
				return homeCountryCapital;
			}
			rejectedProvinceIds.Add(homeCountryCapital.Value);
		}
		
		var countryCapital = Country?.CapitalProvinceId;
		if (countryCapital.HasValue && !rejectedProvinceIds.Contains(countryCapital.Value)) {
			if (!irMapData.ProvinceDefinitions.TryGetValue(countryCapital.Value, out var countryCapitalDef)) {
				Logger.Warn($"Potential source province {countryCapital.Value} for character {Id} has no definition!");
			} else if (countryCapitalDef.IsLand) {
				return countryCapital;
			}
			rejectedProvinceIds.Add(countryCapital.Value);
		}
		
		var fatherProvince = Father?.GetSourceLandProvince(irMapData);
		if (fatherProvince.HasValue) {
			return fatherProvince;
		}
		
		var motherProvince = Mother?.GetSourceLandProvince(irMapData);
		if (motherProvince.HasValue) {
			return motherProvince;
		}
		
		return null;
	}
}