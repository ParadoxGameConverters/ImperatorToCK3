using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Armies;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.Mappers.UnitType;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Characters {
	public class Character : IIdentifiable<string> {
		public string Id { get; }
		public bool FromImperator { get; } = false;
		public bool Female { get; init; }
		public string CultureId { get; set; } = string.Empty;
		public string FaithId { get; set; } = string.Empty;
		public string Name { get; set; }
		public string? Nickname { get; set; }
		public double? Gold { get; set; }

		public uint Age { get; private set; }
		public string AgeSex {
			get {
				if (Age >= 16) {
					return Female ? "female" : "male";
				}
				return Female ? "girl" : "boy";
			}
		}
		public Date BirthDate { get; set; }
		public Date? DeathDate { get; set; }
		public string? DeathReason { get; set; }
		public bool Dead => DeathDate is not null;
		public List<Pregnancy> Pregnancies { get; } = new();

		public Dictionary<string, int> MenAtArmsStacksPerType { get; } = new();

		public Dictionary<string, string> PrisonerIds { get; } = new(); // <prisoner id, imprisonment type>
		public Dictionary<string, LocBlock> Localizations { get; } = new();

		public Imperator.Characters.Character? ImperatorCharacter { get; set; }

		private static readonly HistoryFactory historyFactory = new HistoryFactory.HistoryFactoryBuilder()
			//.WithSimpleField("name", "name", null)
			//.WithSimpleField("female", "female", null)
			//.WithSimpleField("dynasty", "dynasty", null)
			.WithSimpleField("diplomacy", "diplomacy", null)
			.WithSimpleField("martial", "martial", null)
			.WithSimpleField("stewardship", "stewardship", null)
			.WithSimpleField("intrigue", "intrigue", null)
			.WithSimpleField("learning", "learning", null)
			//.WithSimpleField("culture", "culture", null)
			//.WithSimpleField("religion", "religion", null)
			.WithDiffField("traits", new() { "trait", "add_trait" }, new OrderedSet<string> { "remove_trait" })
			//.WithSimpleField("dna", "dna", null)
			//.WithSimpleField("mother", "mother", null)
			//.WithSimpleField("father", "father", null)
			.WithDiffField("spouses", new OrderedSet<string> { "add_spouse", "add_matrilineal_spouse" }, new OrderedSet<string> { "remove_spouse" })
			.WithDiffField("effects", new OrderedSet<string> { "effect" }, new OrderedSet<string>())
			.WithDiffField("character_modifiers", "add_character_modifier", "remove_character_modifier")
			.Build();
		public History History { get; } = historyFactory.GetHistory();

		public Character(string id, string name, Date birthDate) {
			Id = id;
			Name = name;
			BirthDate = birthDate;
		}
		public Character(
			RulerTerm.PreImperatorRulerInfo preImperatorRuler,
			Date rulerTermStart,
			Country imperatorCountry,
			LocDB locDB,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			ProvinceMapper provinceMapper,
			Configuration config
		) {
			Id = $"imperatorRegnal{imperatorCountry.Tag}{preImperatorRuler.Name}{rulerTermStart.ToString()[1..]}BC".Replace('.', '_');
			FromImperator = true;
			Name = preImperatorRuler.Name ?? Id;
			if (!string.IsNullOrEmpty(Name)) {
				var impNameLoc = locDB.GetLocBlockForKey(Name);
				if (impNameLoc is not null) {
					Localizations.Add(Name, impNameLoc);
				} else {  // fallback: use unlocalized name as displayed name
					Localizations.Add(Name, new LocBlock(Name, ConverterGlobals.PrimaryLanguage) {
						[ConverterGlobals.PrimaryLanguage] = Name,
					});
				}
			}

			BirthDate = preImperatorRuler.BirthDate!;
			DeathDate = preImperatorRuler.DeathDate!;

			// determine culture and religion
			ulong ck3Province = 0;
			ulong impProvince = 0;
			var srcReligion = preImperatorRuler.Religion ?? imperatorCountry.Religion;
			var srcCulture = preImperatorRuler.Culture ?? imperatorCountry.PrimaryCulture;
			if (imperatorCountry.CapitalProvinceId is not null) {
				impProvince = imperatorCountry.CapitalProvinceId.Value;
				var ck3Provinces = provinceMapper.GetCK3ProvinceNumbers(impProvince);
				if (ck3Provinces.Count > 0) {
					ck3Province = ck3Provinces[0];
				}
			}

			if (srcReligion is not null) {
				var religionMatch = religionMapper.Match(srcReligion, ck3Province, impProvince, imperatorCountry.HistoricalTag, config);
				if (religionMatch is not null) {
					FaithId = religionMatch;
				}
			}

			if (srcCulture is not null) {
				var cultureMatch = cultureMapper.Match(srcCulture, FaithId, ck3Province, impProvince, imperatorCountry.HistoricalTag);
				if (cultureMatch is not null) {
					CultureId = cultureMatch;
				}
			}

			Nickname = nicknameMapper.GetCK3NicknameForImperatorNickname(preImperatorRuler.Nickname);
		}

		public Character(
			Imperator.Characters.Character impCharacter,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			TraitMapper traitMapper,
			NicknameMapper nicknameMapper,
			LocDB locDB,
			ProvinceMapper provinceMapper,   // used to determine ck3 province for religion mapper
			DeathReasonMapper deathReasonMapper,
			Date dateOnConversion,
			Configuration config
		) {
			ImperatorCharacter = impCharacter;
			ImperatorCharacter.CK3Character = this;
			Id = "imperator" + ImperatorCharacter.Id;
			FromImperator = true;

			if (!string.IsNullOrEmpty(ImperatorCharacter.CustomName)) {
				var loc = ImperatorCharacter.CustomName;
				var locKey = CommonFunctions.NormalizeUTF8Path(loc.FoldToASCII().Replace(' ', '_'));
				Name = $"IRTOCK3_CUSTOM_NAME_{locKey}";

				var locBlock = new LocBlock(Name, ConverterGlobals.PrimaryLanguage) {
					[ConverterGlobals.PrimaryLanguage] = loc
				};
				Localizations.Add(Name, locBlock);
			} else {
				var nameLoc = ImperatorCharacter.Name;
				Name = nameLoc.Replace(' ', '_');
				if (!string.IsNullOrEmpty(Name)) {
					var matchedLocBlock = locDB.GetLocBlockForKey(Name);
					if (matchedLocBlock is not null) {
						Localizations.Add(Name, matchedLocBlock);
					} else {  // fallback: use unlocalized name as displayed name
						var locBlock = new LocBlock(Name, ConverterGlobals.PrimaryLanguage) {
							[ConverterGlobals.PrimaryLanguage] = nameLoc
						};
						Localizations.Add(Name, locBlock);
					}
				}
			}

			Female = ImperatorCharacter.Female;
			Age = ImperatorCharacter.Age;

			// Determine valid (not dropped in province mappings) "source province" to be used by religion mapper. Don't give up without a fight.
			var impProvForProvinceMapper = ImperatorCharacter.ProvinceId;
			if (provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper).Count == 0 && ImperatorCharacter.Father is not null) {
				impProvForProvinceMapper = ImperatorCharacter.Father.ProvinceId;
			}

			if (provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper).Count == 0 && ImperatorCharacter.Mother is not null) {
				impProvForProvinceMapper = ImperatorCharacter.Mother.ProvinceId;
			}

			if (provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper).Count == 0 && ImperatorCharacter.Spouses.Count > 0) {
				var firstSpouse = ImperatorCharacter.Spouses.First().Value;
				impProvForProvinceMapper = firstSpouse.ProvinceId;
			}

			var ck3ProvinceNumbers = provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper);
			// determine CK3 province for religionMapper
			ulong ck3Province = ck3ProvinceNumbers.Count > 0 ? ck3ProvinceNumbers[0] : 0;

			var match = religionMapper.Match(ImperatorCharacter.Religion, ck3Province, ImperatorCharacter.ProvinceId, ImperatorCharacter.HomeCountry?.HistoricalTag, config);
			if (match is not null) {
				FaithId = match;
			}

			match = cultureMapper.Match(
				ImperatorCharacter.Culture,
				FaithId, ck3Province,
				ImperatorCharacter.ProvinceId,
				ImperatorCharacter.Country?.HistoricalTag ?? string.Empty
			);
			if (match is null) {
				Logger.Warn($"Could not determine CK3 culture for Imperator character {ImperatorCharacter.Id}" +
							$" with culture {ImperatorCharacter.Culture}!");
			} else {
				CultureId = match;
			}

			// Determine character attributes.
			History.AddFieldValue(null, "diplomacy", "diplomacy", ImperatorCharacter.Attributes.Charisma);
			History.AddFieldValue(null, "martial", "martial", ImperatorCharacter.Attributes.Martial);
			History.AddFieldValue(null, "stewardship", "stewardship", ImperatorCharacter.Attributes.Finesse);
			var intrigue = (ImperatorCharacter.Attributes.Finesse + ImperatorCharacter.Attributes.Charisma) / 2;
			History.AddFieldValue(null, "intrigue", "intrigue", intrigue);
			History.AddFieldValue(null, "learning", "learning", ImperatorCharacter.Attributes.Zeal);

			foreach (var trait in traitMapper.GetCK3TraitsForImperatorTraits(ImperatorCharacter.Traits)) {
				History.Fields["traits"].InitialEntries.Add(new KeyValuePair<string, object>("trait", trait));
			}

			var nicknameMatch = nicknameMapper.GetCK3NicknameForImperatorNickname(ImperatorCharacter.Nickname);
			if (nicknameMatch is not null) {
				Nickname = nicknameMatch;
			}

			BirthDate = ImperatorCharacter.BirthDate;
			DeathDate = ImperatorCharacter.DeathDate;
			var impDeathReason = ImperatorCharacter.DeathReason;
			if (impDeathReason is not null) {
				DeathReason = deathReasonMapper.GetCK3ReasonForImperatorReason(impDeathReason);
			}

			if (ImperatorCharacter.Wealth != 0) {
				Gold = ImperatorCharacter.Wealth * config.ImperatorCurrencyRate;
			}

			// if character is imprisoned, set jailor
			SetJailor();
			SetEmployer();

			void SetJailor() {
				if (ImperatorCharacter.PrisonerHome is null) {
					return;
				}

				var prisonCountry = ImperatorCharacter.Country;
				if (prisonCountry is null) {
					Logger.Warn($"Imperator character {ImperatorCharacter.Id} is imprisoned but has no country!");
				} else if (prisonCountry.CK3Title is null) {
					Logger.Debug($"Imperator character {ImperatorCharacter.Id}'s prison country does not exist in CK3!");
				} else {
					jailorId = prisonCountry.CK3Title.GetHolderId(dateOnConversion);
				}
			}

			void SetEmployer() {
				var prisonerHome = ImperatorCharacter.PrisonerHome;
				var homeCountry = ImperatorCharacter.HomeCountry;
				if (prisonerHome?.CK3Title is not null) { // is imprisoned
					EmployerId = prisonerHome.CK3Title.GetHolderId(dateOnConversion);
				} else if (homeCountry?.CK3Title is not null) {
					EmployerId = homeCountry.CK3Title.GetHolderId(dateOnConversion);
				}
			}
		}

		public void BreakAllLinks(CharacterCollection characters) {
			Mother?.RemoveChild(Id);
			RemoveMother();
			Father?.RemoveChild(Id);
			RemoveFather();

			foreach (var spouse in spousesCache) {
				spouse.RemoveSpouse(Id);
			}
			if (History.Fields.TryGetValue("spouses", out var spousesHistory)) {
				spousesHistory.InitialEntries.Clear();
				spousesHistory.DateToEntriesDict.Clear();
			}

			if (Female) {
				foreach (var (childId, child) in Children) {
					if (child is null) {
						Logger.Warn($"Child {childId} of {Id} is null!");
						continue;
					}
					child.RemoveMother();
				}
			} else {
				foreach (var (childId, child) in Children) {
					if (child is null) {
						Logger.Warn($"Child {childId} of {Id} is null!");
						continue;
					}
					child.RemoveFather();
				}
			}
			Children.Clear();

			if (ImperatorCharacter is not null) {
				ImperatorCharacter.CK3Character = null;
				ImperatorCharacter = null;
			}
		}

		public OrderedSet<object>? GetSpouseIds(Date date) {
			return History.GetFieldValueAsCollection("spouses", date);
		}
		public void AddSpouse(Date date, Character spouse) {
			History.AddFieldValue(date, "spouses", "add_spouse", spouse.Id);
			spouse.spousesCache.Add(this);
		}
		private void RemoveSpouse(string spouseId) {
			if (History.Fields.TryGetValue("spouses", out var spousesHistory)) {
				spousesHistory.RemoveAllEntries(value => (value.ToString() ?? string.Empty).Equals(spouseId));
			}
			spousesCache.RemoveWhere(c => c.Id == spouseId);
		}

		private void RemoveFather() {
			Father = null;
		}

		private void RemoveMother() {
			Mother = null;
		}

		private void RemoveChild(string childId) {
			Children.Remove(childId);
		}

		public string? PendingMotherId { get; set; }
		private Character? mother;
		public Character? Mother {
			get => mother;
			set {
				if (PendingMotherId is not null && value is not null && value.Id != PendingMotherId) {
					Logger.Warn($"Character {Id}: linking mother {value.Id} instead of expected {PendingMotherId}");
				}
				mother = value;
				PendingMotherId = null;
			}
		}
		public string? PendingFatherId { get; set; }
		private Character? father;
		public Character? Father {
			get => father;
			set {
				if (PendingFatherId is not null && value is not null && value.Id != PendingFatherId) {
					Logger.Warn($"Character {Id}: linking father {value.Id} instead of expected {PendingFatherId}");
				}
				father = value;
				PendingFatherId = null;
			}
		}
		public Dictionary<string, Character?> Children { get; set; } = new();

		public string? DynastyId { get; set; } // not always set

		private string? jailorId;
		private readonly HashSet<Character> spousesCache = new();
		public string? EmployerId { get; set; }

		public bool LinkJailor(CharacterCollection characters) {
			if (jailorId is null or "0") {
				return false;
			}

			var type = DynastyId is null ? "dungeon" : "house_arrest";
			characters[jailorId].PrisonerIds.Add(Id, type);
			return true;
		}

		public void ImportUnitsAsMenAtArms(
			IEnumerable<Unit> countryUnits,
			Date date,
			UnitTypeMapper unitTypeMapper,
			IdObjectCollection<string, MenAtArmsType> menAtArmsTypes
		) {
			var locKey = $"IRToCK3_character_{Id}";
			var locBlock = new LocBlock(locKey, ConverterGlobals.PrimaryLanguage) {
				[ConverterGlobals.PrimaryLanguage] = $"[GetPlayer.MakeScope.Var('IRToCK3_character_{Id}').Char.GetID]"
			};
			Localizations.Add(locKey, locBlock);

			var menPerUnitType = new Dictionary<string, int>();
			foreach (var unit in countryUnits) {
				foreach (var (type, menInUnit) in unitTypeMapper.GetMenPerCK3UnitType(unit.MenPerUnitType)) {
					if (menPerUnitType.TryGetValue(type, out var men)) {
						menPerUnitType[type] = men + menInUnit;
					} else {
						menPerUnitType[type] = menInUnit;
					}
				}
			}

			foreach (var (typeId, men) in menPerUnitType) {
				var baseType = menAtArmsTypes[typeId];
				var dedicatedType = new MenAtArmsType(baseType, this, men/8, date);
				menAtArmsTypes.Add(dedicatedType);
				MenAtArmsStacksPerType[dedicatedType.Id] = 1;

				var maaTypeLocBlock = new LocBlock(dedicatedType.Id, ConverterGlobals.PrimaryLanguage) {
					[ConverterGlobals.PrimaryLanguage] = $"${baseType.Id}$"
				};
				Localizations.Add(dedicatedType.Id, maaTypeLocBlock);
			}

			var sb = new StringBuilder();
			sb.AppendLine("{ add_character_modifier=IRToCK3_fuck_CK3_military_system_modifier }");

			History.AddFieldValue(date, "effects", "effect", new StringOfItem(sb.ToString()));
		}
		public void ImportUnitsAsSpecialTroops(
			IEnumerable<Unit> countryUnits,
			Imperator.Characters.CharacterCollection imperatorCharacters,
			Date date,
			UnitTypeMapper unitTypeMapper,
			ProvinceMapper provinceMapper
		) {
			var sb = new StringBuilder();
			sb.AppendLine("{");

			foreach (var unit in countryUnits) {
				var menPerUnitType = unitTypeMapper.GetMenPerCK3UnitType(unit.MenPerUnitType);

				var imperatorLeader = imperatorCharacters[unit.LeaderId];
				var ck3Leader = imperatorLeader.CK3Character;

				sb.AppendLine("\t\tspawn_army={");

				sb.AppendLine("\t\t\tuses_supply=yes");
				sb.AppendLine("\t\t\tinheritable=yes");

				if (unit.LocalizedName is not null) {
					var locKey = unit.LocalizedName.Id;
					sb.AppendLine($"\t\t\tname={locKey}");
					Localizations[locKey] = unit.LocalizedName;
				}

				foreach (var (type, men) in menPerUnitType) {
					sb.AppendLine($"\t\t\tmen_at_arms={{type={type} men={men}}}");
				}

				var ck3Location = provinceMapper.GetCK3ProvinceNumbers(unit.Location)
					.Cast<ulong?>()
					.FirstOrDefault(defaultValue: null);
				if (ck3Location is not null) {
					sb.AppendLine($"\t\t\tlocation=province:{ck3Location}");
					sb.AppendLine($"\t\t\torigin=province:{ck3Location}");
				}

				if (ck3Leader is not null) {
					// Will have no effect if army is not actually spawned (see spawn_army explanation on CK3 wiki).
					sb.AppendLine($"\t\t\tsave_temporary_scope_as={unit.Id}");
				}

				sb.AppendLine("\t\t}");

				if (ck3Leader is not null) {
					sb.AppendLine($"\t\tif={{ limit={{ exists=scope:{unit.Id} }} scope:{unit.Id}={{ set_commander=character:{ck3Leader.Id} }} }}");
				}
			}

			sb.AppendLine("\t}");
			History.AddFieldValue(date, "effects", "effect", new StringOfItem(sb.ToString()));
		}
	}
}
