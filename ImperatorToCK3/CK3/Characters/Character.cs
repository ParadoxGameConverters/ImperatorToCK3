using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Exceptions;
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
		
		public bool Female {
			get {
				var entries = History.Fields["female"].InitialEntries;
				if (entries.Count == 0) {
					return false;
				}

				var value = entries.LastOrDefault().Value;
				if (value is string str) {
					return str == "yes";
				}
				return (bool)value;
			}
			init {
				History.AddFieldValue(null, "female", "female", value);
			}
		}

		public void SetName(string name, Date? date) {
			History.AddFieldValue(date, "name", "name", name);
		}
		public string? GetName(Date date) {
			return History.GetFieldValue("name", date)?.ToString();
		}
		
		public void SetNickname(string nickname, Date? date) {
			var deathDate = DeathDate;
			// Date should not be given later than death date.
			if (deathDate is not null && date is not null && date > deathDate) {
				date = deathDate;
			}
			History.AddFieldValue(date, "nickname", "give_nickname", nickname);
		}
		public string? GetNickname(Date date) {
			return History.GetFieldValue("nickname", date)?.ToString();
		}
		
		public double? Gold { get; set; }

		public uint GetAge(Date date) {
			var birthDate = BirthDate;
			var deathDate = DeathDate;
			if (deathDate is null) {
				return (uint)date.DiffInYears(birthDate);
			}
			return (uint)deathDate.DiffInYears(birthDate);
		}
		public string GetAgeSex(Date date) {
			if (GetAge(date) >= 16) {
				return Female ? "female" : "male";
			}
			return Female ? "girl" : "boy";
		}

		public Date BirthDate {
			get => History.Fields["birth"].DateToEntriesDict.First().Key;
			init {
				var field = History.Fields["birth"];
				field.RemoveAllEntries();
				field.AddEntryToHistory(value, "birth", true);
			}
		}
		
		public Date? DeathDate {
			get {
				var entriesDict = History.Fields["death"].DateToEntriesDict;
				return entriesDict.Count == 0 ? null : entriesDict.First().Key;
			}
			init {
				var field = History.Fields["death"];
				field.RemoveAllEntries();
				field.AddEntryToHistory(value, "death", true);
			}
		}
		public string? DeathReason {
			get {
				var entriesDict = History.Fields["death"].DateToEntriesDict;
				if (entriesDict.Count == 0) {
					return null;
				}
				var deathObj = entriesDict.First().Value.Last().Value;
				if (deathObj is not StringOfItem deathStrOfItem || !deathStrOfItem.IsArrayOrObject()) {
					return null;
				}

				var deathObjParser = new Parser();
				string? deathReason = null;
				deathObjParser.RegisterKeyword("death_reason", reader => {
					deathReason = reader.GetString();
				});
				deathObjParser.IgnoreUnregisteredItems();
				deathObjParser.ParseStream(new BufferedReader(deathStrOfItem.ToString()));
				return deathReason;
			}
			init {
				var entriesDict = History.Fields["death"].DateToEntriesDict;
				if (entriesDict.Count == 0) {
					throw new ConverterException($"Character {Id} has no death date set!");
				}
				
				// Modify the last entry in the history to include the death reason.
				var entriesList = entriesDict.First().Value;
				var lastEntry = entriesList.Last();
				var newEntry = new KeyValuePair<string, object>(lastEntry.Key, new StringOfItem($"{{ death_reason = {value} }}"));
				entriesList[^1] = newEntry;
			}
		}

		public bool Dead => DeathDate is not null;
		public List<Pregnancy> Pregnancies { get; } = new();

		public Dictionary<string, int> MenAtArmsStacksPerType { get; } = new();

		public Dictionary<string, string> PrisonerIds { get; } = new(); // <prisoner id, imprisonment type>
		public Dictionary<string, LocBlock> Localizations { get; } = new();

		public Imperator.Characters.Character? ImperatorCharacter { get; set; }

		private static readonly HistoryFactory historyFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithSimpleField("name", "name", null)
			.WithSimpleField("female", "female", null)
			.WithSimpleField("dynasty", "dynasty", null)
			.WithSimpleField("dynasty_house", "dynasty_house", null)
			.WithSimpleField("diplomacy", "diplomacy", null)
			.WithSimpleField("martial", "martial", null)
			.WithSimpleField("stewardship", "stewardship", null)
			.WithSimpleField("intrigue", "intrigue", null)
			.WithSimpleField("learning", "learning", null)
			.WithSimpleField("culture", "culture", null)
			.WithSimpleField("faith", new OrderedSet<string> { "faith", "religion" }, null)
			.WithDiffField("traits", new() { "trait", "add_trait" }, new OrderedSet<string> { "remove_trait" })
			.WithSimpleField("dna", "dna", null)
			.WithSimpleField("mother", "mother", null)
			.WithSimpleField("father", "father", null)
			.WithDiffField("spouses", new OrderedSet<string> { "add_spouse", "add_matrilineal_spouse" }, new OrderedSet<string> { "remove_spouse" })
			.WithDiffField("effects", new OrderedSet<string> { "effect" }, new OrderedSet<string>())
			.WithDiffField("character_modifiers", "add_character_modifier", "remove_character_modifier")
			.WithDiffField("character_flags", new OrderedSet<string> {"add_character_flag"}, new OrderedSet<string>())
			.WithSimpleField("birth", "birth", null)
			.WithSimpleField("death", "death", null)
			.WithSimpleField("nickname", "give_nickname", null)
			.WithSimpleField("employer", "employer", null)
			.WithDiffField("claims", "add_pressed_claim", "remove_claim")
			.WithDiffField("lovers", new OrderedSet<string> {"set_relation_lover"}, new OrderedSet<string>())
			.WithDiffField("rivals", new OrderedSet<string> {"set_relation_rival"}, new OrderedSet<string>())
			.Build();
		public History History { get; } = historyFactory.GetHistory();

		public Character(string id, BufferedReader reader, CharacterCollection characters) {
			this.characters = characters;
			
			Id = id;
			History = historyFactory.GetHistory(reader);
		}
		public Character(string id, string name, Date birthDate, CharacterCollection characters) {
			this.characters = characters;
			
			Id = id;
			SetName(name, null);
			BirthDate = birthDate;
		}
		public Character(
			RulerTerm.PreImperatorRulerInfo preImperatorRuler,
			Date rulerTermStart,
			Country imperatorCountry,
			CharacterCollection characters,
			LocDB locDB,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			ProvinceMapper provinceMapper,
			Configuration config
		) {
			this.characters = characters;
			
			Id = $"imperatorRegnal{imperatorCountry.Tag}{preImperatorRuler.Name}{rulerTermStart.ToString()[1..]}BC".Replace('.', '_');
			FromImperator = true;
			var name = preImperatorRuler.Name ?? Id;
			SetName(name, null);
			if (!string.IsNullOrEmpty(name)) {
				var impNameLoc = locDB.GetLocBlockForKey(name);
				if (impNameLoc is not null) {
					Localizations.Add(name, impNameLoc);
				} else {  // fallback: use unlocalized name as displayed name
					Localizations.Add(name, new LocBlock(name, ConverterGlobals.PrimaryLanguage) {
						[ConverterGlobals.PrimaryLanguage] = name,
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
				var faithMatch = religionMapper.Match(srcReligion, ck3Province, impProvince, imperatorCountry.HistoricalTag, config);
				if (faithMatch is not null) {
					SetFaithId(faithMatch, null);
				}
			}

			if (srcCulture is not null) {
				var cultureMatch = cultureMapper.Match(srcCulture, GetFaithId(config.CK3BookmarkDate) ?? string.Empty, ck3Province, impProvince, imperatorCountry.HistoricalTag);
				if (cultureMatch is not null) {
					SetCultureId(cultureMatch, null);
				}
			}
			
			var nickname = nicknameMapper.GetCK3NicknameForImperatorNickname(preImperatorRuler.Nickname);
			if (nickname is not null) {
				SetNickname(nickname, DeathDate);
			}
		}

		public Character(
			Imperator.Characters.Character impCharacter,
			CharacterCollection characters,
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
			this.characters = characters;
			
			ImperatorCharacter = impCharacter;
			ImperatorCharacter.CK3Character = this;
			Id = "imperator" + ImperatorCharacter.Id;
			FromImperator = true;

			if (!string.IsNullOrEmpty(ImperatorCharacter.CustomName)) {
				var loc = ImperatorCharacter.CustomName;
				var locKey = CommonFunctions.NormalizeUTF8Path(loc.FoldToASCII().Replace(' ', '_'));
				var name = $"IRTOCK3_CUSTOM_NAME_{locKey}";
				SetName(name, null);

				var locBlock = new LocBlock(name, ConverterGlobals.PrimaryLanguage) {
					[ConverterGlobals.PrimaryLanguage] = loc
				};
				Localizations.Add(name, locBlock);
			} else {
				var nameLoc = ImperatorCharacter.Name;
				var name = nameLoc.Replace(' ', '_');
				SetName(name, null);
				if (!string.IsNullOrEmpty(name)) {
					var matchedLocBlock = locDB.GetLocBlockForKey(name);
					if (matchedLocBlock is not null) {
						Localizations.Add(name, matchedLocBlock);
					} else {  // fallback: use unlocalized name as displayed name
						var locBlock = new LocBlock(name, ConverterGlobals.PrimaryLanguage) {
							[ConverterGlobals.PrimaryLanguage] = nameLoc
						};
						Localizations.Add(name, locBlock);
					}
				}
			}

			Female = ImperatorCharacter.Female;

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
				SetFaithId(match, null);
			}

			match = cultureMapper.Match(
				ImperatorCharacter.Culture,
				GetFaithId(dateOnConversion) ?? string.Empty,
				ck3Province,
				ImperatorCharacter.ProvinceId,
				ImperatorCharacter.Country?.HistoricalTag ?? string.Empty
			);
			if (match is null) {
				Logger.Warn($"Could not determine CK3 culture for Imperator character {ImperatorCharacter.Id}" +
							$" with culture {ImperatorCharacter.Culture}!");
			} else {
				SetCultureId(match, null);
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

			BirthDate = ImperatorCharacter.BirthDate;
			DeathDate = ImperatorCharacter.DeathDate;
			var impDeathReason = ImperatorCharacter.DeathReason;
			if (impDeathReason is not null) {
				DeathReason = deathReasonMapper.GetCK3ReasonForImperatorReason(impDeathReason);
			}

			var nicknameMatch = nicknameMapper.GetCK3NicknameForImperatorNickname(ImperatorCharacter.Nickname);
			if (nicknameMatch is not null) {
				SetNickname(nicknameMatch, dateOnConversion);
			}

			if (ImperatorCharacter.Wealth != 0) {
				Gold = ImperatorCharacter.Wealth * config.ImperatorCurrencyRate;
			}

			// If character is imprisoned, set jailor.
			SetJailor();
			SetEmployerFromImperator();

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

			void SetEmployerFromImperator() {
				var prisonerHome = ImperatorCharacter.PrisonerHome;
				var homeCountry = ImperatorCharacter.HomeCountry;
				if (prisonerHome?.CK3Title is not null) { // is imprisoned
					SetEmployerId(prisonerHome.CK3Title.GetHolderId(dateOnConversion), null);
				} else if (homeCountry?.CK3Title is not null) {
					SetEmployerId(homeCountry.CK3Title.GetHolderId(dateOnConversion), null);
				}
			}
		}
		
		public void SetCultureId(string cultureId, Date? date) {
			History.AddFieldValue(date, "culture", "culture", cultureId);
		}
		public string? GetCultureId(Date date) {
			return History.GetFieldValue("culture", date)?.ToString();
		}
		
		public void SetFaithId(string faithId, Date? date) {
			History.AddFieldValue(date, "faith", "faith", faithId);
		}
		public string? GetFaithId(Date date) {
			return History.GetFieldValue("faith", date)?.ToString();
		}

		public void BreakAllLinks() {
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

		public string? MotherId {
			get {
				var entries = History.Fields["mother"].InitialEntries;
				return entries.Count == 0 ? null : entries.Last().Value.ToString();
			}
			private set {
				if (value is null) {
					History.Fields["mother"].RemoveAllEntries();
				} else {
					History.AddFieldValue(null, "mother", "mother", value);
				}
			}
		}
		public Character? Mother {
			get {
				var motherId = MotherId;
				return motherId is null ? null : characters[motherId];
			}
			set {
				MotherId = value?.Id;
			}
		}

		public string? FatherId {
			get {
				var entries = History.Fields["father"].InitialEntries;
				return entries.Count == 0 ? null : entries.Last().Value.ToString();
			}
			private set {
				if (value is null) {
					History.Fields["father"].RemoveAllEntries();
				} else {
					History.AddFieldValue(null, "father", "father", value);
				}
			}
		}
		public Character? Father {
			get {
				var fatherId = FatherId;
				return fatherId is null ? null : characters[fatherId];
			}
			set {
				FatherId = value?.Id;
			}
		}
		
		public Dictionary<string, Character?> Children { get; set; } = new();

		public void SetDynastyId(string dynastyId, Date? date) {
			History.AddFieldValue(date, "dynasty", "dynasty", dynastyId);
		}
		public string? GetDynastyId(Date date) {
			return History.GetFieldValue("dynasty", date)?.ToString();
		}

		private string? jailorId;
		private readonly HashSet<Character> spousesCache = new();
		public void SetEmployer(Character employer, Date? date) {
			SetEmployerId(employer.Id, date);
		}
		private void SetEmployerId(string employerId, Date? date) {
			History.AddFieldValue(date, "employer", "employer", employerId);
		}
		public string? GetEmployerId(Date date) {
			return History.GetFieldValue("employer", date)?.ToString();
		}

		public bool LinkJailor(Date date) {
			if (jailorId is null or "0") {
				return false;
			}

			var type = GetDynastyId(date) is null ? "dungeon" : "house_arrest";
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
		
		private CharacterCollection characters;
	}
}
