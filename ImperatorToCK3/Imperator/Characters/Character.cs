using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.Imperator.Families;

namespace ImperatorToCK3.Imperator.Characters {
	public class Character {
		public Character(ulong ID) {
			this.ID = ID;
		}
		public ulong ID { get; } = 0;
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
		static Character() {
			parser.RegisterKeyword("first_name_loc", reader => {
				parsedCharacter.Name = new CharacterName(reader).Name;
			});
			parser.RegisterKeyword("province", reader => {
				parsedCharacter.ProvinceID = new SingleULong(reader).ULong;
			});
			parser.RegisterKeyword("culture", reader => {
				parsedCharacter.culture = new SingleString(reader).String;
			});
			parser.RegisterKeyword("religion", reader => {
				parsedCharacter.Religion = new SingleString(reader).String;
			});
			parser.RegisterKeyword("female", reader => {
				var femStr = new SingleString(reader).String;
				parsedCharacter.Female = femStr == "yes";
			});
			parser.RegisterKeyword("traits", reader => {
				parsedCharacter.Traits = new StringList(reader).Strings;
			});
			parser.RegisterKeyword("birth_date", reader => {
				var dateStr = new SingleString(reader).String;
				parsedCharacter.BirthDate = new Date(dateStr, true); // converted to AD
			});
			parser.RegisterKeyword("death_date", reader => {
				var dateStr = new SingleString(reader).String;
				parsedCharacter.DeathDate = new Date(dateStr, true); // converted to AD
			});
			parser.RegisterKeyword("death", reader => {
				parsedCharacter.DeathReason = new SingleString(reader).String;
			});
			parser.RegisterKeyword("age", reader => {
				parsedCharacter.Age = (uint)new SingleInt(reader).Int;
			});
			parser.RegisterKeyword("nickname", reader => {
				parsedCharacter.Nickname = new SingleString(reader).String;
			});
			parser.RegisterKeyword("family", reader => {
				parsedCharacter.family = new(new SingleULong(reader).ULong, null);
			});
			parser.RegisterKeyword("dna", reader => {
				parsedCharacter.DNA = new SingleString(reader).String;
			});
			parser.RegisterKeyword("mother", reader => {
				parsedCharacter.Mother = new(new SingleULong(reader).ULong, null);
			});
			parser.RegisterKeyword("father", reader => {
				parsedCharacter.Father = new(new SingleULong(reader).ULong, null);
			});
			parser.RegisterKeyword("wealth", reader => {
				parsedCharacter.Wealth = new SingleDouble(reader).Double;
			});
			parser.RegisterKeyword("spouse", reader => {
				foreach (var spouse in new ULongList(reader).ULongs) {
					parsedCharacter.Spouses.Add(spouse, null);
				}
			});
			parser.RegisterKeyword("children", reader => {
				foreach (var child in new ULongList(reader).ULongs) {
					parsedCharacter.Children.Add(child, null);
				}
			});
			parser.RegisterKeyword("attributes", reader => {
				parsedCharacter.Attributes = CharacterAttributes.Parse(reader);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
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
