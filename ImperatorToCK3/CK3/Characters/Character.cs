using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters {
	public class Character : IIdentifiable<string> {
		public string Id { get; }
		public bool FromImperator { get; } = false;
		public bool Female { get; private set; }
		public string Culture { get; set; } = string.Empty;
		public string Religion { get; set; } = string.Empty;
		public string Name { get; set; }
		public string? Nickname { get; set; }

		public uint Age { get; private set; } // used when option to convert character age is chosen
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

		public SortedSet<string> Traits { get; } = new();
		public Dictionary<string, string> PrisonerIds { get; } = new(); // <prisoner id, imprisonment type>
		public Dictionary<string, LocBlock> Localizations { get; } = new();

		public Imperator.Characters.Character? ImperatorCharacter { get; set; }

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
			Id = $"imperatorRegnal{imperatorCountry.Tag}{preImperatorRuler.Name}{rulerTermStart.ToString()[1..]}BC";
			FromImperator = true;
			Name = preImperatorRuler.Name ?? Id;
			if (!string.IsNullOrEmpty(Name)) {
				var impNameLoc = locDB.GetLocBlockForKey(Name);
				if (impNameLoc is not null) {
					Localizations.Add(Name, impNameLoc);
				} else {  // fallback: use unlocalized name as displayed name
					Localizations.Add(Name, new LocBlock(Name, "english") {
						["english"] = Name,
						["french"] = Name,
						["german"] = Name,
						["russian"] = Name,
						["simp_chinese"] = Name,
						["spanish"] = Name
					});
				}
			}

			BirthDate = new Date(0, 1, 1);
			DeathDate = new Date(0, 1, 30);

			// determine culture and religion
			ulong ck3Province = 0;
			ulong impProvince = 0;
			var srcReligion = preImperatorRuler.Religion ?? imperatorCountry.Religion;
			var srcCulture = preImperatorRuler.Culture ?? imperatorCountry.PrimaryCulture;
			if (imperatorCountry.Capital is not null) {
				impProvince = (ulong)imperatorCountry.Capital;
				var ck3Provinces = provinceMapper.GetCK3ProvinceNumbers(impProvince);
				if (ck3Provinces.Count > 0) {
					ck3Province = ck3Provinces[0];
				}
			}

			if (srcReligion is not null) {
				var religionMatch = religionMapper.Match(srcReligion, ck3Province, impProvince, config);
				if (religionMatch is not null) {
					Religion = religionMatch;
				}
			}

			if (srcCulture is not null) {
				var cultureMatch = cultureMapper.Match(srcCulture, Religion, ck3Province, impProvince, imperatorCountry.HistoricalTag);
				if (cultureMatch is not null) {
					Culture = cultureMatch;
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
				Name = "IMPTOCK3_CUSTOM_NAME_" + loc.Replace(' ', '_');

				var locBlock = new LocBlock(Name, "english") {
					["english"] = loc
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
						var locBlock = new LocBlock(Name, "english") {
							["english"] = nameLoc
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

			var match = religionMapper.Match(ImperatorCharacter.Religion, ck3Province, ImperatorCharacter.ProvinceId, config);
			if (match is not null) {
				Religion = match;
			}

			match = cultureMapper.Match(
				ImperatorCharacter.Culture,
				Religion, ck3Province,
				ImperatorCharacter.ProvinceId,
				ImperatorCharacter.Country?.HistoricalTag ?? string.Empty
			);
			if (match is null) {
				Logger.Warn($"Could not determine CK3 culture for Imperator character {ImperatorCharacter.Id}" +
							$" with culture {ImperatorCharacter.Culture}!");
			} else {
				Culture = match;
			}

			Traits.UnionWith(traitMapper.GetCK3TraitsForImperatorTraits(ImperatorCharacter.Traits));

			var nicknameMatch = nicknameMapper.GetCK3NicknameForImperatorNickname(ImperatorCharacter.Nickname);
			if (nicknameMatch is not null) {
				Nickname = nicknameMatch;
			}

			BirthDate = ImperatorCharacter.BirthDate;
			if (BirthDate.Year < 0) {
				BirthDate = new Date(0, 1, 1);
			}
			DeathDate = ImperatorCharacter.DeathDate;
			if (DeathDate?.Year < 0) {
				DeathDate = new Date(0, 12, 31);
			}
			var impDeathReason = ImperatorCharacter.DeathReason;
			if (impDeathReason is not null) {
				DeathReason = deathReasonMapper.GetCK3ReasonForImperatorReason(impDeathReason);
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
					Logger.Warn($"Imperator character {ImperatorCharacter.Id}'s prison country does not exist in CK3!");
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

		public void BreakAllLinks() {
			Mother?.RemoveChild(Id);
			RemoveMother();
			Father?.RemoveChild(Id);
			RemoveFather();
			foreach (var spouse in Spouses) {
				spouse.RemoveSpouse(Id);
			}
			Spouses.Clear();
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

		private void RemoveSpouse(string spouseId) {
			Spouses.Remove(spouseId);
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
		public IdObjectCollection<string, Character> Spouses { get; set; } = new();

		public string? DynastyId { get; set; } // not always set

		private string? jailorId;
		public string? EmployerId { get; set; }

		public bool LinkJailor(CharacterCollection characters) {
			if (jailorId is null or "0") {
				return false;
			}

			var type = DynastyId is null ? "dungeon" : "house_arrest";
			characters[jailorId].PrisonerIds.Add(Id, type);
			return true;
		}
	}
}
