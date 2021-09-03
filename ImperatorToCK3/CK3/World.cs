using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.Imperator;
using commonItems;
using ImperatorToCK3.Imperator.Countries;

namespace ImperatorToCK3.CK3 {
	public class World {
		public Dictionary<string, Character> Characters { get; private set; } = new();
		public Dictionary<string, Dynasty> Dynasties { get; private set; } = new();
		public Dictionary<ulong, Province> Provinces { get; private set; } = new();
		private LandedTitles landedTitles;
		public Dictionary<string, Title> LandedTitles {
			get {
				return landedTitles.StoredTitles;
			}
		}

		private void OverWriteCountiesHistory() {
			Logger.Info("Overwriting counties' history.");
			foreach (var title in LandedTitles.Values) {
				if (title.Rank == TitleRank.county && title.CapitalBaronyProvince > 0) { // title is a county and its capital province has a valid ID (0 is not a valid province in CK3)
					if (!Provinces.ContainsKey(title.CapitalBaronyProvince)) {
						Logger.Warn($"Capital barony province not found {title.CapitalBaronyProvince}");
					} else {
						var ck3CapitalBaronyProvince = Provinces[title.CapitalBaronyProvince];
						var impProvince = ck3CapitalBaronyProvince.ImperatorProvince;
						if (impProvince is not null) {
							var impCountry = impProvince.OwnerCountry.Value;
							if (impCountry is not null && impCountry.CountryType != CountryType.rebels) {
								var impMonarch = impCountry.Monarch;
								if (impMonarch is not null) {
									if (Characters.TryGetValue("imperator" + impMonarch.ToString(), out var holder){
										title.Holder = holder;
									}
									title.DeFactoLiege = null;
									countyHoldersCache.Add(title.HolderID);
								}
							} else { // e.g. uncolonised Imperator province
								title.Holder = null;
								title.DeFactoLiege = null;
							}
						} else { // county is probably outside of Imperator map
							if (!string.IsNullOrEmpty(title.HolderID) && title.HolderID != "0") {
								countyHoldersCache.Add(title.HolderID);
							}
						}
					}
				}
			}
		}

		private void RemoveInvalidLandlessTitles() {
			Logger.Info("Removing invalid landless titles.");
			var removedGeneratedTitles = new HashSet<string>();
			var revokedVanillaTitles = new HashSet<string>();

			foreach (var (name, title) in LandedTitles) {
				//important check: if duchy/kingdom/empire title holder holds no county (is landless), remove the title
				// this also removes landless titles initialized from Imperator
				if (title.Rank != TitleRank.county && title.Rank != TitleRank.barony && !countyHoldersCache.Contains(title.HolderID)) {
					if (!LandedTitles[name].Landless) { // does not have landless attribute set to true
						if (title.IsImportedOrUpdatedFromImperator && name.IndexOf("IMPTOCK3") != -1) {
							removedGeneratedTitles.Add(name);
							landedTitles.EraseTitle(name);
						} else {
							revokedVanillaTitles.Add(name);
							title.Holder = null;
							title.DeFactoLiege = null;
						}
					}
				}
			}
			if (removedGeneratedTitles.Count > 0) {
				var sb = new StringBuilder();
				sb.Append("Found landless generated titles that can't be landless:");
				foreach (var name in removedGeneratedTitles) {
					sb.Append(' ');
					sb.Append(name);
					sb.Append(',');
				}
				Logger.Debug(sb.ToString()[0..^1]); // remove last comma
			}
			if (revokedVanillaTitles.Count > 0) {
				var sb = new StringBuilder();
				sb.Append("Found landless vanilla titles that can't be landless: ");
				foreach (var name in revokedVanillaTitles) {
					sb.Append(' ');
					sb.Append(name);
					sb.Append(',');
				}
				Logger.Debug(sb.ToString()[0..^1]); // remove last comma
			}
		}

		private void PurgeLandlessVanillaCharacters() {
			var farewellIds = new HashSet<string>(Characters.Keys);
			foreach (var id in farewellIds) {
				if (id.StartsWith("imperator")) {
					farewellIds.Remove(id);
				}
			}
			foreach (var title in LandedTitles.Values) {
				farewellIds.Remove(title.HolderID);
			}

			foreach (var characterId in farewellIds) {
				Characters[characterId].BreakAllLinks();
				Characters.Remove(characterId);
			}
			Logger.Info($"Purged {farewellIds.Count} landless vanilla characters.");
		}

		private void LinkSpouses() {
			var spouseCounter = 0;
			foreach (var ck3Character in Characters.Values) {
				var newSpouses = new Dictionary<ulong, Character>();
				// make links between Imperator characters
				foreach (var impSpouseCharacter in ck3Character.ImperatorCharacter.Spouses.Values) {
					if (impSpouseCharacter is not null) {
						var ck3SpouseCharacter = impSpouseCharacter.CK3Character;
						ck3Character.Spouses[ck3SpouseCharacter.ID] = ck3SpouseCharacter;
						ck3SpouseCharacter.Spouses[ck3Character.ID] = ck3Character;
						++spouseCounter;
					}
				}
			}
			Logger.Info($"{spouseCounter} spouses linked in CK3.");
		}

		private void LinkMothersAndFathers() {
			var motherCounter = 0;
			var fatherCounter = 0;
			foreach (var ck3Character in Characters.Values) {
				// make links between Imperator characters
				var impMotherCharacter = ck3Character.ImperatorCharacter.Mother.Value;
				if (impMotherCharacter is not null) {
					var ck3MotherCharacter = impMotherCharacter.CK3Character;
					ck3Character.Mother = ck3MotherCharacter;
					ck3MotherCharacter.Children[ck3Character.ID] = ck3Character;
					++motherCounter;
				}

				// make links between Imperator characters
				var impFatherCharacter = ck3Character.ImperatorCharacter.Father.Value;
				if (impFatherCharacter is not null) {
					var ck3FatherCharacter = impFatherCharacter.CK3Character;
					ck3Character.Father = ck3FatherCharacter;
					ck3FatherCharacter.Children[ck3Character.ID] = ck3Character;
					++fatherCounter;
				}
			}
			Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked in CK3.");
		}


		private void ImportImperatorFamilies(Imperator.World impWorld) {
			Logger.Info("Importing Imperator Families.");

			// dynasties only holds dynasties converted from Imperator families, as vanilla ones aren't modified
			foreach (var family in impWorld.Families.StoredFamilies.Values) {
				if (family.Minor)
					continue;

				var newDynasty = new Dynasty(family, localizationMapper);
				Dynasties.Add(newDynasty.ID, newDynasty);
			}
			Logger.Info($"{Dynasties.Count} total families imported.");
		}

		private CoaMapper coaMapper;
		private CultureMapper cultureMapper;
		private DeathReasonMapper deathReasonMapper;
		private GovernmentMapper governmentMapper;
		private LocalizationMapper localizationMapper;
		private NicknameMapper nicknameMapper;
		private ProvinceMapper provinceMapper;
		private ReligionMapper religionMapper;
		private SuccessionLawMapper successionLawMapper;
		private TagTitleMapper tagTitleMapper;
		private TraitMapper traitMapper;
		private CK3RegionMapper ck3RegionMapper;
		private ImperatorRegionMapper imperatorRegionMapper;
		private TitlesHistory titlesHistory;

		private HashSet<string> countyHoldersCache = new(); // used by RemoveInvalidLandlessTitles
	}
}
