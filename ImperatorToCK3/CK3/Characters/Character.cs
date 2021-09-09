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

		public SortedSet<string> Traits { get; } = new();
		public Dictionary<string, LocBlock> Localizations { get; } = new();

		public Imperator.Characters.Character? ImperatorCharacter { get; set; }

		public void InitializeFromImperator(
			Imperator.Characters.Character impCharacter,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			TraitMapper traitMapper,
			NicknameMapper nicknameMapper,
			LocalizationMapper localizationMapper,
			ProvinceMapper provinceMapper,   // used to determine ck3 province for religion mapper
			DeathReasonMapper deathReasonMapper,
			bool convertBirthAndDeathDates,
			Date dateOnConversion,
			Date ck3BookmarkDate
		) {
			ImperatorCharacter = impCharacter;
			ImperatorCharacter.CK3Character = this;
			ID = "imperator" + ImperatorCharacter.ID.ToString();

			if (!string.IsNullOrEmpty(ImperatorCharacter.CustomName)) {
				var loc = ImperatorCharacter.CustomName;
				Name = "IMPTOCK3_CUSTOM_NAME_" + loc.Replace(' ', '_');
				Localizations.Add(Name, new LocBlock {
					english = loc,
					french = loc,
					german = loc,
					russian = loc,
					simp_chinese = loc,
					spanish = loc
				});
			} else {
				Name = ImperatorCharacter.Name;
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
			}

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

			var ck3Owner = "";
			if (ImperatorCharacter.Country is not null) {
				var imperatorCountry = ImperatorCharacter.Country.Value.Value;
				if (imperatorCountry?.CK3Title is not null) {
					ck3Owner = imperatorCountry.CK3Title.Name;
				}
			}
			match = cultureMapper.Match(
				ImperatorCharacter.Culture,
				Religion, ck3Province,
				ImperatorCharacter.ProvinceID,
				ck3Owner
			);
			if (match is not null) {
				Culture = match;
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
			if (!convertBirthAndDeathDates) {  // if option to convert character age is chosen
				BirthDate.ChangeByYears((int)ck3BookmarkDate.DiffInYears(dateOnConversion));
				DeathDate?.ChangeByYears((int)ck3BookmarkDate.DiffInYears(dateOnConversion));
			}
		}

		public void BreakAllLinks() {
			Mother?.RemoveChild(ID);
			RemoveMother();
			Father?.RemoveChild(ID);
			RemoveFather();
			foreach (var spouse in Spouses.Values) {
				spouse.RemoveSpouse(ID);
			}
			Spouses.Clear();
			if (Female) {
				foreach (var child in Children.Values) {
					child.RemoveMother();
				}
			} else {
				foreach (var child in Children.Values) {
					child.RemoveFather();
				}
			}
			Children.Clear();

			if (ImperatorCharacter is not null) {
				ImperatorCharacter.CK3Character = null;
				ImperatorCharacter = null;
			}
		}

		private void RemoveSpouse(string spouseID) {
			Spouses.Remove(spouseID);
		}

		private void RemoveFather() {
			Father = null;
		}

		private void RemoveMother() {
			Mother = null;
		}

		private void RemoveChild(string childID) {
			Children.Remove(childID);
		}

		public string? PendingMotherID { get; set; }
		private Character? mother;
		public Character? Mother {
			get { return mother; }
			set {
				if (PendingMotherID is not null && value is not null && value.ID != PendingMotherID) {
					Logger.Warn($"Character {ID}: linking mother {value.ID} instead of expected {PendingMotherID}");
				}
				mother = value;
				PendingMotherID = null;
			}
		}
		public string? PendingFatherID { get; set; }
		private Character? father;
		public Character? Father {
			get { return father; }
			set {
				if (PendingFatherID is not null && value is not null && value.ID != PendingFatherID) {
					Logger.Warn($"Character {ID}: linking father {value.ID} instead of expected {PendingFatherID}");
				}
				father = value;
				PendingFatherID = null;
			}
		}
		public Dictionary<string, Character?> Children { get; set; } = new();
		public Dictionary<string, Character?> Spouses { get; set; } = new();

		public string? DynastyID { get; set; } // not always set
	}
}
