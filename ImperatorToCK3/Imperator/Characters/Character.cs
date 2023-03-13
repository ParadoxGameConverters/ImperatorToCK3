using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Families;
using Open.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.Imperator.Characters {
	public class Character : IIdentifiable<ulong> {
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
		public ulong ProvinceId { get; private set; } = 0;
		public Date BirthDate { get; private set; } = new(1, 1, 1);
		public Date? DeathDate { get; set; }
		public bool IsDead => DeathDate is not null;
		public string? DeathReason { get; set; }
		private HashSet<ulong> parsedSpouseIds = new();
		public Dictionary<ulong, Character> Spouses { get; set; } = new();
		public OrderedSet<ulong> FriendIds { get; } = new();
		public OrderedSet<ulong> RivalIds { get; } = new();
		private HashSet<ulong> parsedChildrenIds = new();
		public Dictionary<ulong, Character> Children { get; set; } = new();
		private ulong? parsedMotherId;
		public Character? Mother { get; set; }
		private ulong? parsedFatherId;
		public Character? Father { get; set; }
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
		public List<string> Traits { get; set; } = new();
		public CharacterAttributes Attributes { get; private set; } = new();
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
		public double Wealth { get; set; } = 0;
		public ImmutableList<Unborn> Unborns { get; private set; } = ImmutableList<Unborn>.Empty;

		public CK3.Characters.Character? CK3Character { get; set; }

		public void AddYears(int years) {
			BirthDate = BirthDate.ChangeByYears(-years);
		}

		private Genes.GenesDB? genes;

		private static readonly Parser parser = new();
		private static Character parsedCharacter = new(0);
		public static IgnoredKeywordsSet IgnoredTokens { get; } = new();
		static Character() {
			parser.RegisterKeyword("first_name_loc", reader => {
				var characterName = new CharacterName(reader);
				parsedCharacter.Name = characterName.Name;
				parsedCharacter.CustomName = characterName.CustomName;
			});
			parser.RegisterKeyword("country", reader => parsedCharacter.parsedCountryId = reader.GetULong());
			parser.RegisterKeyword("home_country", reader => parsedCharacter.parsedHomeCountryId = reader.GetULong());
			parser.RegisterKeyword("province", reader => parsedCharacter.ProvinceId = reader.GetULong());
			parser.RegisterKeyword("culture", reader => parsedCharacter.culture = reader.GetString());
			parser.RegisterKeyword("religion", reader => parsedCharacter.Religion = reader.GetString());
			parser.RegisterKeyword("family", reader => parsedCharacter.parsedFamilyId = reader.GetULong());
			parser.RegisterKeyword("traits", reader => parsedCharacter.Traits = reader.GetStrings());
			parser.RegisterKeyword("female", reader => parsedCharacter.Female = reader.GetBool());
			parser.RegisterKeyword("children", reader => parsedCharacter.parsedChildrenIds = reader.GetULongs().ToHashSet());
			parser.RegisterKeyword("spouse", reader => parsedCharacter.parsedSpouseIds = reader.GetULongs().ToHashSet());
			parser.RegisterKeyword("friends", reader => {
				parsedCharacter.FriendIds.Clear();
				parsedCharacter.FriendIds.AddRange(reader.GetULongs());
			});
			parser.RegisterKeyword("rivals", reader => {
				parsedCharacter.RivalIds.Clear();
				parsedCharacter.RivalIds.AddRange(reader.GetULongs());
			});
			parser.RegisterKeyword("birth_date", reader => {
				var dateStr = reader.GetString();
				parsedCharacter.BirthDate = new Date(dateStr, true); // converted to AD
			});
			parser.RegisterKeyword("death_date", reader => {
				var dateStr = reader.GetString();
				parsedCharacter.DeathDate = new Date(dateStr, true); // converted to AD
			});
			parser.RegisterKeyword("death", reader => parsedCharacter.DeathReason = reader.GetString());
			parser.RegisterKeyword("age", reader => parsedCharacter.Age = (uint)reader.GetInt());
			parser.RegisterKeyword("nickname", reader => parsedCharacter.Nickname = reader.GetString());
			parser.RegisterKeyword("dna", reader => parsedCharacter.DNA = reader.GetString());
			parser.RegisterKeyword("mother", reader => parsedCharacter.parsedMotherId = reader.GetULong());
			parser.RegisterKeyword("father", reader => parsedCharacter.parsedFatherId = reader.GetULong());
			parser.RegisterKeyword("wealth", reader => parsedCharacter.Wealth = reader.GetDouble());
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
				parsedCharacter.Unborns = unborns.ToImmutableList();
			});
			parser.RegisterKeyword("attributes", reader => parsedCharacter.Attributes = CharacterAttributes.Parse(reader));
			parser.RegisterKeyword("prisoner_home", reader => parsedCharacter.parsedPrisonerHomeId = reader.GetULong());
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}
		public static Character Parse(BufferedReader reader, string idString, Genes.GenesDB? genesDB) {
			parsedCharacter = new Character(ulong.Parse(idString)) {
				genes = genesDB
			};

			parser.ParseStream(reader);
			if (parsedCharacter.DNA?.Length == 552 && parsedCharacter.genes is not null) {
				parsedCharacter.PortraitData = new PortraitData(parsedCharacter.DNA, parsedCharacter.genes, parsedCharacter.AgeSex);
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
	}
}
