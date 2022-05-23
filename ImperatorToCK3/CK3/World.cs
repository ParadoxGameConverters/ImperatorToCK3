using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Map;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.Trait;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3 {
	public class World {
		public CharacterCollection Characters { get; } = new();
		public DynastyCollection Dynasties { get; } = new();
		public ProvinceCollection Provinces { get; } = new();
		public Title.LandedTitles LandedTitles { get; } = new();
		public MapData MapData { get; }
		public Date CorrectedDate { get; }

		public World(Imperator.World impWorld, Configuration config) {
			Logger.Info("*** Hello CK3, let's get painting. ***");
			CorrectedDate = impWorld.EndDate.Year > 1 ? impWorld.EndDate : new Date(2, 1, 1);
			if (config.CK3BookmarkDate.Year == 0) { // bookmark date is not set
				config.CK3BookmarkDate = CorrectedDate;
				Logger.Info($"CK3 bookmark date set to: {config.CK3BookmarkDate}");
			} else if (CorrectedDate > config.CK3BookmarkDate) {
				Logger.Error("Corrected save date is later than CK3 bookmark date, proceeding at your own risk!");
			}

			Logger.Info("Loading map data...");
			MapData = new MapData(config.CK3Path);

			// Scrape localizations from Imperator so we may know proper names for our countries.
			locDB.ScrapeLocalizations(config.ImperatorPath, impWorld.Mods);
			
			// Load CK3 religions from game and blankMod
			var relativeReligionsPath = Path.Join("common", "religion", "religions");
			religionCollection.LoadReligions(Path.Combine(config.CK3Path, "game", relativeReligionsPath));
			religionCollection.LoadReligions(Path.Combine("blankMod", "output", relativeReligionsPath));

			// Load Imperator CoAs to use them for generated CK3 titles
			coaMapper = new CoaMapper(config, impWorld.Mods);

			// Load vanilla CK3 landed titles and their history
			LandedTitles.LoadTitles(config.CK3Path);
			LandedTitles.LoadHistory(config);

			// Loading regions
			ck3RegionMapper = new CK3RegionMapper(config.CK3Path, LandedTitles);
			imperatorRegionMapper = new ImperatorRegionMapper(config.ImperatorPath, impWorld.Mods);
			// Use the region mappers in other mappers
			var religionMapper = new ReligionMapper(imperatorRegionMapper, ck3RegionMapper);
			var cultureMapper = new CultureMapper(imperatorRegionMapper, ck3RegionMapper);
			
			var traitMapper = new TraitMapper(Path.Combine("configurables", "trait_map.txt"), config);

			Characters.ImportImperatorCharacters(
				impWorld,
				religionMapper,
				cultureMapper,
				traitMapper,
				nicknameMapper,
				locDB,
				provinceMapper,
				deathReasonMapper,
				CorrectedDate,
				config
			);
			ClearFeaturedCharactersDescriptions(config.CK3BookmarkDate);

			Dynasties.ImportImperatorFamilies(impWorld, locDB);

			LandedTitles.ImportImperatorCountries(
				impWorld.Countries,
				tagTitleMapper,
				locDB,
				provinceMapper,
				coaMapper,
				governmentMapper,
				successionLawMapper,
				definiteFormMapper,
				religionMapper,
				cultureMapper,
				nicknameMapper,
				Characters,
				CorrectedDate,
				config
			);

			// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
			// Some of it will be overwritten, but not all.
			Provinces.ImportVanillaProvinces(config.CK3Path, config.CK3BookmarkDate);

			// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
			Provinces.ImportImperatorProvinces(impWorld, LandedTitles, cultureMapper, religionMapper, provinceMapper, config);

			var countyLevelGovernorships = new List<Governorship>();
			LandedTitles.ImportImperatorGovernorships(
				impWorld,
				Provinces,
				tagTitleMapper,
				locDB,
				provinceMapper,
				definiteFormMapper,
				imperatorRegionMapper,
				coaMapper,
				countyLevelGovernorships
			);

			OverWriteCountiesHistory(impWorld.Jobs.Governorships, countyLevelGovernorships, impWorld.Characters, CorrectedDate);
			LandedTitles.ImportDevelopmentFromImperator(impWorld.Provinces, provinceMapper, CorrectedDate);
			LandedTitles.RemoveInvalidLandlessTitles(config.CK3BookmarkDate);
			LandedTitles.SetDeJureKingdomsAndEmpires(config.CK3BookmarkDate);

			Characters.RemoveEmployerIdFromLandedCharacters(LandedTitles, CorrectedDate);
			Characters.PurgeUnneededCharacters(LandedTitles);
			
			//religionCollection.ConvertHolySites // TODO: FINISH
		}

		private void ClearFeaturedCharactersDescriptions(Date ck3BookmarkDate) {
			Logger.Info("Clearing featured characters' descriptions...");
			foreach (var title in LandedTitles) {
				if (!title.PlayerCountry) {
					continue;
				}
				var holderId = title.GetHolderId(ck3BookmarkDate);
				if (holderId != "0" && Characters.TryGetValue(holderId, out var holder)) {
					title.Localizations.AddLocBlock($"{holder.Name}_desc");
				}
			}
		}

		private void OverWriteCountiesHistory(IEnumerable<Governorship> governorships, IEnumerable<Governorship> countyLevelGovernorships, Imperator.Characters.CharacterCollection impCharacters, Date conversionDate) {
			Logger.Info("Overwriting counties' history...");
			foreach (var county in LandedTitles.Where(t => t.Rank == TitleRank.county)) {
				if (county.CapitalBaronyProvince is null) {
					Logger.Warn($"County {county} has no capital barony province!");
					continue;
				}
				ulong capitalBaronyProvinceId = (ulong)county.CapitalBaronyProvince;
				if (capitalBaronyProvinceId == 0) {
					// title's capital province has an invalid ID (0 is not a valid province in CK3)
					Logger.Warn($"County {county} has invalid capital barony province!");
					continue;
				}

				if (!Provinces.ContainsKey(capitalBaronyProvinceId)) {
					Logger.Warn($"Capital barony province not found: {capitalBaronyProvinceId}");
					continue;
				}

				var ck3CapitalBaronyProvince = Provinces[capitalBaronyProvinceId];
				var impProvince = ck3CapitalBaronyProvince.ImperatorProvince;
				if (impProvince is null) { // probably outside of Imperator map
					continue;
				}

				var impCountry = impProvince.OwnerCountry;

				if (impCountry is null || impCountry.CountryType == CountryType.rebels) { // e.g. uncolonized Imperator province
					county.SetHolder(null, conversionDate);
					county.SetDeFactoLiege(null, conversionDate);
				} else {
					bool given = TryGiveCountyToGovernor(county, impProvince, impCountry);
					if (!given) {
						given = TryGiveCountyToMonarch(county, impCountry);
					}
					if (!given) {
						Logger.Warn($"County {county} was not given to anyone!");
					}
				}
			}

			bool TryGiveCountyToMonarch(Title county, Country impCountry) {
				var ck3Country = impCountry.CK3Title;
				if (ck3Country is null) {
					Logger.Warn($"{impCountry.Name} has no CK3 title!"); // should not happen
					return false;
				}

				GiveCountyToMonarch(county, ck3Country);
				return true;
			}

			void GiveCountyToMonarch(Title county, Title ck3Country) {
				var date = ck3Country.GetDateOfLastHolderChange();
				var holderId = ck3Country.GetHolderId(date);
				
				if (Characters.TryGetValue(holderId, out var holder)) {
					county.ClearHolderSpecificHistory();
					county.SetHolder(holder, date);
				} else {
					Logger.Warn($"Holder {holderId} of county {county} doesn't exist!");
				}
				county.SetDeFactoLiege(null, date);
			}

			bool TryGiveCountyToGovernor(Title county, Imperator.Provinces.Province impProvince, Country impCountry) {
				var ck3Country = impCountry.CK3Title;
				if (ck3Country is null) {
					Logger.Warn($"{impCountry.Name} has no CK3 title!"); // should not happen
					return false;
				}
				var matchingGovernorships = new List<Governorship>(governorships.Where(g =>
					g.CountryId == impCountry.Id &&
					g.RegionName == imperatorRegionMapper.GetParentRegionName(impProvince.Id)
				));

				var ck3CapitalCounty = ck3Country.CapitalCounty;
				if (ck3CapitalCounty is null) {
					Logger.Warn($"{ck3Country} has no capital county!");
					return false;
				}
				// if title belongs to country ruler's capital's de jure duchy, it needs to be directly held by the ruler
				var countryCapitalDuchy = ck3CapitalCounty.DeJureLiege;
				var deJureDuchyOfCounty = county.DeJureLiege;
				if (countryCapitalDuchy is not null && deJureDuchyOfCounty is not null && countryCapitalDuchy.Id == deJureDuchyOfCounty.Id) {
					return false;
				}

				if (matchingGovernorships.Count == 0) {
					// we have no matching governorship
					return false;
				}

				// give county to governor
				var governorship = matchingGovernorships[0];
				var ck3GovernorshipId = tagTitleMapper.GetTitleForGovernorship(governorship, impCountry, LandedTitles, Provinces, imperatorRegionMapper);
				if (ck3GovernorshipId is null) {
					Logger.Warn($"{nameof(ck3GovernorshipId)} is null for {ck3Country} {governorship.RegionName}!");
					return false;
				}

				if (countyLevelGovernorships.Contains(governorship)) {
					GiveCountyToCountyLevelGovernor(county, governorship, ck3Country);
				} else {
					GiveCountyToGovernor(county, ck3GovernorshipId);
				}
				return true;
			}

			void GiveCountyToGovernor(Title county, string ck3GovernorshipId) {
				var ck3Governorship = LandedTitles[ck3GovernorshipId];
				var holderChangeDate = ck3Governorship.GetDateOfLastHolderChange();
				var holderId = ck3Governorship.GetHolderId(holderChangeDate);
				if (Characters.TryGetValue(holderId, out var governor)) {
					county.ClearHolderSpecificHistory();
					county.SetHolder(governor, holderChangeDate);
				} else {
					Logger.Warn($"Holder {holderId} of county {county} doesn't exist!");
				}
				county.SetDeFactoLiege(null, holderChangeDate);
			}

			void GiveCountyToCountyLevelGovernor(Title county, Governorship governorship, Title ck3Country) {
				var holderChangeDate = governorship.StartDate;
				var impGovernor = impCharacters[governorship.CharacterId];
				var governor = impGovernor.CK3Character;

				county.ClearHolderSpecificHistory();
				county.SetHolder(governor, holderChangeDate);
				county.SetDeFactoLiege(ck3Country, holderChangeDate);
			}
		}

		private readonly CoaMapper coaMapper;
		private readonly DeathReasonMapper deathReasonMapper = new();
		private readonly DefiniteFormMapper definiteFormMapper = new(Path.Combine("configurables", "definite_form_names.txt"));
		private readonly GovernmentMapper governmentMapper = new();
		private readonly LocDB locDB = new("english", "french", "german", "russian", "simp_chinese", "spanish");
		private readonly NicknameMapper nicknameMapper = new(Path.Combine("configurables", "nickname_map.txt"));
		private readonly ProvinceMapper provinceMapper = new();
		private readonly SuccessionLawMapper successionLawMapper = new(Path.Combine("configurables", "succession_law_map.txt"));
		private readonly TagTitleMapper tagTitleMapper = new(
			tagTitleMappingsPath: Path.Combine("configurables", "title_map.txt"),
			governorshipTitleMappingsPath: Path.Combine("configurables", "governorMappings.txt")
		);
		private readonly CK3RegionMapper ck3RegionMapper;
		private readonly ImperatorRegionMapper imperatorRegionMapper;
		private readonly ReligionCollection religionCollection = new();
	}
}
