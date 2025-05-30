﻿using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Localization;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.CommonUtils.Map;
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
using Open.Collections;
using System.Collections.Generic;
using System.Text;
using ZLinq;

namespace ImperatorToCK3.CK3.Characters;

internal sealed class Character : IIdentifiable<string> {
	public string Id { get; }
	public bool FromImperator { get; init; } = false;
	
	// A flag that marks the character as non-removable.
	public bool IsNonRemovable { get; set; } = false;
		
	public bool Female {
		get {
			var entries = History.Fields["female"].InitialEntries;
			if (entries.Count == 0) {
				return false;
			}

			var value = entries.AsValueEnumerable().LastOrDefault().Value;
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
		History.AddFieldValue(date, "give_nickname", "give_nickname", nickname);
	}
	public string? GetNickname(Date date) {
		return History.GetFieldValue("give_nickname", date)?.ToString();
	}

	public List<string> BaseTraits => History.Fields["traits"].InitialEntries.AsValueEnumerable()
		.Where(kvp => kvp.Key == "trait")
		.Select(kvp => kvp.Value)
		.Cast<string>()
		.ToList();

	public void AddBaseTrait(string traitId) {
		History.Fields["traits"].InitialEntries.Add(new KeyValuePair<string, object>("trait", traitId));
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
		get => History.Fields["birth"].DateToEntriesDict.AsValueEnumerable().First().Key;
		set {
			var field = History.Fields["birth"];
			field.RemoveAllEntries();
			field.AddEntryToHistory(value, "birth", true);
		}
	}
		
	public Date? DeathDate {
		get {
			var entriesDict = History.Fields["death"].DateToEntriesDict;
			return entriesDict.Count == 0 ? null : entriesDict.AsValueEnumerable().First().Key;
		}
		set {
			var field = History.Fields["death"];
			field.RemoveAllEntries();
			if (value is not null) {
				field.AddEntryToHistory(value, "death", true);
			}
		}
	}
	public string? DeathReason {
		get {
			var entriesDict = History.Fields["death"].DateToEntriesDict;
			if (entriesDict.Count == 0) {
				return null;
			}
			var deathObj = entriesDict.AsValueEnumerable().First().Value[^1].Value;
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
			var entriesList = entriesDict.AsValueEnumerable().First().Value;
			var lastEntry = entriesList[^1];
			// No reason provided.
			var deathStr = value is null ? "yes" : $"{{ death_reason = {value} }}";
			entriesList[^1] = new KeyValuePair<string, object>(lastEntry.Key, new StringOfItem(deathStr));
		}
	}

	public List<Pregnancy> Pregnancies { get; } = [];

	public Dictionary<string, int> MenAtArmsStacksPerType { get; } = [];

	public Dictionary<string, string> PrisonerIds { get; } = []; // <prisoner id, imprisonment type>

	internal DNA? DNA { get; set; }

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
		.WithSimpleField("prowess", "prowess", null)
		.WithSimpleField("health", "health", null)
		.WithSimpleField("fertility", "fertility", null)
		.WithDiffField("languages", new OrderedSet<string> {"learn_language"}, new OrderedSet<string>())
		.WithLiteralField("learn_language_of_culture", "learn_language_of_culture")
		.WithSimpleField("culture", new OrderedSet<string> {"culture", "set_culture"}, null)
		.WithSimpleField("faith", new OrderedSet<string> { "faith", "religion" }, null)
		.WithSimpleField("government", "change_government", null)
		.WithDiffField("traits", new OrderedSet<string> { "trait", "add_trait" }, new OrderedSet<string> { "remove_trait" })
		.WithLiteralField("trait_xps", "add_trait_xp")
		.WithSimpleField("disallow_random_traits", new OrderedSet<string> {"disallow_random_traits"}, new OrderedSet<string>())
		.WithDiffField("perks", new OrderedSet<string> {"add_perk"}, new OrderedSet<string>())
		.WithSimpleField("dna", "dna", null)
		.WithSimpleField("mother", "mother", null)
		.WithSimpleField("father", "father", null)
		.WithDiffField("spouses", new OrderedSet<string> { "add_spouse", "add_matrilineal_spouse" }, new OrderedSet<string> { "remove_spouse" })
		.WithDiffField("concubines", new OrderedSet<string> { "add_concubine" }, new OrderedSet<string>())
		.WithSimpleField("betrothal", "create_betrothal", null)
		.WithLiteralField("add_character_modifier", "add_character_modifier")
		.WithLiteralField("remove_character_modifier", "remove_character_modifier")
		.WithLiteralField("character_flags", "add_character_flag")
		.WithSimpleField("birth", "birth", null)
		.WithLiteralField("death", "death")
		.WithSimpleField("give_nickname", "give_nickname", null)
		.WithSimpleField("remove_nickname", "remove_nickname", null)
		.WithSimpleField("primary_title", "set_primary_title_to", null)
		.WithSimpleField("capital", "capital", null)
		.WithSimpleField("employer", "employer", null)
		.WithSimpleField("council_position", "give_council_position", null)
		.WithSimpleField("move_to_pool", "move_to_pool", null)
		.WithDiffField("claims", new OrderedSet<string> {"add_pressed_claim", "add_unpressed_claim"}, new OrderedSet<string> {"remove_claim"})
		.WithLiteralField("friends", "set_relation_friend")
		.WithLiteralField("best_friends", "set_relation_best_friend")
		.WithLiteralField("lovers", "set_relation_lover")
		.WithLiteralField("rivals", "set_relation_rival")
		.WithLiteralField("nemesis", "set_relation_nemesis")
		.WithLiteralField("guardian", "set_relation_guardian")
		.WithSimpleField("piety", "add_piety", null)
		.WithSimpleField("prestige", "add_prestige", null)
		.WithLiteralField("secret", "add_secret")
		.WithLiteralField("effects", "effect")
		.WithLiteralField("contract_disease_effect", "contract_disease_effect")
		.WithLiteralField("spawn_army", "spawn_army")
		.WithLiteralField("if", "if")
		.WithSimpleField("sexuality", "sexuality", null)
		.WithLiteralField("domicile", "domicile")
		.WithLiteralField("create_maa_regiment", "create_maa_regiment")
		.WithSimpleField("add_gold", "add_gold", null)
		.WithSimpleField("add_piety_level", "add_piety_level", null)
		.WithSimpleField("add_prestige_level", "add_prestige_level", null)
		.Build();

	public History History { get; } = historyFactory.GetHistory();

	public void InitSpousesCache() {
		var spousesHistoryField = History.Fields["spouses"];
		foreach (var spouseId in spousesHistoryField.InitialEntries.AsValueEnumerable().Select(kvp => kvp.Value.ToString())) {
			if (spouseId is null) {
				continue;
			}
			if (characters.TryGetValue(spouseId, out var spouse)) {
				spousesCache.Add(spouse);
				spouse.spousesCache.Add(this);
			}
		}
		foreach (var entriesList in spousesHistoryField.DateToEntriesDict.Values) {
			foreach (var entry in entriesList) {
				var spouseId = entry.Value.ToString();
				if (spouseId is null) {
					continue;
				}
				if (characters.TryGetValue(spouseId, out var spouse)) {
					spousesCache.Add(spouse);
					spouse.spousesCache.Add(this);
				}
			}
		}
	}

	public void InitConcubinesCache() {
		var concubinesHistoryField = History.Fields["concubines"];
		foreach (var concubineId in concubinesHistoryField.InitialEntries.AsValueEnumerable().Select(kvp => kvp.Value.ToString())) {
			if (concubineId is null) {
				continue;
			}
			if (characters.TryGetValue(concubineId, out var concubine)) {
				concubinesCache.Add(concubine);
				concubine.concubinesCache.Add(this);
			}
		}
		foreach (var entriesList in concubinesHistoryField.DateToEntriesDict.Values) {
			foreach (var entry in entriesList) {
				var concubineId = entry.Value.ToString();
				if (concubineId is null) {
					continue;
				}
				if (characters.TryGetValue(concubineId, out var concubine)) {
					concubinesCache.Add(concubine);
					concubine.concubinesCache.Add(this);
				}
			}
		}
	}

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
		LocDB irLocDB,
		CK3LocDB ck3LocDB,
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
			var impNameLoc = irLocDB.GetLocBlockForKey(name);
			CK3LocBlock ck3NameLoc = ck3LocDB.GetOrCreateLocBlock(name);
			if (impNameLoc is not null) {
				ck3NameLoc.CopyFrom(impNameLoc);
			} else {  // fallback: use unlocalized name as displayed name
				ck3NameLoc[ConverterGlobals.PrimaryLanguage] = name;
			}
		}

		BirthDate = preImperatorRuler.BirthDate!;
		DeathDate = preImperatorRuler.DeathDate!;

		// determine culture and religion
		ulong ck3Province = 0;
		ulong irProvince = 0;
		var srcReligion = preImperatorRuler.Religion ?? imperatorCountry.Religion;
		var srcCulture = preImperatorRuler.Culture ?? imperatorCountry.PrimaryCulture;
		if (imperatorCountry.CapitalProvinceId is not null) {
			irProvince = imperatorCountry.CapitalProvinceId.Value;
			var ck3Provinces = provinceMapper.GetCK3ProvinceNumbers(irProvince);
			if (ck3Provinces.Count > 0) {
				ck3Province = ck3Provinces[0];
			}
		}

		if (srcCulture is not null) {
			var cultureMatch = cultureMapper.Match(srcCulture, ck3Province, irProvince, imperatorCountry.HistoricalTag);
			if (cultureMatch is not null) {
				SetCultureId(cultureMatch, null);
			}
		}

		if (srcReligion is not null) {
			var faithMatch = religionMapper.Match(
				srcReligion, 
				GetCultureId(config.CK3BookmarkDate), 
				ck3Province, 
				irProvince, 
				imperatorCountry.HistoricalTag, 
				config
			);
			if (faithMatch is not null) {
				SetFaithId(faithMatch, null);
			}
		}
			
		var nickname = nicknameMapper.GetCK3NicknameForImperatorNickname(preImperatorRuler.Nickname);
		if (nickname is not null) {
			SetNickname(nickname, DeathDate);
		}
	}

	internal Character(
		Imperator.Characters.Character impCharacter,
		CharacterCollection characters,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		TraitMapper traitMapper,
		NicknameMapper nicknameMapper,
		LocDB irLocDB,
		CK3LocDB ck3LocDB,
		MapData irMapData,
		ProvinceMapper provinceMapper,   // used to determine ck3 province for religion mapper
		DeathReasonMapper deathReasonMapper,
		DNAFactory dnaFactory,
		Date dateOnConversion,
		Configuration config,
		ISet<string> unlocalizedImperatorNames
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
			
			var ck3NameLocBlock = ck3LocDB.GetOrCreateLocBlock(name);
			foreach (var language in ConverterGlobals.SupportedLanguages) {
				ck3NameLocBlock[language] = loc;
			}
		} else {
			var nameLoc = ImperatorCharacter.Name;
			var name = nameLoc.Replace(' ', '_');
			SetName(name, null);
			if (!string.IsNullOrEmpty(name)) {
				var ck3NameLocBlock = ck3LocDB.GetOrCreateLocBlock(name);
				var matchedLocBlock = irLocDB.GetLocBlockForKey(name);
				if (matchedLocBlock is not null) {
					ck3NameLocBlock.CopyFrom(matchedLocBlock);
				} else {  // fallback: use unlocalized name as displayed name
					unlocalizedImperatorNames.Add(name);
					ck3NameLocBlock[ConverterGlobals.PrimaryLanguage] = nameLoc;
				}
			}
		}

		Female = ImperatorCharacter.Female;

		// Determine valid (not dropped in province mappings) "source I:R province" and "source CK3 province"
		// to be used by religion mapper. Don't give up without a fight.
		ulong? irProvinceId = ImperatorCharacter.GetSourceLandProvince(irMapData);
		
		var irProvIdForProvMapper = irProvinceId;
		if (IsImperatorProvIdInvalidForCharacterSource(irProvIdForProvMapper, provinceMapper) && ImperatorCharacter.Father is not null) {
			irProvIdForProvMapper = ImperatorCharacter.Father.ProvinceId;
		}
		if (IsImperatorProvIdInvalidForCharacterSource(irProvIdForProvMapper, provinceMapper) && ImperatorCharacter.Mother is not null) {
			irProvIdForProvMapper = ImperatorCharacter.Mother.ProvinceId;
		}
		if (IsImperatorProvIdInvalidForCharacterSource(irProvIdForProvMapper, provinceMapper) && ImperatorCharacter.Spouses.Count > 0) {
			var firstSpouse = ImperatorCharacter.Spouses.AsValueEnumerable().First().Value;
			irProvIdForProvMapper = firstSpouse.ProvinceId;
		}

		var ck3ProvinceNumbers = irProvIdForProvMapper.HasValue ? provinceMapper.GetCK3ProvinceNumbers(irProvIdForProvMapper.Value) : [];
		ulong? ck3ProvinceId = ck3ProvinceNumbers.Count > 0 ? ck3ProvinceNumbers[0] : null;

		var cultureMatch = cultureMapper.Match(
			ImperatorCharacter.Culture,
			ck3ProvinceId,
			irProvinceId,
			ImperatorCharacter.Country?.HistoricalTag
		);
		if (cultureMatch is null) {
			Logger.Warn($"Could not determine CK3 culture for Imperator character {ImperatorCharacter.Id}" +
			            $" with culture {ImperatorCharacter.Culture}!");
		} else {
			SetCultureId(cultureMatch, null);
		}

		var faithMatch = religionMapper.Match(
			ImperatorCharacter.Religion,
			GetCultureId(dateOnConversion),
			ck3ProvinceId, 
			irProvinceId,
			ImperatorCharacter.HomeCountry?.HistoricalTag,
			config
		);
		if (faithMatch is not null) {
			SetFaithId(faithMatch, null);
		}

		// Determine character attributes.
		History.AddFieldValue(null, "diplomacy", "diplomacy", ImperatorCharacter.Attributes.Charisma);
		History.AddFieldValue(null, "martial", "martial", ImperatorCharacter.Attributes.Martial);
		History.AddFieldValue(null, "stewardship", "stewardship", ImperatorCharacter.Attributes.Finesse);
		var intrigue = (ImperatorCharacter.Attributes.Finesse + ImperatorCharacter.Attributes.Charisma) / 2;
		History.AddFieldValue(null, "intrigue", "intrigue", intrigue);
		History.AddFieldValue(null, "learning", "learning", ImperatorCharacter.Attributes.Zeal);

		if (impCharacter.Fertility.HasValue) {
			History.AddFieldValue(null, "fertility", "fertility", impCharacter.Fertility.Value);
		}

		if (impCharacter.Health is not null) {
			// In I:R, health is a value between 0 and 100, with 100 being the best.
			// In CK3, 0 means near death, ≥ 7 means excellent health.
			// https://imperator.paradoxwikis.com/Characters#Secondary
			// https://ck3.paradoxwikis.com/Attributes#Health
			var ck3Health = impCharacter.Health.Value / 10;
			History.AddFieldValue(null, "health", "health", ck3Health);
		}

		foreach (var traitId in traitMapper.GetCK3TraitsForImperatorTraits(ImperatorCharacter.Traits)) {
			AddBaseTrait(traitId);
		}

		BirthDate = ImperatorCharacter.BirthDate;
		DeathDate = ImperatorCharacter.DeathDate;
		var impDeathReason = ImperatorCharacter.DeathReason;
		if (impDeathReason is not null) {
			DeathReason = deathReasonMapper.GetCK3ReasonForImperatorReason(impDeathReason);
		}

		var nicknameMatch = nicknameMapper.GetCK3NicknameForImperatorNickname(ImperatorCharacter.Nickname);
		if (nicknameMatch is not null) {
			var nicknameDate = ImperatorCharacter.DeathDate ?? dateOnConversion;
			SetNickname(nicknameMatch, nicknameDate);
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

	private static bool IsImperatorProvIdInvalidForCharacterSource(ulong? impProvForProvinceMapper, ProvinceMapper provinceMapper) {
		return !impProvForProvinceMapper.HasValue || provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper.Value).Count == 0;
	}

	public void SetCultureId(string cultureId, Date? date) {
		History.AddFieldValue(date, "culture", "culture", cultureId);
	}
	public string? GetCultureId(Date date) {
		return History.GetFieldValue("culture", date)?.ToString()?.RemQuotes();
	}
		
	public void SetFaithId(string faithId, Date? date) {
		History.AddFieldValue(date, "faith", "faith", faithId);
	}
	public string? GetFaithId(Date date) {
		return History.GetFieldValue("faith", date)?.ToString();
	}

	public OrderedSet<object> GetSpouseIds(Date date) {
		return History.GetFieldValueAsCollection("spouses", date) ?? new OrderedSet<object>();
	}
	public void AddSpouse(Date date, Character spouse) {
		History.AddFieldValue(date, "spouses", "add_spouse", spouse.Id);
		spousesCache.Add(spouse);
		spouse.spousesCache.Add(this);
	}
	private void RemoveSpouse(string spouseId) {
		if (History.Fields.TryGetValue("spouses", out var spousesHistory)) {
			spousesHistory.RemoveAllEntries(value => (value.ToString() ?? string.Empty).Equals(spouseId));
		}
		spousesCache.RemoveWhere(c => c.Id == spouseId);
	}
	public void RemoveAllSpouses() {
		foreach (var spouse in spousesCache) {
			spouse.RemoveSpouse(Id);
		}
	}

	private void RemoveConcubine(string concubineId) {
		if (History.Fields.TryGetValue("concubines", out var concubinesHistory)) {
			concubinesHistory.RemoveAllEntries(value => (value.ToString() ?? string.Empty).Equals(concubineId));
		}
		concubinesCache.RemoveWhere(c => c.Id == concubineId);
	}
	public void RemoveAllConcubines() {
		foreach (var concubine in concubinesCache) {
			concubine.RemoveConcubine(Id);
		}
	}

	public void RemoveAllChildren() {
		if (Female) {
			foreach (var child in childrenCache.AsValueEnumerable().Where(c => c.MotherId == Id)) {
				child.Mother = null;
			}
		} else {
			foreach (var child in childrenCache.AsValueEnumerable().Where(c => c.FatherId == Id)) {
				child.Father = null;
			}
		}
	}

	public void UpdateChildrenCacheOfParents() {
		Father?.childrenCache.Add(this);
		Mother?.childrenCache.Add(this);
	}

	public string? MotherId {
		get {
			var field = History.Fields["mother"];
			var entries = field.InitialEntries;
			if (entries.Count == 0) {
				return null;
			}

			var idObj = entries[^1].Value;
			var idStr = idObj.ToString();
			if (idStr is null) {
				Logger.Warn($"Mother ID string is null! Original value: {idObj}");
				return null;
			}

			if (!idStr.IsQuoted()) {
				return idStr;
			}

			idStr = idStr.RemQuotes();
			field.RemoveAllEntries();
			field.AddEntryToHistory(null, "mother", idStr);
			return idStr;
		}
	}
	public Character? Mother {
		get {
			var motherId = MotherId;
			if (motherId is null) {
				return null;
			}
				
			if (characters.TryGetValue(motherId, out var mother)) {
				return mother;
			}
			Logger.Debug($"Character {Id}'s mother {motherId} does not exist! Removing broken link.");
			History.Fields["mother"].RemoveAllEntries();
			return null;
		}
		set {
			History.Fields["mother"].RemoveAllEntries();
			if (value is not null) {
				History.AddFieldValue(null, "mother", "mother", value.Id);
				value.childrenCache.Add(this);
			}
		}
	}

	public string? FatherId {
		get {
			var field = History.Fields["father"];
			var entries = field.InitialEntries;
			if (entries.Count == 0) {
				return null;
			}
				
			var idObj = entries[^1].Value;
			var idStr = idObj.ToString();
			if (idStr is null) {
				Logger.Warn($"Father ID string is null! Original value: {idObj}");
				return null;
			}
				
			if (!idStr.IsQuoted()) {
				return idStr;
			}
				
			idStr = idStr.RemQuotes();
			field.RemoveAllEntries();
			field.AddEntryToHistory(null, "father", idStr);
			return idStr;
		}
	}
	public Character? Father {
		get {
			var fatherId = FatherId;
			if (fatherId is null) {
				return null;
			}

			if (characters.TryGetValue(fatherId, out var father)) {
				return father;
			}
			Logger.Debug($"Character {Id}'s father {fatherId} does not exist! Removing broken link.");
			History.Fields["father"].RemoveAllEntries();
			return null;
		}
		set {
			History.Fields["father"].RemoveAllEntries();
			if (value is not null) {
				History.AddFieldValue(null, "father", "father", value.Id);
				value.childrenCache.Add(this);
			}
		}
	}

	public IReadOnlyCollection<Character> Children => characters.AsValueEnumerable()
		.Where(c => c.FatherId == Id || c.MotherId == Id)
		.ToImmutableList();

	public void SetDynastyId(string dynastyId, Date? date) {
		History.AddFieldValue(date, "dynasty", "dynasty", dynastyId);
	}
	public string? GetDynastyId(Date date) {
		return History.GetFieldValue("dynasty", date)?.ToString();
	}
	
	public void ClearDynastyHouse() {
		History.Fields["dynasty_house"].RemoveAllEntries();
	}
	public string? GetDynastyHouseId(Date date) {
		return History.GetFieldValue("dynasty_house", date)?.ToString();
	}
	public void SetDynastyHouseId(string dynastyHouseId, Date? date) {
		History.AddFieldValue(date, "dynasty_house", "dynasty_house", dynastyHouseId);
	}

	private string? jailorId;
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

	internal void ImportUnitsAsMenAtArms(
		IEnumerable<Unit> countryUnits,
		Date date,
		UnitTypeMapper unitTypeMapper,
		IdObjectCollection<string, MenAtArmsType> menAtArmsTypes,
		CK3LocDB ck3LocDB
	) {
		var locKey = $"IRToCK3_character_{Id}";
		var locBlock = ck3LocDB.GetOrCreateLocBlock(locKey);
		locBlock[ConverterGlobals.PrimaryLanguage] = $"[GetPlayer.MakeScope.Var('IRToCK3_character_{Id}').Char.GetID]";

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

			var maaTypeLocBlock = ck3LocDB.GetOrCreateLocBlock(dedicatedType.Id);
			maaTypeLocBlock[ConverterGlobals.PrimaryLanguage] = $"${baseType.Id}$";
		}

		var sb = new StringBuilder();
		sb.AppendLine("{ add_character_modifier=IRToCK3_fuck_CK3_military_system_modifier }");

		History.AddFieldValue(date, "effects", "effect", new StringOfItem(sb.ToString()));
	}
	internal void ImportUnitsAsSpecialTroops(
		IEnumerable<Unit> countryUnits,
		Imperator.Characters.CharacterCollection imperatorCharacters,
		Date date,
		UnitTypeMapper unitTypeMapper,
		ProvinceMapper provinceMapper,
		CK3LocDB ck3LocDB
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
				var unitLocBlock = ck3LocDB.GetOrCreateLocBlock(locKey);
				unitLocBlock.CopyFrom(unit.LocalizedName);
			}

			foreach (var (type, men) in menPerUnitType) {
				sb.AppendLine($"\t\t\tmen_at_arms={{type={type} men={men}}}");
			}

			var ck3Location = provinceMapper.GetCK3ProvinceNumbers(unit.Location).AsValueEnumerable()
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

	private readonly CharacterCollection characters;
	private readonly ConcurrentHashSet<Character> spousesCache = [];
	private readonly ConcurrentHashSet<Character> concubinesCache = [];
	private readonly HashSet<Character> childrenCache = new();
}