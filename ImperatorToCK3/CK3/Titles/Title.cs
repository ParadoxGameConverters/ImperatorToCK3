using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Titles {
	public enum TitleRank { barony, county, duchy, kingdom, empire }
	public class Title : IPDXSerializable, IIdentifiable<string> {
		public Title(string id) {
			Id = id;
			SetRank();
		}

		public Title(
			Country country,
			CountryCollection imperatorCountries,
			LocalizationMapper localizationMapper,
			LandedTitles landedTitles,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			TagTitleMapper tagTitleMapper,
			GovernmentMapper governmentMapper,
			SuccessionLawMapper successionLawMapper,
			DefiniteFormMapper definiteFormMapper,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			Characters.CharacterCollection characters
		) {
			Id = DetermineName(country, imperatorCountries, tagTitleMapper, localizationMapper);
			SetRank();
			InitializeFromTag(
				country, imperatorCountries, localizationMapper, landedTitles,
				provinceMapper,
				coaMapper,
				governmentMapper,
				successionLawMapper,
				definiteFormMapper,
				religionMapper,
				cultureMapper,
				nicknameMapper,
				characters
			);
		}
		public Title(
			string id,
			Governorship governorship,
			Country country,
			Imperator.Characters.CharacterCollection imperatorCharacters,
			bool regionHasMultipleGovernorships,
			LocalizationMapper localizationMapper,
			LandedTitles landedTitles,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			TagTitleMapper tagTitleMapper,
			DefiniteFormMapper definiteFormMapper,
			ImperatorRegionMapper imperatorRegionMapper
		) {
			Id = id;
			SetRank();
			InitializeFromGovernorship(
				governorship,
				country,
				imperatorCharacters,
				regionHasMultipleGovernorships,
				localizationMapper,
				landedTitles,
				provinceMapper,
				definiteFormMapper,
				imperatorRegionMapper
			);
		}
		public void InitializeFromTag(
			Country country,
			CountryCollection imperatorCountries,
			LocalizationMapper localizationMapper,
			LandedTitles landedTitles,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			GovernmentMapper governmentMapper,
			SuccessionLawMapper successionLawMapper,
			DefiniteFormMapper definiteFormMapper,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			Characters.CharacterCollection characters
		) {
			IsImportedOrUpdatedFromImperator = true;
			ImperatorCountry = country;
			ImperatorCountry.CK3Title = this;

			LocBlock? validatedName = GetValidatedName(country, imperatorCountries, localizationMapper);

			HasDefiniteForm.Value = definiteFormMapper.IsDefiniteForm(ImperatorCountry.Name);
			RulerUsesTitleName.Value = false;

			PlayerCountry = ImperatorCountry.PlayerCountry;

			ClearHolderSpecificHistory();

			// ------------------ determine previous and current holders
			// there was no 0 AD, but year 0 works in game and serves well for adding BC characters to holder history
			var firstPossibleDate = new Date(0, 1, 1);
			foreach (var impRulerTerm in ImperatorCountry.RulerTerms) {
				var rulerTerm = new RulerTerm(
					impRulerTerm,
					characters,
					governmentMapper,
					localizationMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					provinceMapper
				);

				var characterId = rulerTerm.CharacterId;
				var gov = rulerTerm.Government;

				var startDate = new Date(rulerTerm.StartDate);
				if (startDate < firstPossibleDate) {
					startDate = new Date(firstPossibleDate); // TODO: remove this workaround if CK3 supports negative dates
					firstPossibleDate.ChangeByDays(1);
				}

				history.InternalHistory.AddFieldValue("holder", characterId, startDate, "holder");
				if (gov is not null) {
					history.InternalHistory.AddFieldValue("government", gov, startDate, "government");
				}
			}

			// ------------------ determine color
			var color1Opt = ImperatorCountry.Color1;
			if (color1Opt is not null) {
				Color1 = color1Opt;
			}
			var color2Opt = ImperatorCountry.Color2;
			if (color2Opt is not null) {
				Color2 = color2Opt;
			}

			// determine successions laws
			SuccessionLaws = successionLawMapper.GetCK3LawsForImperatorLaws(ImperatorCountry.GetLaws());

			// ------------------ determine CoA
			CoA = coaMapper.GetCoaForFlagName(ImperatorCountry.Flag);

			// ------------------ determine other attributes

			var srcCapital = ImperatorCountry.Capital;
			if (srcCapital is not null) {
				var provMappingsForImperatorCapital = provinceMapper.GetCK3ProvinceNumbers((ulong)srcCapital);
				if (provMappingsForImperatorCapital.Count > 0) {
					var foundCounty = landedTitles.GetCountyForProvince(provMappingsForImperatorCapital[0]);
					if (foundCounty is not null) {
						CapitalCounty = foundCounty;
					}
				}
			}

			// ------------------ Country Name Locs

			var nameSet = false;
			if (validatedName is not null) {
				Localizations[Id] = validatedName;
				nameSet = true;
			}
			if (!nameSet) {
				var impTagLoc = localizationMapper.GetLocBlockForKey(ImperatorCountry.Tag);
				if (impTagLoc is not null) {
					Localizations[Id] = impTagLoc;
					nameSet = true;
				}
			}
			if (!nameSet) {
				// use unlocalized name if not empty
				var name = ImperatorCountry.Name;
				if (!string.IsNullOrEmpty(name)) {
					Logger.Warn($"Using unlocalized Imperator name {name} as name for {Id}!");
					Localizations[Id] = new LocBlock(name);
					nameSet = true;
				}
			}
			// giving up
			if (!nameSet) {
				Logger.Warn($"{Id} needs help with localization! {ImperatorCountry.Name}?");
			}

			// --------------- Adjective Locs
			TrySetAdjectiveLoc(localizationMapper, imperatorCountries);
		}

		internal void LinkCapital(LandedTitles titles) {
			if (parsedCapitalCountyName is null) {
				return;
			}
			if (CapitalCounty is null) {
				CapitalCounty = titles[parsedCapitalCountyName];
			}
		}

		private static LocBlock? GetValidatedName(Country imperatorCountry, CountryCollection imperatorCountries, LocalizationMapper localizationMapper) {
			return imperatorCountry.Name switch {
				// hard code for Antigonid Kingdom, Seleucid Empire and Maurya
				// these countries use customizable localization for name and adjective
				"PRY_DYN" => localizationMapper.GetLocBlockForKey("get_pry_name_fallback"),
				"SEL_DYN" => localizationMapper.GetLocBlockForKey("get_sel_name_fallback"),
				"MRY_DYN" => localizationMapper.GetLocBlockForKey("get_mry_name_fallback"),
				_ => imperatorCountry.CountryName.GetNameLocBlock(localizationMapper, imperatorCountries)
			};
		}

		public static string DetermineName(
			Country imperatorCountry,
			CountryCollection imperatorCountries,
			TagTitleMapper tagTitleMapper,
			LocalizationMapper localizationMapper
		) {
			var validatedName = GetValidatedName(imperatorCountry, imperatorCountries, localizationMapper);

			string? title;
			if (validatedName is not null) {
				title = tagTitleMapper.GetTitleForTag(
					imperatorCountry.Tag,
					imperatorCountry.GetCountryRank(),
					validatedName.english
				);
			} else {
				title = tagTitleMapper.GetTitleForTag(imperatorCountry.Tag, imperatorCountry.GetCountryRank());
			}

			if (title is null) {
				throw new System.ArgumentException($"Country {imperatorCountry.Tag} could not be mapped to CK3 Title!");
			}

			return title;
		}
		public static string? DetermineName(Governorship governorship, Country country, LandedTitles titles, ProvinceCollection provinces, ImperatorRegionMapper imperatorRegionMapper, TagTitleMapper tagTitleMapper) {
			if (country.CK3Title is null) {
				throw new System.ArgumentException($"{country.Tag} governorship of {governorship.RegionName} could not be mapped to CK3 title: country has no CK3Title!");
			}
			return tagTitleMapper.GetTitleForGovernorship(governorship, country, titles, provinces, imperatorRegionMapper);
		}

		public void InitializeFromGovernorship(Governorship governorship,
			Country country,
			Imperator.Characters.CharacterCollection imperatorCharacters,
			bool regionHasMultipleGovernorships,
			LocalizationMapper localizationMapper,
			LandedTitles landedTitles,
			ProvinceMapper provinceMapper,
			DefiniteFormMapper definiteFormMapper,
			ImperatorRegionMapper imperatorRegionMapper
		) {
			IsImportedOrUpdatedFromImperator = true;

			if (country.CK3Title is null) {
				throw new System.ArgumentException($"{country.Tag} governorship of {governorship.RegionName} could not be mapped to CK3 title: liege doesn't exist!");
			}

			DeJureLiege = country.CK3Title;
			DeFactoLiege = country.CK3Title;

			HasDefiniteForm.Value = definiteFormMapper.IsDefiniteForm(governorship.RegionName);
			RulerUsesTitleName.Value = false;

			PlayerCountry = false;

			var impGovernor = imperatorCharacters[governorship.CharacterId];
			var normalizedStartDate = governorship.StartDate.Year > 0 ? governorship.StartDate : new Date(1, 1, 1);

			ClearHolderSpecificHistory();

			// ------------------ determine holder
			history.InternalHistory.AddFieldValue("holder", $"imperator{impGovernor.Id}", normalizedStartDate, "holder");

			// ------------------ determine government
			var ck3LiegeGov = country.CK3Title.GetGovernment(normalizedStartDate);
			if (ck3LiegeGov is not null) {
				history.InternalHistory.AddFieldValue("government", ck3LiegeGov, normalizedStartDate, "government");
			}

			// ------------------ determine color
			var color1Opt = country.Color1;
			if (color1Opt is not null) {
				Color1 = color1Opt;
			}
			var color2Opt = country.Color2;
			if (color2Opt is not null) {
				Color2 = color2Opt;
			}

			// determine successions laws
			// https://github.com/ParadoxGameConverters/ImperatorToCK3/issues/90#issuecomment-817178552
			SuccessionLaws = new() { "high_partition_succession_law" };

			// ------------------ determine CoA
			CoA = null; // using game-randomized CoA

			// ------------------ determine capital
			var governorProvince = impGovernor.ProvinceId;
			if (imperatorRegionMapper.ProvinceIsInRegion(governorProvince, governorship.RegionName)) {
				foreach (var ck3Prov in provinceMapper.GetCK3ProvinceNumbers(governorProvince)) {
					var foundCounty = landedTitles.GetCountyForProvince(ck3Prov);
					if (foundCounty is not null) {
						CapitalCounty = foundCounty;
						break;
					}
				}
			}

			TrySetNameFromGovernorship(governorship, country, regionHasMultipleGovernorships, localizationMapper);
			TrySetAdjectiveFromGovernorship(country);
		}

		private void TrySetAdjectiveFromGovernorship(Country country) {
			var adjKey = Id + "_adj";
			if (!Localizations.ContainsKey(adjKey)) {
				var adjSet = false;
				var ck3Country = country.CK3Title;
				if (ck3Country is null) {
					return;
				}
				if (ck3Country.Localizations.TryGetValue(ck3Country.Id + "_adj", out var countryAdjectiveLocBlock)) {
					var adjLocBlock = new LocBlock(countryAdjectiveLocBlock);
					Localizations.Add(adjKey, adjLocBlock);
					adjSet = true;
				}
				if (!adjSet) {
					Logger.Warn($"{Id} needs help with adjective localization!");
				}
			}
		}

		private void TrySetNameFromGovernorship(
			Governorship governorship,
			Country country,
			bool regionHasMultipleGovernorships,
			LocalizationMapper localizationMapper
		) {
			if (!Localizations.ContainsKey(Id)) {
				var nameSet = false;
				LocBlock? regionLocBlock = localizationMapper.GetLocBlockForKey(governorship.RegionName);

				if (regionHasMultipleGovernorships && regionLocBlock is not null) {
					var ck3Country = country.CK3Title;
					if (ck3Country is not null && ck3Country.Localizations.TryGetValue(ck3Country.Id + "_adj", out var countryAdjectiveLocBlock)) {
						var nameLocBlock = new LocBlock(regionLocBlock);
						nameLocBlock.ModifyForEveryLanguage(countryAdjectiveLocBlock,
							(ref string orig, string adj) => orig = $"{adj} {orig}"
						);
						Localizations[Id] = nameLocBlock;
						nameSet = true;
					}
				}
				if (!nameSet && regionLocBlock is not null) {
					Localizations[Id] = new LocBlock(regionLocBlock);
					nameSet = true;
				}
				if (!nameSet) {
					Logger.Warn($"{Id} needs help with localization!");
				}
			}
		}

		public void LoadTitles(BufferedReader reader, Dictionary<string, object>? variables = null) {
			var parser = new Parser(variables);
			RegisterKeys(parser);
			parser.ParseStream(reader);
		}

		public Date GetDateOfLastHolderChange() {
			var field = history.InternalHistory.Fields["holder"];
			var dates = new SortedSet<Date>(field.ValueHistory.Keys);
			var lastDate = dates.Max;
			return lastDate ?? new Date(1, 1, 1);
		}
		public string GetHolderId(Date date) {
			return history.GetHolderId(date);
		}
		public void SetHolder(Characters.Character? character, Date date) {
			var id = character is null ? "0" : character.Id;
			history.InternalHistory.AddFieldValue("holder", id, date, "holder");
		}
		public string? GetGovernment(Date date) {
			return history.GetGovernment(date);
		}

		[NonSerialized]
		public int? DevelopmentLevel {
			get => history.DevelopmentLevel;
			set => history.DevelopmentLevel = value;
		}

		[NonSerialized] public Dictionary<string, LocBlock> Localizations { get; set; } = new();
		public void SetNameLoc(LocBlock locBlock) {
			Localizations[Id] = locBlock;
		}
		private void TrySetAdjectiveLoc(LocalizationMapper localizationMapper, CountryCollection imperatorCountries) {
			if (ImperatorCountry is null) {
				Logger.Warn($"Cannot set adjective for CK3 Title {Id} from null Imperator Country!");
				return;
			}

			var adjSet = false;

			if (ImperatorCountry.Tag is "PRY" or "SEL" or "MRY") {
				// these tags use customizable loc for adj
				LocBlock? validatedAdj = ImperatorCountry.Name switch {
					"PRY_DYN" => localizationMapper.GetLocBlockForKey("get_pry_adj_fallback"),
					"SEL_DYN" => localizationMapper.GetLocBlockForKey("get_sel_adj_fallback"),
					"MRY_DYN" => localizationMapper.GetLocBlockForKey("get_mry_adj_fallback"),
					_ => null
				};

				if (validatedAdj is not null) {
					Localizations[Id + "_adj"] = validatedAdj;
					adjSet = true;
				}
			}
			if (!adjSet) {
				var adjOpt = ImperatorCountry.CountryName.GetAdjectiveLocBlock(localizationMapper, imperatorCountries);
				if (adjOpt is not null) {
					Localizations[Id + "_adj"] = adjOpt;
					adjSet = true;
				}
			}
			if (!adjSet) {
				var adjLocalizationMatch = localizationMapper.GetLocBlockForKey(ImperatorCountry.Tag);
				if (adjLocalizationMatch is not null) {
					Localizations[Id + "_adj"] = adjLocalizationMatch;
					adjSet = true;
				}
			}
			if (!adjSet) {
				// use unlocalized name if not empty
				var name = ImperatorCountry.Name;
				if (!string.IsNullOrEmpty(name)) {
					Logger.Warn($"Using unlocalized Imperator name {name} as adjective for {Id}!");
					Localizations[Id + "_adj"] = new LocBlock(name);
					adjSet = true;
				}
			}
			// giving up
			if (!adjSet) {
				Logger.Warn($"{Id} needs help with localization for adjective! {ImperatorCountry.Name}_adj?");
			}
		}
		public void AddHistory(LandedTitles landedTitles, TitleHistory titleHistory) {
			history = titleHistory;
			if (history.Liege is not null && landedTitles.TryGetValue(history.Liege, out var liege)) {
				DeFactoLiege = liege;
			}
		}

		[NonSerialized] public string? CoA { get; private set; }

		private string? parsedCapitalCountyName;
		[NonSerialized] public Title? CapitalCounty { get; set; }
		[SerializedName("capital")]
		public string? CapitalCountyName =>
			CapitalCounty is not null ? CapitalCounty.Id : parsedCapitalCountyName;

		[NonSerialized] public Country? ImperatorCountry { get; private set; }

		[SerializedName("color")] public Color? Color1 { get; private set; }
		[SerializedName("color2")] public Color? Color2 { get; private set; }

		private Title? deJureLiege;
		[NonSerialized]
		public Title? DeJureLiege { // direct de jure liege title
			get => deJureLiege;
			set {
				if (value is not null && value.Rank <= Rank) {
					Logger.Warn($"Cannot set de jure liege {value.Id} to {Id}: rank is not higher!");
					return;
				}
				if (deJureLiege is not null) {
					deJureLiege.DeJureVassals.Remove(Id);
				}
				deJureLiege = value;
				if (value is not null) {
					value.DeJureVassals.Add(this);
				}
			}
		}
		private Title? deFactoLiege;
		[NonSerialized]
		public Title? DeFactoLiege { // direct de facto liege title
			get => deFactoLiege;
			set {
				if (value is not null && value.Rank <= Rank) {
					Logger.Warn($"Cannot set de facto liege {value.Id} to {Id}: rank is not higher!");
					return;
				}
				if (deFactoLiege is not null) {
					deFactoLiege.DeFactoVassals.Remove(Id);
				}
				deFactoLiege = value;
				if (value is not null) {
					value.DeFactoVassals.Add(this);
				}
			}
		}
		[SerializeOnlyValue] public TitleCollection DeJureVassals { get; } = new(); // DIRECT de jure vassals
		public Dictionary<string, Title> GetDeJureVassalsAndBelow() {
			return GetDeJureVassalsAndBelow("bcdke");
		}
		public Dictionary<string, Title> GetDeJureVassalsAndBelow(string rankFilter) {
			var rankFilterAsArray = rankFilter.ToCharArray();
			Dictionary<string, Title> deJureVassalsAndBelow = new();
			foreach (var vassalTitle in DeJureVassals) {
				// add the direct part
				if (vassalTitle.Id.IndexOfAny(rankFilterAsArray) == 0) {
					deJureVassalsAndBelow[vassalTitle.Id] = vassalTitle;
				}

				// add the "below" part (recursive)
				var belowTitles = vassalTitle.GetDeJureVassalsAndBelow(rankFilter);
				foreach (var (belowTitleName, belowTitle) in belowTitles) {
					if (belowTitleName.IndexOfAny(rankFilterAsArray) == 0) {
						deJureVassalsAndBelow[belowTitleName] = belowTitle;
					}
				}
			}
			return deJureVassalsAndBelow;
		}
		[NonSerialized] public TitleCollection DeFactoVassals { get; } = new(); // DIRECT de facto vassals
		public Dictionary<string, Title> GetDeFactoVassalsAndBelow() {
			return GetDeFactoVassalsAndBelow("bcdke");
		}
		public Dictionary<string, Title> GetDeFactoVassalsAndBelow(string rankFilter) {
			var rankFilterAsArray = rankFilter.ToCharArray();
			Dictionary<string, Title> deFactoVassalsAndBelow = new();
			foreach (var vassalTitle in DeFactoVassals) {
				// add the direct part
				if (vassalTitle.Id.IndexOfAny(rankFilterAsArray) == 0) {
					deFactoVassalsAndBelow[vassalTitle.Id] = vassalTitle;
				}

				// add the "below" part (recursive)
				var belowTitles = vassalTitle.GetDeFactoVassalsAndBelow(rankFilter);
				foreach (var (belowTitleName, belowTitle) in belowTitles) {
					if (belowTitleName.IndexOfAny(rankFilterAsArray) == 0) {
						deFactoVassalsAndBelow[belowTitleName] = belowTitle;
					}
				}
			}
			return deFactoVassalsAndBelow;
		}

		[NonSerialized] public bool PlayerCountry { get; private set; }
		[NonSerialized] public string Id { get; } // e.g. d_latium
		[NonSerialized] public TitleRank Rank { get; private set; } = TitleRank.duchy;
		[SerializedName("landless")] public PDXBool Landless { get; private set; } = new(false);
		[SerializedName("definite_form")] public PDXBool HasDefiniteForm { get; private set; } = new(false);

		//This line keeps the Seleucids Seleucid and not "[Dynasty]s"
		[SerializedName("ruler_uses_title_name")] public PDXBool RulerUsesTitleName { get; set; } = new(false);

		[SerializedName("ai_primary_priority")] public StringOfItem? AIPrimaryPriority { get; private set; }
		[SerializedName("can_create")] public StringOfItem? CanCreate { get; private set; }
		[SerializedName("can_create_on_partition")] public StringOfItem? CanCreateOnPartition { get; private set; }
		[SerializedName("destroy_if_invalid_heir")] public PDXBool? DestroyIfInvalidHeir { get; set; }
		[SerializedName("no_automatic_claims")] public PDXBool? NoAutomaticClaims { get; set; }
		[SerializedName("always_follows_primary_heir")] public PDXBool? AlwaysFollowsPrimaryHeir { get; set; }
		[SerializedName("de_jure_drift_disabled")] public PDXBool? DeJureDriftDisabled { get; set; }
		[SerializedName("can_be_named_after_dynasty")] public PDXBool? CanBeNamedAfterDynasty { get; set; }
		[SerializedName("male_names")] public List<string>? MaleNames { get; private set; }
		// <culture, loc key>
		[SerializedName("cultural_names")] public Dictionary<string, string>? CulturalNames { get; private set; }

		[NonSerialized]
		public int? OwnOrInheritedDevelopmentLevel {
			get {
				if (history.DevelopmentLevel is not null) { // if development level is already set, just return it
					return history.DevelopmentLevel;
				}
				if (deJureLiege is not null) { // if de jure liege exists, return their level
					return deJureLiege.OwnOrInheritedDevelopmentLevel;
				}
				return null;
			}
		}
		[NonSerialized] public SortedSet<string> SuccessionLaws { get; private set; } = new();
		[NonSerialized] public bool IsImportedOrUpdatedFromImperator { get; private set; } = false;

		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(@"(k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleNameStr) => {
				// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
				var newTitle = new Title(titleNameStr);
				newTitle.LoadTitles(reader, parser.Variables);

				if (newTitle.Rank == TitleRank.barony && string.IsNullOrEmpty(CapitalBarony)) {
					// title is a barony, and no other barony has been found in this scope yet
					CapitalBarony = newTitle.Id;
				}

				AddFoundTitle(newTitle, foundTitles);
				newTitle.DeJureLiege = this;
			});
			parser.RegisterKeyword("definite_form", reader => HasDefiniteForm = reader.GetPDXBool());
			parser.RegisterKeyword("ruler_uses_title_name", reader => RulerUsesTitleName = reader.GetPDXBool());
			parser.RegisterKeyword("landless", reader => Landless = reader.GetPDXBool());
			parser.RegisterKeyword("color", reader => Color1 = colorFactory.GetColor(reader));
			parser.RegisterKeyword("color2", reader => Color2 = colorFactory.GetColor(reader));
			parser.RegisterKeyword("capital", reader => parsedCapitalCountyName = reader.GetString());
			parser.RegisterKeyword("ai_primary_priority", reader => AIPrimaryPriority = reader.GetStringOfItem());
			parser.RegisterKeyword("can_create", reader => CanCreate = reader.GetStringOfItem());
			parser.RegisterKeyword("can_create_on_partition", reader => CanCreateOnPartition = reader.GetStringOfItem());
			parser.RegisterKeyword("province", reader => Province = reader.GetULong());
			parser.RegisterKeyword("destroy_if_invalid_heir", reader => DestroyIfInvalidHeir = reader.GetPDXBool());
			parser.RegisterKeyword("no_automatic_claims", reader => NoAutomaticClaims = reader.GetPDXBool());
			parser.RegisterKeyword("always_follows_primary_heir", reader => AlwaysFollowsPrimaryHeir = reader.GetPDXBool());
			parser.RegisterKeyword("de_jure_drift_disabled", reader => DeJureDriftDisabled = reader.GetPDXBool());
			parser.RegisterKeyword("can_be_named_after_dynasty", reader => CanBeNamedAfterDynasty = reader.GetPDXBool());
			parser.RegisterKeyword("male_names", reader => MaleNames = reader.GetStrings());
			parser.RegisterKeyword("cultural_names", reader => CulturalNames = reader.GetAssignments());

			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}

		internal void ClearHolderSpecificHistory() {
			history.InternalHistory.Fields.Remove("holder");
			history.InternalHistory.Fields.Remove("government");
		}

		internal static void AddFoundTitle(Title newTitle, Dictionary<string, Title> foundTitles) {
			foreach (var (locatedTitleName, locatedTitle) in newTitle.foundTitles) {
				if (newTitle.Rank == TitleRank.county) {
					var baronyProvince = locatedTitle.Province;
					if (baronyProvince is not null) {
						if (locatedTitleName == newTitle.CapitalBarony) {
							newTitle.CapitalBaronyProvince = (ulong)baronyProvince;
						}
						newTitle.AddCountyProvince((ulong)baronyProvince); // add found baronies' provinces to countyProvinces
					}
				}
				foundTitles[locatedTitleName] = locatedTitle;
			}
			// now that all titles under newTitle have been moved to main foundTitles, newTitle's foundTitles can be cleared
			newTitle.foundTitles.Clear();

			// And then add this one as well, overwriting existing.
			foundTitles[newTitle.Id] = newTitle;
		}

		private TitleHistory history = new();
		private readonly Dictionary<string, Title> foundTitles = new(); // title name, title. Titles are only held here during loading of landed_titles, then they are cleared		// used by duchy titles only

		private static readonly ColorFactory colorFactory = new();

		private void SetRank() {
			if (Id.StartsWith('b')) {
				Rank = TitleRank.barony;
			} else if (Id.StartsWith('c')) {
				Rank = TitleRank.county;
			} else if (Id.StartsWith('d')) {
				Rank = TitleRank.duchy;
			} else if (Id.StartsWith('k')) {
				Rank = TitleRank.kingdom;
			} else if (Id.StartsWith('e')) {
				Rank = TitleRank.empire;
			} else {
				throw new System.FormatException($"Title {Id}: unknown rank!");
			}
		}

		public void OutputHistory(StreamWriter writer, Date conversionDate) {
			bool needsToBeOutput = false;
			var sb = new StringBuilder();

			sb.AppendLine($"{Id} = {{");

			if (history.InternalHistory.Fields.ContainsKey("holder")) {
				needsToBeOutput = true;
				foreach (var (date, holderId) in history.InternalHistory.Fields["holder"].ValueHistory) {
					sb.AppendLine($"\t{date} = {{ holder = {holderId} }}");
				}
			}

			if (history.InternalHistory.Fields.ContainsKey("government")) {
				var govField = history.InternalHistory.Fields["government"];
				var initialGovernment = govField.InitialValue;
				if (initialGovernment is not null) {
					needsToBeOutput = true;
					sb.AppendLine($"\t\tgovernment = {initialGovernment}");
				}
				foreach (var (date, government) in govField.ValueHistory) {
					needsToBeOutput = true;
					sb.AppendLine($"\t{date} = {{ government = {government} }}");
				}
			}

			sb.AppendLine($"\t{conversionDate}={{");

			if (DeFactoLiege is not null) {
				needsToBeOutput = true;
				sb.AppendLine($"\t\tliege = {DeFactoLiege.Id}");
			}

			var succLaws = SuccessionLaws;
			if (succLaws.Count > 0) {
				needsToBeOutput = true;
				sb.AppendLine("\t\tsuccession_laws = {");
				foreach (var law in succLaws) {
					sb.AppendLine($"\t\t\t{law}");
				}
				sb.AppendLine("\t\t}");
			}

			if (Rank != TitleRank.barony) {
				var developmentLevelOpt = DevelopmentLevel;
				if (developmentLevelOpt is not null) {
					needsToBeOutput = true;
					sb.AppendLine($"\t\tchange_development_level = {developmentLevelOpt}");
				}
			}

			sb.AppendLine("\t}");

			sb.AppendLine("}");

			if (needsToBeOutput) {
				writer.Write(sb);
			}
		}

		public HashSet<ulong> GetProvincesInCountry(LandedTitles titles, Date ck3BookmarkDate) {
			var holderId = GetHolderId(ck3BookmarkDate);
			var heldCounties = new List<Title>(
				titles.Where(t => t.GetHolderId(ck3BookmarkDate) == holderId && t.Rank == TitleRank.county)
			);
			var heldProvinces = new HashSet<ulong>();
			// add directly held counties
			foreach (var county in heldCounties) {
				heldProvinces.UnionWith(county.CountyProvinces);
			}
			// add vassals' counties
			foreach (var vassal in GetDeFactoVassalsAndBelow().Values) {
				var vassalHolderId = vassal.GetHolderId(ck3BookmarkDate);
				if (vassalHolderId == "0") {
					Logger.Warn($"Player title {Id}'s vassal {vassal.Id} has 0 holder!");
					continue;
				}
				var heldVassalCounties = new List<Title>(
					titles.Where(t => t.GetHolderId(ck3BookmarkDate) == vassalHolderId && t.Rank == TitleRank.county)
				);
				foreach (var vassalCounty in heldVassalCounties) {
					heldProvinces.UnionWith(vassalCounty.CountyProvinces);
				}
			}
			return heldProvinces;
		}

		[NonSerialized] public static HashSet<string> IgnoredTokens { get; } = new();

		// used by kingdom titles only
		public bool KingdomContainsProvince(ulong provinceId) {
			if (Rank != TitleRank.kingdom) {
				return false;
			}

			return DeJureVassals.Any(vassal => vassal.Rank == TitleRank.duchy && vassal.DuchyContainsProvince(provinceId));
		}

		// used by duchy titles only
		public bool DuchyContainsProvince(ulong provinceId) {
			if (Rank != TitleRank.duchy) {
				return false;
			}

			return DeJureVassals.Any(vassal => vassal.Rank == TitleRank.county && vassal.CountyProvinces.Contains(provinceId));
		}

		// used by county titles only
		public void AddCountyProvince(ulong provinceId) {
			CountyProvinces.Add(provinceId);
		}
		[NonSerialized] public SortedSet<ulong> CountyProvinces { get; } = new();
		[NonSerialized] public string CapitalBarony { get; private set; } = string.Empty; // used when parsing inside county to save first barony
		[NonSerialized] public ulong? CapitalBaronyProvince { get; private set; } // county barony's province; 0 is not a valid barony ID

		// used by barony titles only
		[SerializedName("province")] public ulong? Province { get; private set; } // province is area on map. b_ barony is its corresponding title.

		public void RemoveHistoryPastBookmarkDate(Date ck3BookmarkDate) {
			history.RemoveHistoryPastBookmarkDate(ck3BookmarkDate);
		}
	}
}
