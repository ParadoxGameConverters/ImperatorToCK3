using System.Collections.Generic;
using System.Linq;
using commonItems;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;

namespace ImperatorToCK3.CK3.Characters {
	public class Character {
		public string ID { get; private set; } = "0";
		public bool Female { get; private set; } = false;
		public string Culture { get; private set; } = string.Empty;
		public string Religion { get; private set; } = string.Empty;
		public string Name { get; private set; } = string.Empty;
		public string? Nickname { get; private set; }

		public uint Age { get; private set; } = 0; // used when option to convert character age is chosen
		public Date BirthDate { get; private set; } = new Date(1, 1, 1);
		public Date? DeathDate { get; private set; }
		public string? DeathReason { get; private set; }

		public SortedSet<string> Traits { get; private set; } = new();
		public Dictionary<string, LocBlock> Localizations { get; private set; } = new();

		public Imperator.Characters.Character? ImperatorCharacter;
		public void InitializeFromImperator(Imperator.Characters.Character impCharacter,
											ReligionMapper religionMapper,
											CultureMapper cultureMapper,
											TraitMapper traitMapper,
											NicknameMapper nicknameMapper,
											LocalizationMapper localizationMapper,
											ProvinceMapper provinceMapper,   // used to determine ck3 province for religion mapper
											DeathReasonMapper deathReasonMapper,
											bool ConvertBirthAndDeathDates = true
		) {
			var DateOnConversion = new Date(867, 1, 1); // TODO: FIX THIS
			ImperatorCharacter = impCharacter;
			ID = "imperator" + ImperatorCharacter.ID.ToString();
			Name = ImperatorCharacter.Name;
			Female = ImperatorCharacter.Female;
			Age = ImperatorCharacter.Age;


			ulong ck3Province;  // for religion mapper

			// Determine valid (not dropped in province mappings) "source province" to be used by religion mapper. Don't give up without a fight.
			var impProvForProvinceMapper = ImperatorCharacter.ProvinceID;
			if (provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper).Count == 0 && ImperatorCharacter.Father.Value is not null) {
				impProvForProvinceMapper = ImperatorCharacter.Father.Value.ProvinceID;
			}

			if (provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper).Count == 0 && ImperatorCharacter.Mother.Value is not null) {
				impProvForProvinceMapper = ImperatorCharacter.Mother.Value.ProvinceID;
			}

			if (provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper).Count == 0 && ImperatorCharacter.Spouses.Count > 0) {
				var firstSpouse = ImperatorCharacter.Spouses.First().Value;
				if (firstSpouse is not null) {
					impProvForProvinceMapper = firstSpouse.ProvinceID;
				}
			}

			var ck3ProvinceNumbers = provinceMapper.GetCK3ProvinceNumbers(impProvForProvinceMapper);
			if (ck3ProvinceNumbers.Count == 0) {
				ck3Province = 0;
			} else {
				ck3Province = ck3ProvinceNumbers[0];
			}

			var match = religionMapper.Match(ImperatorCharacter.Religion, ck3Province, ImperatorCharacter.ProvinceID);
			if (match is not null) {
				Religion = match;
			}

			match = cultureMapper.Match(ImperatorCharacter.Culture, Religion, ck3Province, ImperatorCharacter.ProvinceID, "");
			if (match is not null) {
				Culture = match;
			}

			if (!string.IsNullOrEmpty(Name)) {
				var impNameLoc = localizationMapper.GetLocBlockForKey(Name);
				if (impNameLoc is not null) {
					Localizations.Add(Name, impNameLoc);
				} else {  // fallback: use unlocalized name as displayed name
					Localizations.Add(Name, new LocBlock {
						english = Name,
						french = Name,
						german = Name,
						russian = Name,
						simp_chinese = Name,
						spanish = Name
					});
				}
			}

			foreach (var impTrait in ImperatorCharacter.Traits) {
				var traitMatch = traitMapper.GetCK3TraitForImperatorTrait(impTrait);
				if (traitMatch is not null) {
					Traits.Add(traitMatch);
				}
			}

			if (ImperatorCharacter.Nickname is not null) {
				var nicknameMatch = nicknameMapper.GetCK3NicknameForImperatorNickname(ImperatorCharacter.Nickname);
				if (nicknameMatch is not null) {
					Nickname = nicknameMatch;
				}
			}

			BirthDate = ImperatorCharacter.BirthDate;
			DeathDate = ImperatorCharacter.DeathDate;
			var impDeathReason = ImperatorCharacter.DeathReason;
			if (impDeathReason is not null) {
				DeathReason = deathReasonMapper.GetCK3ReasonForImperatorReason(impDeathReason);
			}
			if (!ConvertBirthAndDeathDates) {  // if option to convert character age is chosen
				BirthDate.AddYears((int)new Date(867, 1, 1).DiffInYears(DateOnConversion));
				DeathDate?.AddYears((int)new Date(867, 1, 1).DiffInYears(DateOnConversion));
			}
		}

		public void BreakAllLinks() {
			mother.Value?.RemoveChild(ID);
			RemoveMother();
			father.Value?.RemoveChild(ID);
			RemoveFather();
			foreach (var spouse in spouses.Values) {
				spouse.RemoveSpouse(ID);
			}
			spouses.Clear();
			if (Female) {
				foreach (var child in children.Values) {
					child.RemoveMother();
				}
			} else {
				foreach (var child in children.Values) {
					child.RemoveFather();
				}
			}
			children.Clear();

			if (ImperatorCharacter is not null) {
				ImperatorCharacter.CK3Character = null;
				ImperatorCharacter = null;
			}
		}

		private void RemoveSpouse(string spouseID) {
			spouses.Remove(spouseID);
		}

		private void RemoveFather() {
			father = new("0", null);
		}

		private void RemoveMother() {
			mother = new("0", null);
		}

		private void RemoveChild(string childID) {
			children.Remove(childID);
		}

		private KeyValuePair<string, Character?> mother = new();
		private KeyValuePair<string, Character?> father = new();
		private readonly Dictionary<string, Character?> children = new();
		private readonly Dictionary<string, Character?> spouses = new();

		private string? dynastyID; // not always set
	}
}
