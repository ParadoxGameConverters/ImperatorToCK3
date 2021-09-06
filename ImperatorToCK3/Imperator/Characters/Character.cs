using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.Imperator.Families;

namespace ImperatorToCK3.Imperator.Characters {
	public class Character {
		public Character(ulong ID) {
			this.ID = ID;
		}
		public ulong ID { get; } = 0;
		public KeyValuePair<ulong, Countries.Country?>? Country { get; set; }
		private string culture = string.Empty;
		public string Culture {
			get {
				if (!string.IsNullOrEmpty(culture)) {
					return culture;
				}
				if (family.Key != 0 && family.Value is not null && !string.IsNullOrEmpty(family.Value.Culture)) {
					return family.Value.Culture;
				}
				return culture;
			}
			set {
				culture = value;
			}
		}
		public string Religion { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string? CustomName { get; set; }
		public string Nickname { get; set; } = string.Empty;
		public ulong ProvinceID { get; private set; } = 0;
		public Date BirthDate { get; private set; } = new Date(1, 1, 1);
		public Date? DeathDate { get; private set; }
		public bool IsDead => DeathDate is not null;
		public string? DeathReason { get; set; }
		public Dictionary<ulong, Character?> Spouses { get; set; } = new();
		public Dictionary<ulong, Character?> Children { get; set; } = new();
		public KeyValuePair<ulong, Character?> Mother { get; set; } = new();
		public KeyValuePair<ulong, Character?> Father { get; set; } = new();
		private KeyValuePair<ulong, Family?> family = new(0, null);
		public KeyValuePair<ulong, Family?> Family {
			get {
				return family;
			}
			set {
				if (value.Value is null) {
					Logger.Warn($"Setting null family {value.Key} to character {ID}!");
				} else if (value.Value.ID != value.Key) {
					Logger.Warn($"Setting family with ID mismatch: {value.Key} v. {value.Value.ID}!");
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
					if (Female) {
						return "female";
					}
					return "male";
				}
				if (Female) {
					return "girl";
				}
				return "boy";
			}
		}
		public bool Female { get; private set; } = false;
		public double Wealth { get; private set; } = 0;

		public CK3.Characters.Character? CK3Character { get; set; }

		public void AddYears(int years) {
			BirthDate.SubtractYears(years);
		}

		private Genes.GenesDB? genes;

		private static readonly Parser parser = new();
		private static Character parsedCharacter = new(0);
		public static HashSet<string> IgnoredTokens { get; } = new();
		static Character() {
			parser.RegisterKeyword("first_name_loc", reader => {
				var characterName = new CharacterName(reader);
				parsedCharacter.Name = characterName.Name;
				parsedCharacter.CustomName = characterName.CustomName;
			});
			parser.RegisterKeyword("country", reader => {
				parsedCharacter.Country = new(ParserHelpers.GetULong(reader), null);
			});
			parser.RegisterKeyword("province", reader => {
				parsedCharacter.ProvinceID = ParserHelpers.GetULong(reader);
			});
			parser.RegisterKeyword("culture", reader => {
				parsedCharacter.culture = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("religion", reader => {
				parsedCharacter.Religion = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("female", reader => {
				parsedCharacter.Female = ParserHelpers.GetString(reader) == "yes";
			});
			parser.RegisterKeyword("traits", reader => {
				parsedCharacter.Traits = ParserHelpers.GetStrings(reader);
			});
			parser.RegisterKeyword("birth_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				parsedCharacter.BirthDate = new Date(dateStr, true); // converted to AD
			});
			parser.RegisterKeyword("death_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				parsedCharacter.DeathDate = new Date(dateStr, true); // converted to AD
			});
			parser.RegisterKeyword("death", reader => {
				parsedCharacter.DeathReason = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("age", reader => {
				parsedCharacter.Age = (uint)ParserHelpers.GetInt(reader);
			});
			parser.RegisterKeyword("nickname", reader => {
				parsedCharacter.Nickname = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("family", reader => {
				parsedCharacter.family = new(ParserHelpers.GetULong(reader), null);
			});
			parser.RegisterKeyword("dna", reader => {
				parsedCharacter.DNA = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("mother", reader => {
				parsedCharacter.Mother = new(ParserHelpers.GetULong(reader), null);
			});
			parser.RegisterKeyword("father", reader => {
				parsedCharacter.Father = new(ParserHelpers.GetULong(reader), null);
			});
			parser.RegisterKeyword("wealth", reader => {
				parsedCharacter.Wealth = ParserHelpers.GetDouble(reader);
			});
			parser.RegisterKeyword("spouse", reader => {
				foreach (var spouse in ParserHelpers.GetULongs(reader)) {
					parsedCharacter.Spouses.Add(spouse, null);
				}
			});
			parser.RegisterKeyword("children", reader => {
				foreach (var child in ParserHelpers.GetULongs(reader)) {
					parsedCharacter.Children.Add(child, null);
				}
			});
			parser.RegisterKeyword("attributes", reader => {
				parsedCharacter.Attributes = CharacterAttributes.Parse(reader);
			});
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
			if (parsedCharacter.DNA?.Length == 552) {
				parsedCharacter.PortraitData = new PortraitData(parsedCharacter.DNA, parsedCharacter.genes, parsedCharacter.AgeSex);
			}

			return parsedCharacter;
		}
	}
}
