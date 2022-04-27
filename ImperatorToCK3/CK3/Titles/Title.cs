using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
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

namespace ImperatorToCK3.CK3.Titles;

public enum TitleRank { barony, county, duchy, kingdom, empire }
public sealed partial class Title : IPDXSerializable, IIdentifiable<string> {
	public override string ToString() {
		return Id;
	}

	private Title(LandedTitles parentCollection, string id) {
		this.parentCollection = parentCollection;
		Id = id;
		SetRank();
	}

	private Title(LandedTitles parentCollection,
		Country country,
		CountryCollection imperatorCountries,
		LocDB locDB,
		ProvinceMapper provinceMapper,
		CoaMapper coaMapper,
		TagTitleMapper tagTitleMapper,
		GovernmentMapper governmentMapper,
		SuccessionLawMapper successionLawMapper,
		DefiniteFormMapper definiteFormMapper,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		NicknameMapper nicknameMapper,
		CharacterCollection characters,
		Date conversionDate,
		Configuration config
	) {
		this.parentCollection = parentCollection;
		Id = DetermineName(country, imperatorCountries, tagTitleMapper, locDB);
		SetRank();
		InitializeFromTag(
			country,
			imperatorCountries,
			locDB,
			provinceMapper,
			coaMapper,
			governmentMapper,
			successionLawMapper,
			definiteFormMapper,
			religionMapper,
			cultureMapper,
			nicknameMapper,
			characters,
			conversionDate,
			config
		);
	}
	private Title(LandedTitles parentCollection,
		string id,
		Governorship governorship,
		Country country,
		Imperator.Characters.CharacterCollection imperatorCharacters,
		bool regionHasMultipleGovernorships,
		LocDB locDB,
		ProvinceMapper provinceMapper,
		CoaMapper coaMapper,
		DefiniteFormMapper definiteFormMapper,
		ImperatorRegionMapper imperatorRegionMapper
	) {
		this.parentCollection = parentCollection;
		Id = id;
		SetRank();
		InitializeFromGovernorship(
			governorship,
			country,
			imperatorCharacters,
			regionHasMultipleGovernorships,
			locDB,
			provinceMapper,
			definiteFormMapper,
			imperatorRegionMapper
		);
	}
	public void InitializeFromTag(
		Country country,
		CountryCollection imperatorCountries,
		LocDB locDB,
		ProvinceMapper provinceMapper,
		CoaMapper coaMapper,
		GovernmentMapper governmentMapper,
		SuccessionLawMapper successionLawMapper,
		DefiniteFormMapper definiteFormMapper,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		NicknameMapper nicknameMapper,
		CharacterCollection characters,
		Date conversionDate,
		Configuration config
	) {
		IsImportedOrUpdatedFromImperator = true;
		ImperatorCountry = country;
		ImperatorCountry.CK3Title = this;

		LocBlock? validatedName = GetValidatedName(country, imperatorCountries, locDB);

		HasDefiniteForm.Value = definiteFormMapper.IsDefiniteForm(ImperatorCountry.Name);
		RulerUsesTitleName.Value = false;

		PlayerCountry = ImperatorCountry.PlayerCountry;

		ClearHolderSpecificHistory();

		FillHolderAndGovernmentHistory();

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
		History.InternalHistory.AddFieldValue("succession_laws",
			successionLawMapper.GetCK3LawsForImperatorLaws(ImperatorCountry.GetLaws()),
			conversionDate,
			"succession_laws"
		);

		// determine CoA
		CoA = coaMapper.GetCoaForFlagName(ImperatorCountry.Flag);

		// Determine other attributes:
		// Set capital to Imperator tag's capital.
		if (ImperatorCountry.Capital is not null) {
			var srcCapital = (ulong)ImperatorCountry.Capital;
			foreach (var ck3ProvId in provinceMapper.GetCK3ProvinceNumbers(srcCapital)) {
				var foundCounty = parentCollection.GetCountyForProvince(ck3ProvId);
				if (foundCounty is null) {
					continue;
				}

				// If the title is a de jure duchy, potential capital must be within it.
				if (Rank == TitleRank.duchy && DeJureVassals.Count > 0 && foundCounty.DeJureLiege?.Id != Id) {
					continue;
				}

				CapitalCounty = foundCounty;
				break;
			}
		}

		// determine country name localization
		var nameSet = false;
		if (validatedName is not null) {
			var nameLocBlock = Localizations.AddLocBlock(Id);
			nameLocBlock.CopyFrom(validatedName);
			nameSet = true;
		}
		if (!nameSet) {
			var impTagLoc = locDB.GetLocBlockForKey(ImperatorCountry.Tag);
			if (impTagLoc is not null) {
				var nameLocBlock = Localizations.AddLocBlock(Id);
				nameLocBlock.CopyFrom(impTagLoc);
				nameSet = true;
			}
		}
		if (!nameSet) {
			// use unlocalized name if not empty
			var name = ImperatorCountry.Name;
			if (!string.IsNullOrEmpty(name)) {
				Logger.Warn($"Using unlocalized Imperator name {name} as name for {Id}!");
				var nameLocBlock = Localizations.AddLocBlock(Id);
				nameLocBlock["english"] = name;
				nameSet = true;
			}
		}
		// giving up
		if (!nameSet) {
			Logger.Warn($"{Id} needs help with localization! {ImperatorCountry.Name}?");
		}

		// determine adjective localization
		TrySetAdjectiveLoc(locDB, imperatorCountries);

		void FillHolderAndGovernmentHistory() {
			// ------------------ determine previous and current holders
			// there was no 0 AD, but year 0 works in game and serves well for adding BC characters to holder history
			var firstPossibleDate = new Date(0, 1, 1);
			foreach (var impRulerTerm in ImperatorCountry.RulerTerms) {
				var rulerTerm = new RulerTerm(
					impRulerTerm,
					characters,
					governmentMapper,
					locDB,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					provinceMapper,
					config
				);

				var characterId = rulerTerm.CharacterId;
				var gov = rulerTerm.Government;

				var startDate = new Date(rulerTerm.StartDate);
				if (startDate < firstPossibleDate) {
					startDate = new Date(firstPossibleDate); // TODO: remove this workaround if CK3 supports negative dates
					firstPossibleDate.ChangeByDays(1);
				}

				History.InternalHistory.AddFieldValue("holder", characterId, startDate, "holder");
				if (gov is not null) {
					History.InternalHistory.AddFieldValue("government", gov, startDate, "government");
				}
			}

			if (ImperatorCountry.Government is not null) {
				var lastCK3TermGov = History.GetGovernment(conversionDate);
				var ck3CountryGov = governmentMapper.GetCK3GovernmentForImperatorGovernment(ImperatorCountry.Government);
				if (lastCK3TermGov != ck3CountryGov && ck3CountryGov is not null) {
					History.InternalHistory.AddFieldValue("government", ck3CountryGov, conversionDate, "government");
				}
			}
		}
	}

	internal void RemoveDeFactoLiegeReferences(string name) {
		if (!History.InternalHistory.Fields.TryGetValue("liege", out var liegeField)) {
			return;
		}

		liegeField.ValueHistory = new SortedDictionary<Date, object>(liegeField.ValueHistory.Where(
				kvp => !(kvp.Value is string vStr && vStr == name)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
		);
		if (liegeField.InitialValue is string str && str == name) {
			liegeField.InitialValue = null;
		}
	}

	private static LocBlock? GetValidatedName(Country imperatorCountry, CountryCollection imperatorCountries, LocDB locDB) {
		return imperatorCountry.Name switch {
			// hard code for Antigonid Kingdom, Seleucid Empire and Maurya
			// these countries use customizable localization for name and adjective
			"PRY_DYN" => locDB.GetLocBlockForKey("get_pry_name_fallback"),
			"SEL_DYN" => locDB.GetLocBlockForKey("get_sel_name_fallback"),
			"MRY_DYN" => locDB.GetLocBlockForKey("get_mry_name_fallback"),
			_ => imperatorCountry.CountryName.GetNameLocBlock(locDB, imperatorCountries)
		};
	}

	public static string DetermineName(
		Country imperatorCountry,
		CountryCollection imperatorCountries,
		TagTitleMapper tagTitleMapper,
		LocDB locDB
	) {
		var validatedName = GetValidatedName(imperatorCountry, imperatorCountries, locDB);
		var validatedEnglishName = validatedName?["english"];

		string? title;

		if (validatedEnglishName is not null) {
			title = tagTitleMapper.GetTitleForTag(imperatorCountry, validatedEnglishName);
		} else {
			title = tagTitleMapper.GetTitleForTag(imperatorCountry);
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
		LocDB locDB,
		ProvinceMapper provinceMapper,
		DefiniteFormMapper definiteFormMapper,
		ImperatorRegionMapper imperatorRegionMapper
	) {
		var normalizedStartDate = governorship.StartDate.Year > 0 ? governorship.StartDate : new Date(1, 1, 1);

		IsImportedOrUpdatedFromImperator = true;

		if (country.CK3Title is null) {
			throw new System.ArgumentException($"{country.Tag} governorship of {governorship.RegionName} could not be mapped to CK3 title: liege doesn't exist!");
		}

		ClearHolderSpecificHistory();

		DeJureLiege = country.CK3Title;
		SetDeFactoLiege(country.CK3Title, normalizedStartDate);

		HasDefiniteForm.Value = definiteFormMapper.IsDefiniteForm(governorship.RegionName);
		RulerUsesTitleName.Value = false;

		PlayerCountry = false;

		var impGovernor = imperatorCharacters[governorship.CharacterId];

		// ------------------ determine holder
		History.InternalHistory.AddFieldValue("holder", $"imperator{impGovernor.Id}", normalizedStartDate, "holder");

		// ------------------ determine government
		var ck3LiegeGov = country.CK3Title.GetGovernment(normalizedStartDate);
		if (ck3LiegeGov is not null) {
			History.InternalHistory.AddFieldValue("government", ck3LiegeGov, normalizedStartDate, "government");
		}

		// ------------------ determine color
		var countryColor = country.Color1;
		if (countryColor is not null) {
			Color1 = parentCollection.GetDerivedColor(countryColor);
		}
		var color2Opt = country.Color2;
		if (color2Opt is not null) {
			Color2 = color2Opt;
		}

		// determine successions laws
		// https://github.com/ParadoxGameConverters/ImperatorToCK3/issues/90#issuecomment-817178552
		History.InternalHistory.AddFieldValue("succession_laws",
			new SortedSet<string> { "high_partition_succession_law" },
			normalizedStartDate,
			"succession_laws"
		);

		// ------------------ determine CoA
		CoA = null; // using game-randomized CoA

		// ------------------ determine capital
		var governorProvince = impGovernor.ProvinceId;
		if (imperatorRegionMapper.ProvinceIsInRegion(governorProvince, governorship.RegionName)) {
			foreach (var ck3Prov in provinceMapper.GetCK3ProvinceNumbers(governorProvince)) {
				var foundCounty = parentCollection.GetCountyForProvince(ck3Prov);
				if (foundCounty is not null) {
					CapitalCounty = foundCounty;
					break;
				}
			}
		}

		TrySetNameFromGovernorship(governorship, country, regionHasMultipleGovernorships, locDB);
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
			if (ck3Country.Localizations.TryGetValue($"{ck3Country.Id}_adj", out var countryAdjectiveLocBlock)) {
				var adjLocBlock = Localizations.AddLocBlock(adjKey);
				adjLocBlock.CopyFrom(countryAdjectiveLocBlock);
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
		LocDB locDB
	) {
		if (!Localizations.ContainsKey(Id)) {
			var nameSet = false;
			LocBlock? regionLocBlock = locDB.GetLocBlockForKey(governorship.RegionName);

			if (regionHasMultipleGovernorships && regionLocBlock is not null) {
				var ck3Country = country.CK3Title;
				if (ck3Country is not null && ck3Country.Localizations.TryGetValue(ck3Country.Id + "_adj", out var countryAdjectiveLocBlock)) {
					var nameLocBlock = Localizations.AddLocBlock(Id);
					nameLocBlock.CopyFrom(regionLocBlock);
					nameLocBlock.ModifyForEveryLanguage(countryAdjectiveLocBlock,
						(orig, adj) => $"{adj} {orig}"
					);
					nameSet = true;
				}
			}
			if (!nameSet && regionLocBlock is not null) {
				var nameLocBlock = Localizations.AddLocBlock(Id);
				nameLocBlock.CopyFrom(regionLocBlock);
				nameSet = true;
			}
			if (!nameSet) {
				Logger.Warn($"{Id} needs help with localization!");
			}
		}
	}

	public void LoadTitles(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);

		TrySetCapitalBarony();
	}

	public Date GetDateOfLastHolderChange() {
		var field = History.InternalHistory.Fields["holder"];
		var dates = new SortedSet<Date>(field.ValueHistory.Keys);
		var lastDate = dates.Max;
		return lastDate ?? new Date(1, 1, 1);
	}
	public string GetHolderId(Date date) {
		return History.GetHolderId(date);
	}
	public HashSet<string> GetAllHolderIds() {
		if (History.InternalHistory.Fields.TryGetValue("holder", out var holderField)) {
			var ids = holderField.ValueHistory.Values.OfType<string>().ToHashSet();
			if (holderField.InitialValue is not null) {
				ids.Add((string)holderField.InitialValue);
			}

			return ids;
		}
		else {
			return new HashSet<string>();
		}
	}
	public void SetHolder(Character? character, Date date) {
		var id = character is null ? "0" : character.Id;
		History.InternalHistory.AddFieldValue("holder", id, date, "holder");
	}
	public string? GetGovernment(Date date) {
		return History.GetGovernment(date);
	}

	public int? GetDevelopmentLevel(Date date) {
		return History.GetDevelopmentLevel(date);
	}
	public void SetDevelopmentLevel(int value, Date date) {
		if (Rank == TitleRank.barony) {
			Logger.Warn($"Cannot set development level to a barony title {Id}!");
			return;
		}
		History.InternalHistory.AddFieldValue("development_level", value, date, "change_development_level");
	}

	[NonSerialized] public LocDB Localizations { get; } = new("english", "french", "german", "russian", "simp_chinese", "spanish");

	private void TrySetAdjectiveLoc(LocDB LocDB, CountryCollection imperatorCountries) {
		if (ImperatorCountry is null) {
			Logger.Warn($"Cannot set adjective for CK3 Title {Id} from null Imperator Country!");
			return;
		}

		var adjSet = false;
		var locKey = Id + "_adj";

		if (ImperatorCountry.Tag is "PRY" or "SEL" or "MRY") {
			// these tags use customizable loc for adj
			LocBlock? validatedAdj = ImperatorCountry.Name switch {
				"PRY_DYN" => LocDB.GetLocBlockForKey("get_pry_adj_fallback"),
				"SEL_DYN" => LocDB.GetLocBlockForKey("get_sel_adj_fallback"),
				"MRY_DYN" => LocDB.GetLocBlockForKey("get_mry_adj_fallback"),
				_ => null
			};

			if (validatedAdj is not null) {
				var adjLocBlock = Localizations.AddLocBlock(locKey);
				adjLocBlock.CopyFrom(validatedAdj);
				adjSet = true;
			}
		}
		if (!adjSet) {
			var adjOpt = ImperatorCountry.CountryName.GetAdjectiveLocBlock(LocDB, imperatorCountries);
			if (adjOpt is not null) {
				var adjLocBlock = Localizations.AddLocBlock(locKey);
				adjLocBlock.CopyFrom(adjOpt);
				adjSet = true;
			}
		}
		if (!adjSet) {
			var adjLocalizationMatch = LocDB.GetLocBlockForKey(ImperatorCountry.Tag);
			if (adjLocalizationMatch is not null) {
				var adjLocBlock = Localizations.AddLocBlock(locKey);
				adjLocBlock.CopyFrom(adjLocalizationMatch);
				adjSet = true;
			}
		}
		if (!adjSet) {
			// use unlocalized name if not empty
			var name = ImperatorCountry.Name;
			if (!string.IsNullOrEmpty(name)) {
				Logger.Warn($"Using unlocalized Imperator name {name} as adjective for {Id}!");
				var adjLocBlock = Localizations.AddLocBlock(locKey);
				adjLocBlock["english"] = name;
				adjSet = true;
			}
		}
		// giving up
		if (!adjSet) {
			Logger.Warn($"{Id} needs help with localization for adjective! {ImperatorCountry.Name}_adj?");
		}
	}
	public void AddHistory(TitleHistory titleHistory) {
		History = titleHistory;
	}

	[NonSerialized] public string? CoA { get; private set; }

	[SerializedName("capital")] public string? CapitalCountyId { get; private set; }
	[NonSerialized]
	public Title? CapitalCounty {
		get => CapitalCountyId is null ? null : parentCollection[CapitalCountyId];
		private set => CapitalCountyId = value?.Id;
	}

	[NonSerialized] public Country? ImperatorCountry { get; private set; }

	[SerializedName("color")] public Color? Color1 { get; set; }
	[SerializedName("color2")] public Color? Color2 { get; set; }

	private Title? deJureLiege;
	[NonSerialized]
	public Title? DeJureLiege { // direct de jure liege title
		get => deJureLiege;
		set {
			if (value is not null && value.Rank <= Rank) {
				Logger.Warn($"Cannot set de jure liege {value} to {Id}: rank is not higher!");
				return;
			}
			deJureLiege?.DeJureVassals.Remove(Id);
			deJureLiege = value;
			if (value is not null) {
				value.DeJureVassals.Add(this);
			}
		}
	}
	public Title? GetDeFactoLiege(Date date) { // direct de facto liege title
		var liegeStr = History.GetLiege(date);
		if (liegeStr is not null && parentCollection.TryGetValue(liegeStr, out var liegeTitle)) {
			return liegeTitle;
		}

		return null;
	}
	public void SetDeFactoLiege(Title? newLiege, Date date) {
		if (newLiege is not null && newLiege.Rank <= Rank) {
			Logger.Warn($"Cannot set de facto liege {newLiege} to {Id}: rank is not higher!");
			return;
		}

		const string fieldName = "liege";
		if (newLiege is null) {
			History.InternalHistory.AddFieldValue(fieldName, 0, date, fieldName);
		} else {
			History.InternalHistory.AddFieldValue(fieldName, newLiege.Id, date, fieldName);
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
	public Dictionary<string, Title> GetDeFactoVassals(Date date) { // DIRECT de facto vassals
		return parentCollection.Where(t => t.GetDeFactoLiege(date)?.Id == Id)
			.ToDictionary(t => t.Id, t => t);
	}
	public Dictionary<string, Title> GetDeFactoVassalsAndBelow(Date date) {
		return GetDeFactoVassalsAndBelow(date, "bcdke");
	}
	public Dictionary<string, Title> GetDeFactoVassalsAndBelow(Date date, string rankFilter) {
		var rankFilterAsArray = rankFilter.ToCharArray();
		Dictionary<string, Title> deFactoVassalsAndBelow = new();
		foreach (var (vassalTitleName, vassalTitle) in GetDeFactoVassals(date)) {
			// add the direct part
			if (vassalTitleName.IndexOfAny(rankFilterAsArray) == 0) {
				deFactoVassalsAndBelow[vassalTitleName] = vassalTitle;
			}

			// add the "below" part (recursive)
			var belowTitles = vassalTitle.GetDeFactoVassalsAndBelow(date, rankFilter);
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

	public int? GetOwnOrInheritedDevelopmentLevel(Date date) {
		var ownDev = History.GetDevelopmentLevel(date);
		if (ownDev is not null) { // if development level is already set, just return it
			return ownDev;
		}
		if (deJureLiege is not null) { // if de jure liege exists, return their level
			return deJureLiege.GetOwnOrInheritedDevelopmentLevel(date);
		}
		return null;
	}

	public ICollection<string> GetSuccessionLaws(Date date) {
		switch (History.InternalHistory.GetFieldValue("succession_laws", date)) {
			case null:
				return new SortedSet<string>();
			case ICollection<string> stringCollection:
				return stringCollection;
			case ICollection<object> objectCollection:
				var setToReturn = new SortedSet<string>();
				foreach (var item in objectCollection) {
					var itemStr = item.ToString();
					if (itemStr is null) {
						continue;
					}
					setToReturn.Add(itemStr);
				}
				return setToReturn;
			default:
				return new SortedSet<string>();
		}
	}
	[NonSerialized] public bool IsImportedOrUpdatedFromImperator { get; private set; } = false;

	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(@"(k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleNameStr) => {
			// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
			var newTitle = parentCollection.Add(titleNameStr);
			newTitle.LoadTitles(reader);

			if (newTitle.Rank == TitleRank.barony && string.IsNullOrEmpty(CapitalBaronyId)) {
				// title is a barony, and no other barony has been found in this scope yet
				CapitalBaronyId = newTitle.Id;
			}

			newTitle.DeJureLiege = this;
		});
		parser.RegisterKeyword("definite_form", reader => HasDefiniteForm = reader.GetPDXBool());
		parser.RegisterKeyword("ruler_uses_title_name", reader => RulerUsesTitleName = reader.GetPDXBool());
		parser.RegisterKeyword("landless", reader => Landless = reader.GetPDXBool());
		parser.RegisterKeyword("color", reader => Color1 = colorFactory.GetColor(reader));
		parser.RegisterKeyword("color2", reader => Color2 = colorFactory.GetColor(reader));
		parser.RegisterKeyword("capital", reader => CapitalCountyId = reader.GetString());
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
	private void TrySetCapitalBarony() {
		if (Rank != TitleRank.county) {
			return;
		}

		foreach (var deJureVassal in DeJureVassals) {
			if (deJureVassal.Province is null) {
				continue;
			}
			ulong baronyProvinceId = (ulong)deJureVassal.Province;

			if (deJureVassal.Id == CapitalBaronyId) {
				CapitalBaronyProvince = baronyProvinceId;
				break;
			}
		}
	}

	internal void ClearHolderSpecificHistory() {
		History.InternalHistory.Fields.Remove("holder");
		History.InternalHistory.Fields.Remove("government");
		History.InternalHistory.Fields.Remove("liege");
	}

	[NonSerialized] public TitleHistory History { get; private set; } = new();
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

	public void OutputHistory(StreamWriter writer) {
		var sb = new StringBuilder();
		var content = PDXSerializer.Serialize(History.InternalHistory, "\t");
		if (string.IsNullOrWhiteSpace(content)) {
			// doesn't need to be output
			return;
		}

		sb.Append(Id).AppendLine("={").Append(content).AppendLine("}");
		writer.Write(sb);
	}

	public HashSet<ulong> GetProvincesInCountry(Date date) {
		var holderId = GetHolderId(date);
		var heldCounties = new List<Title>(
			parentCollection.Where(t => t.GetHolderId(date) == holderId && t.Rank == TitleRank.county)
		);
		var heldProvinces = new HashSet<ulong>();
		// add directly held counties
		foreach (var county in heldCounties) {
			heldProvinces.UnionWith(county.CountyProvinces);
		}
		// add vassals' counties
		foreach (var vassal in GetDeFactoVassalsAndBelow(date).Values) {
			var vassalHolderId = vassal.GetHolderId(date);
			if (vassalHolderId == "0") {
				Logger.Warn($"Player title {Id}'s vassal {vassal} has 0 holder!");
				continue;
			}
			var heldVassalCounties = new List<Title>(
				parentCollection.Where(t => t.GetHolderId(date) == vassalHolderId && t.Rank == TitleRank.county)
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

	public Title? GetRealmOfRank(TitleRank realmRank, Date ck3BookmarkDate) {
		var holderId = GetHolderId(ck3BookmarkDate);
		if (holderId == "0") {
			return null;
		}

		if (realmRank == Rank) {
			return this;
		}

		// case: title is not independent
		var dfLiege = GetDeFactoLiege(ck3BookmarkDate);
		while (dfLiege is not null) { // title is not independent
			if (dfLiege.Rank == realmRank) {
				return dfLiege;
			}
			dfLiege = dfLiege.GetDeFactoLiege(ck3BookmarkDate);
		}

		// case: title is independent
		var higherTitlesOfHolder = parentCollection.Where(t => t.GetHolderId(ck3BookmarkDate) == holderId && t.Rank > Rank)
			.OrderByDescending(t => t.Rank);
		var highestTitleRank = higherTitlesOfHolder.FirstOrDefault(defaultValue: null)?.Rank;
		if (highestTitleRank is null) {
			return null;
		}
		foreach (var title in higherTitlesOfHolder.Where(t => t.Rank == highestTitleRank)) {
			if (title.Rank == realmRank) {
				return title;
			}
			var realm = title.GetRealmOfRank(realmRank, ck3BookmarkDate);
			if (realm is not null) {
				return realm;
			}
		}

		return null;
	}

	// used by county titles only
	[NonSerialized] public IEnumerable<ulong> CountyProvinces => DeJureVassals.Where(v => v.Rank == TitleRank.barony).Select(v => (ulong)v.Province!);
	[NonSerialized] private string CapitalBaronyId { get; set; } = string.Empty; // used when parsing inside county to save first barony
	[NonSerialized] public ulong? CapitalBaronyProvince { get; private set; } // county barony's province; 0 is not a valid barony ID

	// used by barony titles only
	[SerializedName("province")] public ulong? Province { get; private set; } // province is area on map. b_barony is its corresponding title.

	public void RemoveHistoryPastBookmarkDate(Date ck3BookmarkDate) {
		History.RemoveHistoryPastBookmarkDate(ck3BookmarkDate);
	}
}