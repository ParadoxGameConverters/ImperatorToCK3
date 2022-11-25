using commonItems;
using commonItems.Collections;
using commonItems.Linguistics;
using commonItems.Localization;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
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
using System;
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
		IsCreatedFromImperator = true;
		this.parentCollection = parentCollection;
		Id = DetermineId(country, imperatorCountries, tagTitleMapper, locDB);
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
		Imperator.Provinces.ProvinceCollection irProvinces,
		Imperator.Characters.CharacterCollection imperatorCharacters,
		bool regionHasMultipleGovernorships,
		bool staticDeJure,
		LocDB locDB,
		ProvinceMapper provinceMapper,
		CoaMapper coaMapper,
		DefiniteFormMapper definiteFormMapper,
		ImperatorRegionMapper imperatorRegionMapper
	) {
		IsCreatedFromImperator = true;
		this.parentCollection = parentCollection;
		Id = id;
		SetRank();
		InitializeFromGovernorship(
			governorship,
			country,
			irProvinces,
			imperatorCharacters,
			regionHasMultipleGovernorships,
			staticDeJure,
			locDB,
			provinceMapper,
			definiteFormMapper,
			imperatorRegionMapper
		);
	}

	private Title(Title? baseTitle, Title overrideTitle, LandedTitles parentCollection) {
		// Merge titles.
		this.parentCollection = parentCollection;

		Id = overrideTitle.Id;
		SetRank();

		if (baseTitle is not null) {
			if (baseTitle.Id != overrideTitle.Id) {
				Logger.Warn($"Merging {overrideTitle.Id} into {Id}! This is unsupported.");
			}
			// Copy vital items for a DeJure only title.
			HasDefiniteForm = baseTitle.HasDefiniteForm;
			RulerUsesTitleName = baseTitle.RulerUsesTitleName;

			Color1 = baseTitle.Color1;
			History = baseTitle.History;
			CoA = baseTitle.CoA;

			CapitalCounty = baseTitle.CapitalCounty;
			Localizations = baseTitle.Localizations;

			AIPrimaryPriority = baseTitle.AIPrimaryPriority;
			RulerUsesTitleName = baseTitle.RulerUsesTitleName;
			CanBeNamedAfterDynasty = baseTitle.CanBeNamedAfterDynasty;
			MaleNames = baseTitle.MaleNames;
			CulturalNames = baseTitle.CulturalNames;

			// Bring base vassals into merged title
			foreach (var vassal in baseTitle.DeJureVassals) {
				vassal.DeJureLiege = this;
			}
		}

		DeJureLiege = overrideTitle.DeJureLiege;

		// Bring new LandedTitle.txt style specifications into merged Title
		if (overrideTitle.Color1 is not null) {
			Color1 = overrideTitle.Color1;
		}
		if (overrideTitle.CapitalCountyId is not null) {
			CapitalCountyId = overrideTitle.CapitalCountyId;
		}
		if (overrideTitle.AIPrimaryPriority is not null) {
			AIPrimaryPriority = overrideTitle.AIPrimaryPriority;
		}

		// Bring new vassals into merged title
		foreach (var vassal in overrideTitle.DeJureVassals) {
			vassal.DeJureLiege = this;
		}
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
		ImperatorCountry = country;
		ImperatorCountry.CK3Title = this;

		LocBlock? validatedName = GetValidatedName(country, imperatorCountries, locDB);

		HasDefiniteForm = definiteFormMapper.IsDefiniteForm(ImperatorCountry.Name);
		RulerUsesTitleName = false;

		PlayerCountry = ImperatorCountry.PlayerCountry;

		ClearHolderSpecificHistory();

		FillHolderAndGovernmentHistory();

		// ------------------ determine color
		var color1Opt = ImperatorCountry.Color1;
		if (color1Opt is not null) {
			Color1 = color1Opt;
		}

		// determine successions laws
		History.AddFieldValue(conversionDate,
			"succession_laws",
			"succession_laws",
			successionLawMapper.GetCK3LawsForImperatorLaws(ImperatorCountry.GetLaws())
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

				var termStartDate = new Date(rulerTerm.StartDate);
				var ruler = characters[characterId];
				if (ruler.DeathDate is not null && ruler.DeathDate < termStartDate) {
					Logger.Warn($"{ruler.Id} can not begin his rule over {Id} after his death, skipping!");
					continue;
				}

				History.AddFieldValue(termStartDate, "holder", "holder", characterId);
				if (gov is not null) {
					History.AddFieldValue(termStartDate, "government", "government", gov);
				}
			}

			if (ImperatorCountry.Government is not null) {
				var lastCK3TermGov = GetGovernment(conversionDate);
				var ck3CountryGov = governmentMapper.GetCK3GovernmentForImperatorGovernment(ImperatorCountry.Government, ImperatorCountry.PrimaryCulture);
				if (lastCK3TermGov != ck3CountryGov && ck3CountryGov is not null) {
					History.AddFieldValue(conversionDate, "government", "government", ck3CountryGov);
				}
			}
		}
	}

	internal void RemoveDeFactoLiegeReferences(string liegeName) {
		if (!History.Fields.TryGetValue("liege", out var liegeField)) {
			return;
		}

		liegeField.RemoveAllEntries(v => v is string str && str == liegeName);
	}

	private static LocBlock? GetValidatedName(Country imperatorCountry, CountryCollection imperatorCountries, LocDB locDB) {
		switch (imperatorCountry.Name) {
			// Hard code for Antigonid Kingdom, Seleucid Empire and Maurya.
			// These countries use customizable localization for name and adjective.
			case "PRY_DYN" when imperatorCountry.Monarch?.Family?.Key == "Antigonid":
				var pryLocBlock = locDB.GetLocBlockForKey("get_pry_name_fetch");
				const string pryNameKey = "PRY";
				pryLocBlock?.ModifyForEveryLanguage(
					locDB.GetLocBlockForKey(pryNameKey) ?? new LocBlock(pryNameKey, "english") {
						["english"] = "Antigonid Kingdom"
					},
					(loc, modifyingLoc, _) => loc?.Replace($"${pryNameKey}$", modifyingLoc));
				return pryLocBlock;
			case "PRY_DYN":
				return locDB.GetLocBlockForKey("get_pry_name_fallback");
			case "SEL_DYN" when imperatorCountry.Monarch?.Family?.Key == "Seleukid":
				var selLocBlock = locDB.GetLocBlockForKey("get_sel_name_fetch");
				const string selNameKey = "SEL";
				selLocBlock?.ModifyForEveryLanguage(
					locDB.GetLocBlockForKey(selNameKey) ?? new LocBlock(selNameKey, "english") {
						["english"] = "Seleukid Empire"
					},
					(loc, modifyingLoc, _) => loc?.Replace($"${selNameKey}$", modifyingLoc));
				return selLocBlock;
			case "SEL_DYN":
				return locDB.GetLocBlockForKey("get_sel_name_fallback");
			case "MRY_DYN" when imperatorCountry.Monarch?.Family?.Key == "Maurya":
				var mryLocBlock = locDB.GetLocBlockForKey("get_mry_name_fetch");
				const string mryNameKey = "MRY";
				mryLocBlock?.ModifyForEveryLanguage(
					locDB.GetLocBlockForKey(mryNameKey) ?? new LocBlock(mryNameKey, "english") {
						["english"] = "Maurya"
					},
					(loc, modifyingLoc, _) => loc?.Replace($"${mryNameKey}$", modifyingLoc));
				return mryLocBlock;
			case "MRY_DYN":
				return locDB.GetLocBlockForKey("get_mry_name_fallback");
			default:
				return imperatorCountry.CountryName.GetNameLocBlock(locDB, imperatorCountries);
		}
	}

	public static string DetermineId(
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
			throw new ArgumentException($"Country {imperatorCountry.Tag} could not be mapped to CK3 Title!");
		}
		return title;
	}

	public static string? DetermineId(Governorship governorship, Country country, LandedTitles titles, ProvinceCollection provinces, ImperatorRegionMapper imperatorRegionMapper, TagTitleMapper tagTitleMapper) {
		if (country.CK3Title is null) {
			throw new ArgumentException($"{country.Tag} governorship of {governorship.RegionName} could not be mapped to CK3 title: country has no CK3Title!");
		}
		return tagTitleMapper.GetTitleForGovernorship(governorship, country, titles, provinces, imperatorRegionMapper);
	}

	public void InitializeFromGovernorship(
		Governorship governorship,
		Country country,
		Imperator.Provinces.ProvinceCollection irProvinces,
		Imperator.Characters.CharacterCollection imperatorCharacters,
		bool regionHasMultipleGovernorships,
		bool staticDeJure,
		LocDB locDB,
		ProvinceMapper provinceMapper,
		DefiniteFormMapper definiteFormMapper,
		ImperatorRegionMapper imperatorRegionMapper
	) {
		var governorshipStartDate = governorship.StartDate;

		if (country.CK3Title is null) {
			throw new ArgumentException($"{country.Tag} governorship of {governorship.RegionName} could not be mapped to CK3 title: liege doesn't exist!");
		}

		ClearHolderSpecificHistory();

		if (!staticDeJure) {
			DeJureLiege = country.CK3Title;
		}
		SetDeFactoLiege(country.CK3Title, governorshipStartDate);

		HasDefiniteForm = definiteFormMapper.IsDefiniteForm(governorship.RegionName);
		RulerUsesTitleName = false;

		PlayerCountry = false;

		var impGovernor = imperatorCharacters[governorship.CharacterId];

		// ------------------ determine holder
		History.AddFieldValue(governorshipStartDate, "holder", "holder", $"imperator{impGovernor.Id}");

		// ------------------ determine government
		var ck3LiegeGov = country.CK3Title.GetGovernment(governorshipStartDate);
		if (ck3LiegeGov is not null) {
			History.AddFieldValue(governorshipStartDate, "government", "government", ck3LiegeGov);
		}

		// ------------------ determine color
		var countryColor = country.Color1;
		if (countryColor is not null) {
			Color1 = parentCollection.GetDerivedColor(countryColor);
		}

		// determine successions laws
		// https://github.com/ParadoxGameConverters/ImperatorToCK3/issues/90#issuecomment-817178552
		History.AddFieldValue(governorshipStartDate,
			"succession_laws",
			"succession_laws",
			new SortedSet<string> { "high_partition_succession_law" }
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

		TrySetNameFromGovernorship(governorship, imperatorRegionMapper, country, irProvinces, regionHasMultipleGovernorships, locDB);
		TrySetAdjectiveFromGovernorship(governorship, country, locDB);
	}

	private void TrySetAdjectiveFromGovernorship(Governorship governorship, Country country, LocDB locDB) {
		var adjKey = $"{Id}_adj";
		if (Localizations.ContainsKey(adjKey)) {
			return;
		}

		var adjSet = false;
		// Try to generate adjective from name.
		var nameLocBlock = Localizations.GetLocBlockForKey(Id) ?? locDB.GetLocBlockForKey(governorship.RegionName);
		if (!adjSet && nameLocBlock is not null) {
			var adjLocBlock = Localizations.AddLocBlock(adjKey);
			adjLocBlock.CopyFrom(nameLocBlock);

			var englishLoc = adjLocBlock["english"];
			if (englishLoc is not null) {
				adjLocBlock["english"] = englishLoc.GetAdjective();
			}

			adjSet = true;
		}
		// Try to use country adjective.
		if (!adjSet) {
			var ck3Country = country.CK3Title;
			if (ck3Country is null) {
				return;
			}
			if (ck3Country.Localizations.TryGetValue($"{ck3Country.Id}_adj", out var countryAdjectiveLocBlock)) {
				var adjLocBlock = Localizations.AddLocBlock(adjKey);
				adjLocBlock.CopyFrom(countryAdjectiveLocBlock);
				adjSet = true;
			}
		}

		if (!adjSet) {
			Logger.Warn($"{Id} needs help with adjective localization!");
		}
	}

	private void TrySetNameFromGovernorship(
		Governorship governorship,
		ImperatorRegionMapper irRegionMapper,
		Country country,
		Imperator.Provinces.ProvinceCollection irProvinces,
		bool regionHasMultipleGovernorships,
		LocDB locDB
	) {
		if (Localizations.ContainsKey(Id)) {
			return;
		}

		var nameSet = false;
		var regionId = governorship.RegionName;
		irRegionMapper.Regions.TryGetValue(regionId, out var region);
		LocBlock? regionLocBlock = locDB.GetLocBlockForKey(regionId);

		// If any area in the region is at least 75% owned, use the area name for governorship name.
		if (regionHasMultipleGovernorships && region is not null) {
			Area? potentialSourceArea = null;
			float biggestOwnershipPercentage = 0f;
			foreach (var area in region.Areas) {
				var provinces = area.Provinces;
				if (provinces.Count == 0) {
					continue;
				}
				var controlledProvinces = irProvinces.Where(p => country.Equals(p.OwnerCountry));
				var ownershipPercentage = (float)provinces.Count / controlledProvinces.Count();
				if (ownershipPercentage < 0.75) {
					continue;
				}
				if (ownershipPercentage > biggestOwnershipPercentage) {
					potentialSourceArea = area;
					biggestOwnershipPercentage = ownershipPercentage;
				}
			}

			if (potentialSourceArea is not null && locDB.TryGetValue(potentialSourceArea.Id, out var areaLocBlock)) {
				var nameLocBlock = Localizations.AddLocBlock(Id);
				nameLocBlock.CopyFrom(areaLocBlock);
				nameSet = true;

				var adjLocBlock = Localizations.AddLocBlock($"{Id}_adj");
				adjLocBlock.CopyFrom(nameLocBlock);
				adjLocBlock.ModifyForEveryLanguage((loc, language) => language == "english" ? loc?.GetAdjective() : loc);
			}
		}
		// Try to use the name of most developed owned territory in the region.
		if (!nameSet && regionHasMultipleGovernorships && region is not null) {
			var sourceProvince = irProvinces
				.Where(p => region.ContainsProvince(p.Id) && country.Equals(p.OwnerCountry))
				.MaxBy(p => p.CivilizationValue);
			if (sourceProvince is not null && locDB.TryGetValue(sourceProvince.Name, out var provinceLocBlock)) {
				var nameLocBlock = Localizations.AddLocBlock(Id);
				nameLocBlock.CopyFrom(provinceLocBlock);
				nameSet = true;

				var adjLocBlock = Localizations.AddLocBlock($"{Id}_adj");
				adjLocBlock.CopyFrom(nameLocBlock);
				adjLocBlock.ModifyForEveryLanguage((loc, language) => language == "english" ? loc?.GetAdjective() : loc);
			}
		}
		// Try to use "<country adjective> <region name>" as governorship name if region has multiple governorships.
		// Example: Mauretania -> Roman Mauretania
		if (!nameSet && regionHasMultipleGovernorships && regionLocBlock is not null) {
			var ck3Country = country.CK3Title;
			if (ck3Country is not null && ck3Country.Localizations.TryGetValue($"{ck3Country.Id}_adj", out var countryAdjectiveLocBlock)) {
				var nameLocBlock = Localizations.AddLocBlock(Id);
				nameLocBlock.CopyFrom(regionLocBlock);
				nameLocBlock.ModifyForEveryLanguage(countryAdjectiveLocBlock,
					(orig, adj, _) => $"{adj} {orig}"
				);
				nameSet = true;
			}
		}
		if (!nameSet && regionLocBlock is not null) {
			var nameLocBlock = Localizations.AddLocBlock(Id);
			nameLocBlock.CopyFrom(regionLocBlock);
			nameSet = true;
		}
		if (!nameSet && Id.Contains("_IMPTOCK3_")) {
			Logger.Warn($"{Id} needs help with localization!");
		}
	}

	public void LoadTitles(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);

		TrySetCapitalBarony();
	}

	public Date GetDateOfLastHolderChange() {
		var field = History.Fields["holder"];
		var dates = new SortedSet<Date>(field.DateToEntriesDict.Keys);
		var lastDate = dates.Max;
		return lastDate ?? new Date(1, 1, 1);
	}
	
	public ISet<string> GetAllHolderIds() {
		if (!History.Fields.TryGetValue("holder", out var holderField)) {
			return new HashSet<string>();
		}

		var ids = new HashSet<string>();
		var holderEntriesByDate = holderField.DateToEntriesDict.Values;
		foreach (var entries in holderEntriesByDate) {
			foreach (var entry in entries) {
				var holderStrValue = entry.Value.ToString();
				if (holderStrValue is not null) {
					ids.Add(holderStrValue);
				}
			}
		}

		var initialHolderEntries = holderField.InitialEntries;
		foreach (var entry in initialHolderEntries) {
			var value = entry.Value;
			var holderStrValue = value.ToString();
			if (holderStrValue is null) {
				Logger.Warn($"Cannot convert holder {value} of {Id} to string!");
			} else {
				ids.Add(holderStrValue);
			}
		}

		return ids;
	}
	public void SetHolder(Character? character, Date date) {
		var id = character is null ? "0" : character.Id;
		History.AddFieldValue(date, "holder", "holder", id);
	}

	public void SetDevelopmentLevel(int value, Date date) {
		if (Rank == TitleRank.barony) {
			Logger.Warn($"Cannot set development level to a barony title {Id}!");
			return;
		}
		History.AddFieldValue(date, "development_level", "change_development_level", value);
	}

	[commonItems.Serialization.NonSerialized] public LocDB Localizations { get; } = new("english", "french", "german", "russian", "simp_chinese", "spanish");

	private void TrySetAdjectiveLoc(LocDB locDB, CountryCollection imperatorCountries) {
		if (ImperatorCountry is null) {
			Logger.Warn($"Cannot set adjective for CK3 title {Id} from null Imperator country!");
			return;
		}

		var adjSet = false;
		var locKey = $"{Id}_adj";

		if (ImperatorCountry.Tag is "PRY" or "SEL" or "MRY") {
			// these tags use customizable loc for adj
			LocBlock? validatedAdj;
			switch (ImperatorCountry.Name) {
				case "PRY_DYN" when ImperatorCountry.Monarch?.Family?.Key == "Antigonid":
					validatedAdj = locDB.GetLocBlockForKey("get_pry_adj_fetch");
					const string pryAdjKey = "PRY_ADJ";
					validatedAdj?.ModifyForEveryLanguage(
						locDB.GetLocBlockForKey(pryAdjKey) ?? new LocBlock(pryAdjKey, "english") {
							["english"] = "Antigonid"
						},
						(loc, modifyingLoc, _) => loc?.Replace($"${pryAdjKey}$", modifyingLoc));
					break;
				case "PRY_DYN":
					validatedAdj = locDB.GetLocBlockForKey("get_pry_adj_fallback");
					break;
				case "SEL_DYN" when ImperatorCountry.Monarch?.Family?.Key == "Seleukid":
					validatedAdj = locDB.GetLocBlockForKey("get_sel_adj_fetch");
					const string selAdjKey = "SEL_ADJ";
					validatedAdj?.ModifyForEveryLanguage(
						locDB.GetLocBlockForKey(selAdjKey) ?? new LocBlock(selAdjKey, "english") {
							["english"] = "Seleukid"
						},
						(loc, modifyingLoc, _) => loc?.Replace($"${selAdjKey}$", modifyingLoc));
					break;
				case "SEL_DYN":
					validatedAdj = locDB.GetLocBlockForKey("get_sel_adj_fallback");
					break;
				case "MRY_DYN" when ImperatorCountry.Monarch?.Family?.Key == "Maurya":
					validatedAdj = locDB.GetLocBlockForKey("get_mry_adj_fetch");
					const string mryAdjKey = "SEL_ADJ";
					validatedAdj?.ModifyForEveryLanguage(
						locDB.GetLocBlockForKey(mryAdjKey) ?? new LocBlock(mryAdjKey, "english") {
							["english"] = "Mauryan"
						},
						(loc, modifyingLoc, _) => loc?.Replace($"${mryAdjKey}$", modifyingLoc));
					break;
				case "MRY_DYN":
					validatedAdj = locDB.GetLocBlockForKey("get_mry_adj_fallback");
					break;
				default:
					validatedAdj = null;
					break;
			}

			if (validatedAdj is not null) {
				var adjLocBlock = Localizations.AddLocBlock(locKey);
				adjLocBlock.CopyFrom(validatedAdj);
				adjSet = true;
			}
		}
		if (!adjSet) {
			var adjOpt = ImperatorCountry.CountryName.GetAdjectiveLocBlock(locDB, imperatorCountries);
			if (adjOpt is not null) {
				var adjLocBlock = Localizations.AddLocBlock(locKey);
				adjLocBlock.CopyFrom(adjOpt);
				adjSet = true;
			}
		}
		if (!adjSet) {
			var adjLocalizationMatch = locDB.GetLocBlockForKey(ImperatorCountry.Tag);
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
		
		// Generate English adjective if missing.
		if (Localizations.TryGetValue(locKey, out var locBlock) && locBlock["english"] is null) {
			if (!Localizations.TryGetValue(Id, out var nameLocBlock) || nameLocBlock["english"] is not string name) {
				return;
			}

			var generatedAdjective = name.GetAdjective();
			locBlock["english"] = generatedAdjective;
			Logger.Debug($"Generated adjective for country \"{name}\": \"{generatedAdjective}\"");
		}
	}
	[commonItems.Serialization.NonSerialized] public string? CoA { get; private set; }

	[SerializedName("capital")] public string? CapitalCountyId { get; private set; }
	[commonItems.Serialization.NonSerialized]
	public Title? CapitalCounty {
		get => CapitalCountyId is null ? null : parentCollection[CapitalCountyId];
		private set => CapitalCountyId = value?.Id;
	}

	[commonItems.Serialization.NonSerialized] public Country? ImperatorCountry { get; private set; }

	[SerializedName("color")] public Color? Color1 { get; set; }

	private Title? deJureLiege;
	[commonItems.Serialization.NonSerialized]
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
				value.DeJureVassals.AddOrReplace(this);
			}
		}
	}
	public Title? GetDeFactoLiege(Date date) { // direct de facto liege title
		var liegeStr = GetLiege(date);
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
			History.AddFieldValue(date, fieldName, fieldName, 0);
		} else {
			History.AddFieldValue(date, fieldName, fieldName, newLiege.Id);
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

	[commonItems.Serialization.NonSerialized] public bool PlayerCountry { get; private set; }
	[commonItems.Serialization.NonSerialized] public string Id { get; } // e.g. d_latium
	[commonItems.Serialization.NonSerialized] public TitleRank Rank { get; private set; } = TitleRank.duchy;
	[SerializedName("landless")] public bool Landless { get; private set; } = false;
	[SerializedName("definite_form")] public bool HasDefiniteForm { get; private set; } = false;

	//This line keeps the Seleucids Seleucid and not "[Dynasty]s"
	[SerializedName("ruler_uses_title_name")] public bool RulerUsesTitleName { get; set; } = false;

	[SerializedName("ai_primary_priority")] public StringOfItem? AIPrimaryPriority { get; private set; }
	[SerializedName("can_create")] public StringOfItem? CanCreate { get; private set; }
	[SerializedName("can_create_on_partition")] public StringOfItem? CanCreateOnPartition { get; private set; }
	[SerializedName("destroy_if_invalid_heir")] public bool? DestroyIfInvalidHeir { get; set; }
	[SerializedName("no_automatic_claims")] public bool? NoAutomaticClaims { get; set; }
	[SerializedName("always_follows_primary_heir")] public bool? AlwaysFollowsPrimaryHeir { get; set; }
	[SerializedName("de_jure_drift_disabled")] public bool? DeJureDriftDisabled { get; set; }
	[SerializedName("can_be_named_after_dynasty")] public bool? CanBeNamedAfterDynasty { get; set; }
	[SerializedName("male_names")] public List<string>? MaleNames { get; private set; }
	// <culture, loc key>
	[SerializedName("cultural_names")] public Dictionary<string, string>? CulturalNames { get; private set; }

	public int? GetOwnOrInheritedDevelopmentLevel(Date date) {
		var ownDev = GetDevelopmentLevel(date);
		if (ownDev is not null) { // if development level is already set, just return it
			return ownDev;
		}
		if (deJureLiege is not null) { // if de jure liege exists, return their level
			return deJureLiege.GetOwnOrInheritedDevelopmentLevel(date);
		}
		return null;
	}

	public ICollection<string> GetSuccessionLaws(Date date) {
		switch (History.GetFieldValue("succession_laws", date)) {
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
	[commonItems.Serialization.NonSerialized] public bool IsCreatedFromImperator { get; private set; } = false;

	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(Regexes.TitleId, (reader, titleNameStr) => {
			// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
			var newTitle = parentCollection.Add(titleNameStr);
			newTitle.LoadTitles(reader);

			if (newTitle.Rank == TitleRank.barony && string.IsNullOrEmpty(CapitalBaronyId)) {
				// title is a barony, and no other barony has been found in this scope yet
				CapitalBaronyId = newTitle.Id;
			}

			newTitle.DeJureLiege = this;
		});
		parser.RegisterKeyword("definite_form", reader => HasDefiniteForm = reader.GetBool());
		parser.RegisterKeyword("ruler_uses_title_name", reader => RulerUsesTitleName = reader.GetBool());
		parser.RegisterKeyword("landless", reader => Landless = reader.GetBool());
		parser.RegisterKeyword("color", reader => Color1 = colorFactory.GetColor(reader));
		parser.RegisterKeyword("capital", reader => CapitalCountyId = reader.GetString());
		parser.RegisterKeyword("ai_primary_priority", reader => AIPrimaryPriority = reader.GetStringOfItem());
		parser.RegisterKeyword("can_create", reader => CanCreate = reader.GetStringOfItem());
		parser.RegisterKeyword("can_create_on_partition", reader => CanCreateOnPartition = reader.GetStringOfItem());
		parser.RegisterKeyword("province", reader => Province = reader.GetULong());
		parser.RegisterKeyword("destroy_if_invalid_heir", reader => DestroyIfInvalidHeir = reader.GetBool());
		parser.RegisterKeyword("no_automatic_claims", reader => NoAutomaticClaims = reader.GetBool());
		parser.RegisterKeyword("always_follows_primary_heir", reader => AlwaysFollowsPrimaryHeir = reader.GetBool());
		parser.RegisterKeyword("de_jure_drift_disabled", reader => DeJureDriftDisabled = reader.GetBool());
		parser.RegisterKeyword("can_be_named_after_dynasty", reader => CanBeNamedAfterDynasty = reader.GetBool());
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
		History.Fields.Remove("holder");
		History.Fields.Remove("government");
		History.Fields.Remove("liege");
	}

	[commonItems.Serialization.NonSerialized] public History History { get; } = new();
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
			throw new FormatException($"Title {Id}: unknown rank!");
		}
	}

	public void OutputHistory(StreamWriter writer) {
		var sb = new StringBuilder();
		var content = PDXSerializer.Serialize(History, "\t");
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

	[commonItems.Serialization.NonSerialized] public static IgnoredKeywordsSet IgnoredTokens { get; } = new();

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
	[commonItems.Serialization.NonSerialized] public IEnumerable<ulong> CountyProvinces => DeJureVassals.Where(v => v.Rank == TitleRank.barony).Select(v => (ulong)v.Province!);
	[commonItems.Serialization.NonSerialized] private string CapitalBaronyId { get; set; } = string.Empty; // used when parsing inside county to save first barony
	[commonItems.Serialization.NonSerialized] public ulong? CapitalBaronyProvince { get; private set; } // county barony's province; 0 is not a valid barony ID

	// used by barony titles only
	[SerializedName("province")] public ulong? Province { get; private set; } // province is area on map. b_barony is its corresponding title.

	public void RemoveHistoryPastDate(Date ck3BookmarkDate) {
		History.RemoveHistoryPastDate(ck3BookmarkDate);
	}
}