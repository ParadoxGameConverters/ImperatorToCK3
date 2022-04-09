using commonItems;
using commonItems.Localization;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Map;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
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

			// Scraping localizations from Imperator so we may know proper names for our countries.
			locDB.ScrapeLocalizations(config.ImperatorPath, impWorld.Mods);

			// Loading Imperator CoAs to use them for generated CK3 titles
			coaMapper = new CoaMapper(config, impWorld.Mods);

			// Load vanilla titles history
			var titlesHistoryPath = Path.Combine(config.CK3Path, "game", "history", "titles");
			titlesHistory = new TitlesHistory(titlesHistoryPath);

			// Load vanilla CK3 landed titles
			var landedTitlesPath = Path.Combine(config.CK3Path, "game", "common", "landed_titles", "00_landed_titles.txt");

			LandedTitles.LoadTitles(landedTitlesPath);
			AddHistoryToVanillaTitles(config.CK3BookmarkDate);

			// Loading regions
			ck3RegionMapper = new CK3RegionMapper(config.CK3Path, LandedTitles);
			imperatorRegionMapper = new ImperatorRegionMapper(config.ImperatorPath, impWorld.Mods);
			// Use the region mappers in other mappers
			religionMapper.LoadRegionMappers(imperatorRegionMapper, ck3RegionMapper);
			var cultureMapper = new CultureMapper(imperatorRegionMapper, ck3RegionMapper);

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
				CorrectedDate
			);

			// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
			// Some of it will be overwritten, but not all.
			Provinces.ImportVanillaProvinces(config.CK3Path, config.CK3BookmarkDate);

			// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
			Provinces.ImportImperatorProvinces(impWorld, LandedTitles, cultureMapper, religionMapper, provinceMapper);

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
				config.CK3BookmarkDate
			);
			ClearFeaturedCharactersDescriptions(config.CK3BookmarkDate);

			Dynasties.ImportImperatorFamilies(impWorld, locDB);

			OverWriteCountiesHistory(impWorld.Jobs.Governorships, countyLevelGovernorships, impWorld.Characters, CorrectedDate);
			LandedTitles.ImportDevelopmentFromImperator(impWorld.Provinces, provinceMapper, CorrectedDate);
			LandedTitles.RemoveInvalidLandlessTitles(config.CK3BookmarkDate);
			LandedTitles.SetDeJureKingdomsAndEmpires(config.CK3BookmarkDate);

			Characters.RemoveEmployerIdFromLandedCharacters(LandedTitles, CorrectedDate);
			Characters.PurgeUnneededCharacters(LandedTitles);
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

		private void AddHistoryToVanillaTitles(Date ck3BookmarkDate) {
			foreach (var title in LandedTitles) {
				var historyOpt = titlesHistory.PopTitleHistory(title.Id);
				if (historyOpt is not null) {
					title.AddHistory(historyOpt);
				}
			}
			// Add vanilla development to counties
			// For counties that inherit development level from de jure lieges, assign it to them directly for better reliability.
			foreach (var title in LandedTitles.Where(t => t.Rank == TitleRank.county && t.GetDevelopmentLevel(ck3BookmarkDate) is null)) {
				var inheritedDev = title.GetOwnOrInheritedDevelopmentLevel(ck3BookmarkDate);
				title.SetDevelopmentLevel(inheritedDev ?? 0, ck3BookmarkDate);
			}
			foreach (var title in LandedTitles.Where(t => t.Rank > TitleRank.county)) {
				title.History.InternalHistory.Fields.Remove("development_level");
			}

			// Remove history entries past the bookmark date.
			foreach (var title in LandedTitles) {
				title.RemoveHistoryPastBookmarkDate(ck3BookmarkDate);
			}
		}

		private void OverWriteCountiesHistory(IReadOnlyCollection<Governorship> governorships, IReadOnlyCollection<Governorship> countyLevelGovernorships, Imperator.Characters.CharacterCollection impCharacters, Date conversionDate) {
			Logger.Info("Overwriting counties' history...");
			foreach (var county in LandedTitles.Where(t => t.Rank == TitleRank.county)) {
				ulong capitalBaronyProvinceId = (ulong)county.CapitalBaronyProvince!;
				if (capitalBaronyProvinceId == 0) {
					// title's capital province has an invalid ID (0 is not a valid province in CK3)
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

				var impMonarch = impCountry.Monarch;
				if (impMonarch is null) {
					Logger.Warn($"Imperator ruler doesn't exist for {impCountry.Name} owning {county}!");
					return false;
				}
				GiveCountyToMonarch(county, ck3Country, impMonarch.Id);
				return true;
			}

			void GiveCountyToMonarch(Title county, Title ck3Country, ulong impMonarchId) {
				var holderId = $"imperator{impMonarchId}";
				var date = ck3Country.GetDateOfLastHolderChange();
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
				var holderChangeDate = governorship.StartDate.Year > 0 ? governorship.StartDate : new Date(1, 1, 1);
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
		private readonly ReligionMapper religionMapper = new();
		private readonly SuccessionLawMapper successionLawMapper = new(Path.Combine("configurables", "succession_law_map.txt"));
		private readonly TagTitleMapper tagTitleMapper = new(
			tagTitleMappingsPath: Path.Combine("configurables", "title_map.txt"),
			governorshipTitleMappingsPath: Path.Combine("configurables", "governorMappings.txt")
		);
		private readonly CK3RegionMapper ck3RegionMapper;
		private readonly ImperatorRegionMapper imperatorRegionMapper;
		private readonly TitlesHistory titlesHistory;
	}
}
