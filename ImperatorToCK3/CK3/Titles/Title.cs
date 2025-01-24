using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Linguistics;
using commonItems.Localization;
using commonItems.Serialization;
using commonItems.SourceGenerators;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Localization;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.CK3.Titles;

[SerializationByProperties]
internal sealed partial class Title : IPDXSerializable, IIdentifiable<string> {
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
		Dependency? dependency,
		CountryCollection imperatorCountries,
		LocDB irLocDB,
		CK3LocDB ck3LocDB,
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
		Configuration config,
		IReadOnlyCollection<string> enabledCK3Dlcs
	) {
		IsCreatedFromImperator = true;
		this.parentCollection = parentCollection;
		Id = DetermineId(country, dependency, imperatorCountries, tagTitleMapper, irLocDB);
		SetRank();
		InitializeFromTag(
			country,
			dependency,
			imperatorCountries,
			irLocDB,
			ck3LocDB,
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
			config,
			enabledCK3Dlcs
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
		LocDB irLocDB,
		CK3LocDB ck3LocDB,
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
			irLocDB,
			ck3LocDB,
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
	internal void InitializeFromTag(
		Country country,
		Dependency? dependency,
		CountryCollection imperatorCountries,
		LocDB irLocDB,
		CK3LocDB ck3LocDB,
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
		Configuration config,
		IReadOnlyCollection<string> enabledCK3Dlcs
	) {
		ImperatorCountry = country;
		ImperatorCountry.CK3Title = this;

		LocBlock? validatedName = GetValidatedName(country, imperatorCountries, irLocDB);

		HasDefiniteForm = definiteFormMapper.IsDefiniteForm(ImperatorCountry.Name);
		RulerUsesTitleName = false;

		PlayerCountry = ImperatorCountry.PlayerCountry;

		ClearHolderSpecificHistory();

		FillHolderAndGovernmentHistory(country, characters, governmentMapper, irLocDB, ck3LocDB, religionMapper, cultureMapper, nicknameMapper, provinceMapper, config, conversionDate, enabledCK3Dlcs);

		// Determine color.
		var color1Opt = ImperatorCountry.Color1;
		if (color1Opt is not null) {
			Color1 = color1Opt;
		}

		// determine successions laws
		History.AddFieldValue(conversionDate,
			"succession_laws",
			"succession_laws",
			successionLawMapper.GetCK3LawsForImperatorLaws(ImperatorCountry.GetLaws(), country.Government, enabledCK3Dlcs)
		);

		// Determine CoA.
		if (IsCreatedFromImperator || !config.UseCK3Flags) {
			CoA = coaMapper.GetCoaForFlagName(ImperatorCountry.Flag);
		}

		// Determine other attributes:
		// Set capital to Imperator tag's capital.
		if (ImperatorCountry.CapitalProvinceId is not null) {
			var srcCapital = ImperatorCountry.CapitalProvinceId.Value;
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
			var nameLocBlock = ck3LocDB.GetOrCreateLocBlock(Id);
			nameLocBlock.CopyFrom(validatedName);
			nameSet = true;
		}
		if (!nameSet) {
			var irTagLoc = irLocDB.GetLocBlockForKey(ImperatorCountry.Tag);
			if (irTagLoc is not null) {
				var nameLocBlock = ck3LocDB.GetOrCreateLocBlock(Id);
				nameLocBlock.CopyFrom(irTagLoc);
				nameSet = true;
			}
		}
		if (!nameSet) {
			// use unlocalized name if not empty
			var name = ImperatorCountry.Name;
			if (!string.IsNullOrEmpty(name)) {
				Logger.Warn($"Using unlocalized Imperator name {name} as name for {Id}!");
				var nameLocBlock = ck3LocDB.GetOrCreateLocBlock(Id);
				nameLocBlock[ConverterGlobals.PrimaryLanguage] = name;
				nameSet = true;
			}
		}
		// giving up
		if (!nameSet) {
			Logger.Warn($"{Id} needs help with localization! {ImperatorCountry.Name}?");
		}

		// determine adjective localization
		TrySetAdjectiveLoc(irLocDB, imperatorCountries, ck3LocDB);

		// If country is a subject, convert it to a vassal.
		if (dependency is not null) {
			var overLordTitle = imperatorCountries[dependency.OverlordId].CK3Title;
			if (overLordTitle is null) {
				Logger.Warn($"Can't find CK3 title for country {dependency.OverlordId}, overlord of {country.Id}.");
			}
			if (!config.StaticDeJure) {
				DeJureLiege = overLordTitle;
			}
			SetDeFactoLiege(overLordTitle, dependency.StartDate);
		}
	}

	/// <summary>
	/// Fills title's history with Imperator and pre-Imperator rulers and sets appropriate government.
	/// </summary>
	private void FillHolderAndGovernmentHistory(Country imperatorCountry,
		CharacterCollection characters,
		GovernmentMapper governmentMapper,
		LocDB irLocDB,
		CK3LocDB ck3LocDB,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		NicknameMapper nicknameMapper,
		ProvinceMapper provinceMapper,
		Configuration config,
		Date conversionDate,
		IReadOnlyCollection<string> enabledCK3Dlcs) {
		// ------------------ determine previous and current holders

		foreach (var impRulerTerm in imperatorCountry.RulerTerms) {
			var rulerTerm = new RulerTerm(
				impRulerTerm,
				characters,
				governmentMapper,
				irLocDB,
				ck3LocDB,
				religionMapper,
				cultureMapper,
				nicknameMapper,
				provinceMapper,
				config,
				enabledCK3Dlcs
			);

			var characterId = rulerTerm.CharacterId;
			if (characterId is null) {
				continue;
			}
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

		if (imperatorCountry.Government is not null) {
			var lastCK3TermGov = GetGovernment(conversionDate);
			var ck3CountryGov = governmentMapper.GetCK3GovernmentForImperatorGovernment(imperatorCountry.Government, Rank, imperatorCountry.PrimaryCulture, enabledCK3Dlcs);
			if (lastCK3TermGov != ck3CountryGov && ck3CountryGov is not null) {
				History.AddFieldValue(conversionDate, "government", "government", ck3CountryGov);
			}
			
			// If the government is administrative, add a history effect for setting the state faith.
			string? effectiveGov = ck3CountryGov ?? lastCK3TermGov;
			var holderId = GetHolderId(conversionDate);
			if (effectiveGov == "administrative_government" && characters.TryGetValue(holderId, out var holder)) {
				var holderFaithId = holder.GetFaithId(conversionDate);
				if (holderFaithId is not null) {
					History.AddFieldValue(conversionDate, "effects", "effect",
						$$"""
							{
								if = {
									limit = {
										exists = holder
										holder = { has_government = administrative_government }
									}
									set_state_faith = faith:{{holderFaithId}}
								}
							}
						""");
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

	private static LocBlock? GetValidatedName(Country imperatorCountry, CountryCollection imperatorCountries, LocDB irLocDB) {
		switch (imperatorCountry.Name) {
			// Hard code for Antigonid Kingdom, Seleucid Empire and Maurya.
			// These countries use customizable localization for name and adjective.
			case "PRY_DYN" when imperatorCountry.Monarch?.Family?.Key == "Antigonid":
				const string pryLocKey = "get_pry_name_fetch";
				var pryLocBlock = irLocDB.GetLocBlockForKey(pryLocKey);
				if (pryLocBlock is null) {
					return pryLocBlock;
				}

				var modifiedPrylocBlockToReturn = new LocBlock(pryLocKey, pryLocBlock);
				const string pryNameKey = "PRY";
				modifiedPrylocBlockToReturn.ModifyForEveryLanguage(
					otherBlock: irLocDB.GetLocBlockForKey(pryNameKey) ?? new LocBlock(pryNameKey, ConverterGlobals.PrimaryLanguage) {
						[ConverterGlobals.PrimaryLanguage] = "Antigonid Kingdom"
					},
					(loc, modifyingLoc, language) => {
						var locToReturn = loc?.Replace($"${pryNameKey}$", modifyingLoc);
						if (locToReturn is not null && locToReturn.Contains("[GetCountry(")) {
							locToReturn = irLocDB.GetLocBlockForKey("get_pry_name_fallback")?[language];
						}
						return locToReturn;
					});
				return modifiedPrylocBlockToReturn;
			case "PRY_DYN":
				return irLocDB.GetLocBlockForKey("get_pry_name_fallback");
			case "SEL_DYN" when imperatorCountry.Monarch?.Family?.Key == "Seleukid":
				const string selLocKey = "get_sel_name_fetch";
				var selLocBlock = irLocDB.GetLocBlockForKey(selLocKey);
				if (selLocBlock is null) {
					return selLocBlock;
				}

				var modifiedSelLocBlockToReturn = new LocBlock(selLocKey, selLocBlock);
				const string selNameKey = "SEL";
				modifiedSelLocBlockToReturn.ModifyForEveryLanguage(
					otherBlock: irLocDB.GetLocBlockForKey(selNameKey) ?? new LocBlock(selNameKey, ConverterGlobals.PrimaryLanguage) {
						[ConverterGlobals.PrimaryLanguage] = "Seleukid Empire"
					},
					(loc, modifyingLoc, language) => {
						var locToReturn = loc?.Replace($"${selNameKey}$", modifyingLoc);
						if (locToReturn is not null && locToReturn.Contains("[GetCountry(")) {
							locToReturn = irLocDB.GetLocBlockForKey("get_sel_name_fallback")?[language];
						}
						return locToReturn;
					});
				return modifiedSelLocBlockToReturn;
			case "SEL_DYN":
				return irLocDB.GetLocBlockForKey("get_sel_name_fallback");
			case "MRY_DYN" when imperatorCountry.Monarch?.Family?.Key == "Maurya":
				const string mryLocKey = "get_mry_name_fetch";
				var mryLocBlock = irLocDB.GetLocBlockForKey(mryLocKey);
				if (mryLocBlock is null) {
					return mryLocBlock;
				}
				
				var modifiedMryLocBlockToReturn = new LocBlock(mryLocKey, mryLocBlock);
				const string mryNameKey = "MRY";
				modifiedMryLocBlockToReturn.ModifyForEveryLanguage(
					otherBlock: irLocDB.GetLocBlockForKey(mryNameKey) ?? new LocBlock(mryNameKey, ConverterGlobals.PrimaryLanguage) {
						[ConverterGlobals.PrimaryLanguage] = "Maurya"
					},
					(loc, modifyingLoc, language) => {
						var locToReturn = loc?.Replace($"${mryNameKey}$", modifyingLoc);
						if (locToReturn is not null && locToReturn.Contains("[GetCountry(")) {
							locToReturn = irLocDB.GetLocBlockForKey("get_mry_name_fallback")?[language];
						}
						return locToReturn;
					});
				return modifiedMryLocBlockToReturn;
			case "MRY_DYN":
				return irLocDB.GetLocBlockForKey("get_mry_name_fallback");
			default:
				return imperatorCountry.CountryName.GetNameLocBlock(irLocDB, imperatorCountries);
		}
	}

	public static string DetermineId(
		Country irCountry,
		Dependency? dependency,
		CountryCollection imperatorCountries,
		TagTitleMapper tagTitleMapper,
		LocDB irLocDB
	) {
		var validatedName = GetValidatedName(irCountry, imperatorCountries, irLocDB);
		var validatedEnglishName = validatedName?[ConverterGlobals.PrimaryLanguage];

		string? titleId;
		
		if (dependency is not null) {
			var overlord = imperatorCountries[dependency.OverlordId];
			titleId = tagTitleMapper.GetTitleForSubject(irCountry, validatedEnglishName ?? string.Empty, overlord);
		} else if (validatedEnglishName is not null) {
			titleId = tagTitleMapper.GetTitleForTag(irCountry, validatedEnglishName, maxTitleRank: TitleRank.empire);
		} else {
			titleId = tagTitleMapper.GetTitleForTag(irCountry);
		}

		if (titleId is null) {
			throw new ArgumentException($"Country {irCountry.Tag} could not be mapped to CK3 Title!");
		}
		return titleId;
	}

	public static string? DetermineId(Governorship governorship, LandedTitles titles, Imperator.Provinces.ProvinceCollection irProvinces, ProvinceCollection ck3Provinces, ImperatorRegionMapper imperatorRegionMapper, TagTitleMapper tagTitleMapper, ProvinceMapper provMapper) {
		var country = governorship.Country;
		if (country.CK3Title is null) {
			Logger.Debug($"{country.Tag} governorship of {governorship.Region.Id} could not be mapped to CK3 title: country has no CK3Title.");
			return null;
		}
		return tagTitleMapper.GetTitleForGovernorship(governorship, titles, irProvinces, ck3Provinces, imperatorRegionMapper, provMapper);
	}

	public void InitializeFromGovernorship(
		Governorship governorship,
		Country country,
		Imperator.Provinces.ProvinceCollection irProvinces,
		Imperator.Characters.CharacterCollection imperatorCharacters,
		bool regionHasMultipleGovernorships,
		bool staticDeJure,
		LocDB irLocDB,
		CK3LocDB ck3LocDB,
		ProvinceMapper provinceMapper,
		DefiniteFormMapper definiteFormMapper,
		ImperatorRegionMapper imperatorRegionMapper
	) {
		var governorshipStartDate = governorship.StartDate;

		if (country.CK3Title is null) {
			throw new ArgumentException($"{country.Tag} governorship of {governorship.Region.Id} could not be mapped to CK3 title: liege doesn't exist!");
		}

		ClearHolderSpecificHistory();

		if (!staticDeJure) {
			DeJureLiege = country.CK3Title;
		}
		SetDeFactoLiege(country.CK3Title, governorshipStartDate);

		HasDefiniteForm = definiteFormMapper.IsDefiniteForm(governorship.Region.Id);
		RulerUsesTitleName = false;

		PlayerCountry = false;

		var impGovernor = imperatorCharacters[governorship.CharacterId];

		// ------------------ determine holder
		History.AddFieldValue(governorshipStartDate, "holder", "holder", $"imperator{impGovernor.Id}");

		// ------------------ determine government
		Date normalizedGovernorshipStartDate = governorshipStartDate.Year >= 2 ? governorshipStartDate : new(2, 1, 1);
		var ck3LiegeGov = country.CK3Title.GetGovernment(normalizedGovernorshipStartDate);
		if (ck3LiegeGov is not null) {
			History.AddFieldValue(normalizedGovernorshipStartDate, "government", "government", ck3LiegeGov);
		}

		// Determine color.
		var countryColor = country.Color1;
		if (countryColor is not null) {
			var regionColor = governorship.Region.Color;
			if (regionColor is not null && !parentCollection.IsColorUsed(regionColor)) {
				Color1 = regionColor;
			} else {
				Color1 = parentCollection.GetDerivedColor(countryColor);
			}
		}

		// determine successions laws
		// https://github.com/ParadoxGameConverters/ImperatorToCK3/issues/90#issuecomment-817178552
		OrderedSet<string> successionLaws = [];
		if (ck3LiegeGov is not null && ck3LiegeGov == "administrative_government") {
			successionLaws.Add("appointment_succession_law");
		} else {
			successionLaws.Add("high_partition_succession_law");
		}
		History.AddFieldValue(governorshipStartDate,
			"succession_laws",
			"succession_laws",
			successionLaws
		);

		// ------------------ determine CoA
		CoA = null; // using game-randomized CoA

		// ------------------ determine capital
		var governorProvince = impGovernor.ProvinceId;
		if (governorProvince.HasValue && imperatorRegionMapper.ProvinceIsInRegion(governorProvince.Value, governorship.Region.Id)) {
			foreach (var ck3Prov in provinceMapper.GetCK3ProvinceNumbers(governorProvince.Value)) {
				var foundCounty = parentCollection.GetCountyForProvince(ck3Prov);
				if (foundCounty is not null) {
					CapitalCounty = foundCounty;
					break;
				}
			}
		}

		TrySetNameFromGovernorship(governorship, imperatorRegionMapper, country, irProvinces, regionHasMultipleGovernorships, irLocDB, ck3LocDB);
		TrySetAdjectiveFromGovernorship(governorship, country, irLocDB, ck3LocDB);
	}

	private void TrySetAdjectiveFromGovernorship(Governorship governorship, Country country, LocDB irLocDB, CK3LocDB ck3LocDB) {
		var adjKey = $"{Id}_adj";
		if (ck3LocDB.ContainsKey(adjKey)) {
			return;
		}

		var adjSet = false;
		// Try to generate adjective from name.
		CK3LocBlock? nameLocBlock = ck3LocDB.GetLocBlockForKey(Id);
		if (nameLocBlock is null) {
			var irRegionLoc = irLocDB.GetLocBlockForKey(governorship.Region.Id);
			if (irRegionLoc is not null) {
				nameLocBlock = new CK3LocBlock(irRegionLoc.Id, ConverterGlobals.PrimaryLanguage, irRegionLoc);
			}
		}
		if (!adjSet && nameLocBlock is not null) {
			var adjLocBlock = ck3LocDB.GetOrCreateLocBlock(adjKey);
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
			if (ck3LocDB.TryGetValue($"{ck3Country.Id}_adj", out var countryAdjectiveLocBlock)) {
				var adjLocBlock = ck3LocDB.GetOrCreateLocBlock(adjKey);
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
		LocDB irLocDB,
		CK3LocDB ck3LocDB
	) {
		if (ck3LocDB.ContainsKey(Id)) {
			return;
		}

		var nameSet = false;
		var regionId = governorship.Region.Id;
		irRegionMapper.Regions.TryGetValue(regionId, out var region);
		LocBlock? regionLocBlock = irLocDB.GetLocBlockForKey(regionId);

		// If any area in the region is at least 60% owned, use the area name for governorship name.
		if (regionHasMultipleGovernorships && region is not null) {
			Area? potentialSourceArea = null;
			float biggestOwnershipPercentage = 0f;
			foreach (var area in region.Areas) {
				var areaProvinces = area.Provinces;
				if (areaProvinces.Count == 0) {
					continue;
				}
				var controlledProvinces = areaProvinces.Where(p => country.Equals(p.OwnerCountry));
				var ownershipPercentage = (float)controlledProvinces.Count() / areaProvinces.Count;
				if (ownershipPercentage < 0.6) {
					continue;
				}
				if (ownershipPercentage > biggestOwnershipPercentage) {
					potentialSourceArea = area;
					biggestOwnershipPercentage = ownershipPercentage;
				}
			}

			if (potentialSourceArea is not null && irLocDB.TryGetValue(potentialSourceArea.Id, out var areaLocBlock)) {
				Logger.Debug($"Naming {Id} after I:R area {potentialSourceArea.Id} majorly ({biggestOwnershipPercentage:P}) controlled by {country.Tag}...");
				var nameLocBlock = ck3LocDB.GetOrCreateLocBlock(Id);
				nameLocBlock.CopyFrom(areaLocBlock);
				nameSet = true;

				var adjLocBlock = ck3LocDB.GetOrCreateLocBlock($"{Id}_adj");
				adjLocBlock.CopyFrom(nameLocBlock);
				adjLocBlock.ModifyForEveryLanguage((loc, language) => language == "english" ? loc?.GetAdjective() : loc);
			}
		}
		// Try to use the name of most developed owned territory in the region.
		if (!nameSet && regionHasMultipleGovernorships && region is not null) {
			var sourceProvince = irProvinces
				.Where(p => region.ContainsProvince(p.Id) && country.Equals(p.OwnerCountry))
				.MaxBy(p => p.CivilizationValue);
			if (sourceProvince is not null && irLocDB.TryGetValue(sourceProvince.Name, out var provinceLocBlock)) {
				Logger.Debug($"Naming {Id} after most developed I:R territory: {sourceProvince.Id}...");
				var nameLocBlock = ck3LocDB.GetOrCreateLocBlock(Id);
				nameLocBlock.CopyFrom(provinceLocBlock);
				nameSet = true;

				var adjLocBlock = ck3LocDB.GetOrCreateLocBlock($"{Id}_adj");
				adjLocBlock.CopyFrom(nameLocBlock);
				adjLocBlock.ModifyForEveryLanguage((loc, language) => language == "english" ? loc?.GetAdjective() : loc);
			}
		}
		// Try to use "<country adjective> <region name>" as governorship name if region has multiple governorships.
		// Example: Mauretania -> Roman Mauretania
		if (!nameSet && regionHasMultipleGovernorships && regionLocBlock is not null) {
			var ck3Country = country.CK3Title;
			if (ck3Country is not null && ck3LocDB.TryGetValue($"{ck3Country.Id}_adj", out var countryAdjectiveLocBlock)) {
				Logger.Debug($"Naming {Id} after governorship with country adjective: {country.Tag} {governorship.Region.Id}...");
				var nameLocBlock = ck3LocDB.GetOrCreateLocBlock(Id);
				nameLocBlock.CopyFrom(regionLocBlock);
				nameLocBlock.ModifyForEveryLanguage(countryAdjectiveLocBlock,
					(orig, adj, _) => $"{adj} {orig}"
				);
				nameSet = true;
			}
		}
		if (!nameSet && regionLocBlock is not null) {
			Logger.Debug($"Naming {Id} after governorship: {governorship.Region.Id}...");
			var nameLocBlock = ck3LocDB.GetOrCreateLocBlock(Id);
			nameLocBlock.CopyFrom(regionLocBlock);
			nameSet = true;
		}
		if (!nameSet && Id.Contains("_IRTOCK3_")) {
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

	public HashSet<string> GetAllHolderIds() {
		if (!History.Fields.TryGetValue("holder", out var holderField)) {
			return [];
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
		var holderId = character is null ? "0" : character.Id;
		History.AddFieldValue(date, "holder", "holder", holderId);
	}

	public void SetDevelopmentLevel(int value, Date date) {
		if (Rank == TitleRank.barony) {
			Logger.Warn($"Cannot set development level to a barony title {Id}!");
			return;
		}
		History.AddFieldValue(date, "development_level", "change_development_level", value);
	}

	private void TrySetAdjectiveLoc(LocDB irLocDB, CountryCollection imperatorCountries, CK3LocDB ck3LocDB) {
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
					var pryAdjLocBlock = irLocDB.GetLocBlockForKey("get_pry_adj_fetch");
					if (pryAdjLocBlock is not null) {
						const string pryAdjKey = "PRY_ADJ";
						validatedAdj = new LocBlock(pryAdjLocBlock.Id, pryAdjLocBlock);
						validatedAdj.ModifyForEveryLanguage(
							otherBlock: irLocDB.GetLocBlockForKey(pryAdjKey) ?? new LocBlock(pryAdjKey, ConverterGlobals.PrimaryLanguage) {
								[ConverterGlobals.PrimaryLanguage] = "Antigonid"
							},
							(loc, modifyingLoc, language) => {
								var locToReturn = loc?.Replace($"${pryAdjKey}$", modifyingLoc);
								if (locToReturn is not null && locToReturn.Contains("[GetCountry(")) {
									locToReturn = irLocDB.GetLocBlockForKey("get_pry_adj_fallback")?[language];
								}
								return locToReturn;
							});
					} else {
						validatedAdj = pryAdjLocBlock;
					}

					break;
				case "PRY_DYN":
					validatedAdj = irLocDB.GetLocBlockForKey("get_pry_adj_fallback");
					break;
				case "SEL_DYN" when ImperatorCountry.Monarch?.Family?.Key == "Seleukid":
					var selAdjLocBlock = irLocDB.GetLocBlockForKey("get_sel_adj_fetch");
					if (selAdjLocBlock is not null) {
						const string selAdjKey = "SEL_ADJ";
						validatedAdj = new LocBlock(selAdjLocBlock.Id, selAdjLocBlock);
						validatedAdj.ModifyForEveryLanguage(
							otherBlock: irLocDB.GetLocBlockForKey(selAdjKey) ?? new LocBlock(selAdjKey, ConverterGlobals.PrimaryLanguage) {
								[ConverterGlobals.PrimaryLanguage] = "Seleukid"
							},
							(loc, modifyingLoc, language) => {
								var locToReturn = loc?.Replace($"${selAdjKey}$", modifyingLoc);
								if (locToReturn is not null && locToReturn.Contains("[GetCountry(")) {
									locToReturn = irLocDB.GetLocBlockForKey("get_sel_adj_fallback")?[language];
								}
								return locToReturn;
							});
					} else {
						validatedAdj = selAdjLocBlock;
					}
					
					break;
				case "SEL_DYN":
					validatedAdj = irLocDB.GetLocBlockForKey("get_sel_adj_fallback");
					break;
				case "MRY_DYN" when ImperatorCountry.Monarch?.Family?.Key == "Maurya":
					var mryAdjLocBlock = irLocDB.GetLocBlockForKey("get_mry_adj_fetch");
					if (mryAdjLocBlock is not null) {
						const string mryAdjKey = "MRY_ADJ";
						validatedAdj = new LocBlock(mryAdjLocBlock.Id, mryAdjLocBlock);
						validatedAdj.ModifyForEveryLanguage(
							otherBlock: irLocDB.GetLocBlockForKey(mryAdjKey) ?? new LocBlock(mryAdjKey, ConverterGlobals.PrimaryLanguage) {
								[ConverterGlobals.PrimaryLanguage] = "Mauryan"
							},
							(loc, modifyingLoc, language) => {
								var locToReturn = loc?.Replace($"${mryAdjKey}$", modifyingLoc);
								if (locToReturn is not null && locToReturn.Contains("[GetCountry(")) {
									locToReturn = irLocDB.GetLocBlockForKey("get_mry_adj_fallback")?[language];
								}
								return locToReturn;
							});
					} else {
						validatedAdj = mryAdjLocBlock;
					}
					break;
				case "MRY_DYN":
					validatedAdj = irLocDB.GetLocBlockForKey("get_mry_adj_fallback");
					break;
				default:
					validatedAdj = null;
					break;
			}

			if (validatedAdj is not null) {
				var adjLocBlock = ck3LocDB.GetOrCreateLocBlock(locKey);
				adjLocBlock.CopyFrom(validatedAdj);
				adjSet = true;
			}
		}
		if (!adjSet) {
			var adjOpt = ImperatorCountry.CountryName.GetAdjectiveLocBlock(irLocDB, imperatorCountries);
			if (adjOpt is not null) {
				var adjLocBlock = ck3LocDB.GetOrCreateLocBlock(locKey);
				adjLocBlock.CopyFrom(adjOpt);
				adjSet = true;
			}
		}
		if (!adjSet) {
			// Try to use the country name as adjective.
			var adjLocalizationMatch = irLocDB.GetLocBlockForKey(ImperatorCountry.Tag);
			if (adjLocalizationMatch is not null) {
				var adjLocBlock = ck3LocDB.GetOrCreateLocBlock(locKey);
				adjLocBlock.CopyFrom(adjLocalizationMatch);
				adjSet = true;
			}
		}
		
		// Try to generate English adjective from country name.
		if (!adjSet) {
			if (ck3LocDB.TryGetValue(Id, out var nameLocBlock) && nameLocBlock["english"] is {} name) {
				// If name has 3 characters and last 2 characters are digits, it's probably a raw Imperator tag.
				// In that case, we don't want to use it as a base for adjective.
				if (!(name.Length == 3 && char.IsDigit(name[1]) && char.IsDigit(name[2]))) {
					var generatedAdjective = name.GetAdjective();
					Logger.Debug($"Generated adjective for country \"{name}\": \"{generatedAdjective}\"");
				
					var adjLocBlock = ck3LocDB.GetOrCreateLocBlock(locKey);
					adjLocBlock["english"] = generatedAdjective;
					adjSet = true;
				}
			}
		}
		
		if (!adjSet) {
			// Use unlocalized name if not empty
			var name = ImperatorCountry.Name;
			if (!string.IsNullOrEmpty(name)) {
				Logger.Warn($"Using unlocalized Imperator name {name} as adjective for {Id}!");
				var adjLocBlock = ck3LocDB.GetOrCreateLocBlock(locKey);
				adjLocBlock[ConverterGlobals.PrimaryLanguage] = name;
				adjSet = true;
			}
		}
		
		// Give up.
		if (!adjSet) {
			Logger.Warn($"{Id} needs help with localization for adjective! {ImperatorCountry.Name}_adj?");
		}
	}
	[commonItems.Serialization.NonSerialized] public string? CoA { get; private set; }

	[SerializedName("capital")] public string? CapitalCountyId { get; private set; }
	[commonItems.Serialization.NonSerialized]
	public Title? CapitalCounty {
		get {
			if (CapitalCountyId is null) {
				return null;
			}
			if (parentCollection.TryGetValue(CapitalCountyId, out var capitalCounty)) {
				return capitalCounty;
			}
			Logger.Warn($"Capital county {CapitalCountyId} of {Id} not found!");
			return null;
		}

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
			deJureLiege?.deJureVassals.Remove(Id);
			deJureLiege = value;
			value?.deJureVassals.AddOrReplace(this);
		}
	}
	public Title? GetDeFactoLiege(Date date) { // direct de facto liege title
		var liegeId = GetLiegeId(date);
		if (liegeId is not null && parentCollection.TryGetValue(liegeId, out var liegeTitle)) {
			if (liegeTitle.Id == Id) {
				Logger.Debug($"A title cannot be its own liege! Title: {Id}");
				return null;
			}
			
			if (liegeTitle.Rank <= Rank) {
				Logger.Debug($"Liege title's rank is not higher than vassal's! " +
				             $"Title: {Id}, liege: {liegeTitle.Id}");
				return null;
			}
			
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

	private readonly TitleCollection deJureVassals = [];
	[SerializeOnlyValue] public IReadOnlyTitleCollection DeJureVassals => deJureVassals; // DIRECT de jure vassals
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
	[SerializedName("require_landless")] public bool? RequireLandless { get; private set; }
	[SerializedName("definite_form")] public bool HasDefiniteForm { get; private set; } = false;

	//This line keeps the Seleucids Seleucid and not "[Dynasty]s"
	[SerializedName("ruler_uses_title_name")] public bool RulerUsesTitleName { get; set; } = false;

	[SerializedName("ai_primary_priority")] public StringOfItem? AIPrimaryPriority { get; private set; }
	[SerializedName("ignore_titularity_for_title_weighting")] public bool? IgnoreTitularityForTitleWeighting { get; private set; }
	[SerializedName("can_create")] public StringOfItem? CanCreate { get; private set; }
	[SerializedName("can_create_on_partition")] public StringOfItem? CanCreateOnPartition { get; private set; }
	[SerializedName("can_destroy")] public StringOfItem? CanDestroy { get; private set; }
	[SerializedName("destroy_if_invalid_heir")] public bool? DestroyIfInvalidHeir { get; set; }
	[SerializedName("destroy_on_succession")] public bool? DestroyOnSuccession { get; set; }
	[SerializedName("no_automatic_claims")] public bool? NoAutomaticClaims { get; set; }
	[SerializedName("noble_family")] public bool? NobleFamily { get; set; }
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
			// Pull the titles beneath this one and add them to the lot.
			// A title can be defined in multiple files, in that case merge the definitions.
			if (parentCollection.TryGetValue(titleNameStr, out var childTitle)) {
				childTitle.LoadTitles(reader);
			} else {
				childTitle = parentCollection.Add(titleNameStr);
				childTitle.LoadTitles(reader);
			}

			if (childTitle.Rank == TitleRank.barony && string.IsNullOrEmpty(CapitalBaronyId)) {
				// title is a barony, and no other barony has been found in this scope yet
				CapitalBaronyId = childTitle.Id;
			}
			
			childTitle.DeJureLiege = this;
		});
		parser.RegisterKeyword("definite_form", reader => HasDefiniteForm = reader.GetBool());
		parser.RegisterKeyword("ruler_uses_title_name", reader => RulerUsesTitleName = reader.GetBool());
		parser.RegisterKeyword("landless", reader => Landless = reader.GetBool());
		parser.RegisterKeyword("require_landless", reader => RequireLandless = reader.GetBool());
		parser.RegisterKeyword("color", reader => {
			try {
				Color1 = colorFactory.GetColor(reader);
			} catch (ArgumentException e) {
				Logger.Warn($"{e.Message} - defaulting to black");
				Color1 = new Color(0, 0, 0);
			}
		});
		parser.RegisterKeyword("capital", reader => CapitalCountyId = reader.GetString());
		parser.RegisterKeyword("ai_primary_priority", reader => {
			var stringOfItem = reader.GetStringOfItem();
			
			// Drop ai_primary_priority blocks that contain references to specific dynasties or characters.
			var str = stringOfItem.ToString();
			if (str.Contains("dynasty:") || str.Contains("character:")) {
				return;
			}
			
			AIPrimaryPriority = stringOfItem;
		});
		parser.RegisterKeyword("ignore_titularity_for_title_weighting", reader => IgnoreTitularityForTitleWeighting = reader.GetBool());
		parser.RegisterKeyword("can_create", reader => CanCreate = reader.GetStringOfItem());
		parser.RegisterKeyword("can_create_on_partition", reader => CanCreateOnPartition = reader.GetStringOfItem());
		parser.RegisterKeyword("can_destroy", reader => CanDestroy = reader.GetStringOfItem());
		parser.RegisterKeyword("province", reader => ProvinceId = reader.GetULong());
		parser.RegisterKeyword("destroy_if_invalid_heir", reader => DestroyIfInvalidHeir = reader.GetBool());
		parser.RegisterKeyword("destroy_on_succession", reader => DestroyOnSuccession = reader.GetBool());
		parser.RegisterKeyword("no_automatic_claims", reader => NoAutomaticClaims = reader.GetBool());
		parser.RegisterKeyword("noble_family", reader => NobleFamily = reader.GetBool());
		parser.RegisterKeyword("always_follows_primary_heir", reader => AlwaysFollowsPrimaryHeir = reader.GetBool());
		parser.RegisterKeyword("de_jure_drift_disabled", reader => DeJureDriftDisabled = reader.GetBool());
		parser.RegisterKeyword("can_be_named_after_dynasty", reader => CanBeNamedAfterDynasty = reader.GetBool());
		parser.RegisterKeyword("male_names", reader => MaleNames = reader.GetStrings());
		parser.RegisterKeyword("cultural_names", reader => CulturalNames = reader.GetAssignments()
			.GroupBy(a => a.Key)
			.ToDictionary(g => g.Key, g => g.Last().Value));

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
			if (deJureVassal.ProvinceId is null) {
				continue;
			}
			ulong baronyProvinceId = (ulong)deJureVassal.ProvinceId;

			if (deJureVassal.Id == CapitalBaronyId) {
				CapitalBaronyProvinceId = baronyProvinceId;
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
		Rank = GetRankForId(Id);
	}

	public async Task OutputHistory(StreamWriter writer) {
		var sb = new StringBuilder();
		var content = PDXSerializer.Serialize(History, "\t");
		if (string.IsNullOrWhiteSpace(content)) {
			// doesn't need to be output
			return;
		}

		sb.Append(Id).AppendLine("={").Append(content).AppendLine("}");
		await writer.WriteAsync(sb);
	}

	public HashSet<ulong> GetProvincesInCountry(Date date) {
		var holderId = GetHolderId(date);
		var heldCounties = new List<Title>(
			parentCollection.Where(t => t.GetHolderId(date) == holderId && t.Rank == TitleRank.county)
		);
		var heldProvinces = new HashSet<ulong>();
		// add directly held counties
		foreach (var county in heldCounties) {
			heldProvinces.UnionWith(county.CountyProvinceIds);
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
				heldProvinces.UnionWith(vassalCounty.CountyProvinceIds);
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

		return DeJureVassals.Any(vassal => vassal.Rank == TitleRank.county && vassal.CountyProvinceIds.Contains(provinceId));
	}

	public Title GetTopRealm(Date date) {
		var titleToReturn = this;
		var dfLiege = GetDeFactoLiege(date);
		while (dfLiege is not null) {
			titleToReturn = dfLiege;
			dfLiege = dfLiege.GetDeFactoLiege(date);
		}
		
		return titleToReturn;
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
			.OrderByDescending(t => t.Rank)
			.ToImmutableList();
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

	public static TitleRank GetRankForId(string titleId) {
		var firstChar = titleId[0];
		return firstChar switch {
			'b' => TitleRank.barony,
			'c' => TitleRank.county,
			'd' => TitleRank.duchy,
			'k' => TitleRank.kingdom,
			'e' => TitleRank.empire,
			_ => throw new FormatException($"Title {titleId}: unknown rank!")
		};
	}

	private void AppointCourtierPositionsFromImperator(Dictionary<string, string[]> courtPositionToSourcesDict,
		List<OfficeJob> convertibleJobs,
		HashSet<string> alreadyEmployedCharacters, 
		Character ck3Ruler,
		Date irSaveDate) {
		Dictionary<string, int> heldTitlesPerCharacterCache = [];
		
		foreach (var (ck3Position, sources) in courtPositionToSourcesDict) {
			// The order of I:R source position types is important - the first filled one found will be used.
			foreach (var sourceOfficeType in sources) {
				var job = convertibleJobs.Find(o => o.OfficeType == sourceOfficeType);
				if (job is null) {
					continue;
				}

				var ck3Official = job.Character.CK3Character;
				if (ck3Official is null) {
					continue;
				}
				if (alreadyEmployedCharacters.Contains(ck3Official.Id)) {
					continue;
				}

				// A ruler cannot be their own courtier.
				if (ck3Official.Id == ck3Ruler.Id) {
					continue;
				}

				if (!heldTitlesPerCharacterCache.ContainsKey(ck3Official.Id)) {
					heldTitlesPerCharacterCache[ck3Official.Id] = parentCollection.Count(t => t.GetHolderId(irSaveDate) == ck3Official.Id);
				}
				// A potential courtier must not be a ruler.
				if (heldTitlesPerCharacterCache[ck3Official.Id] > 0) {
					continue;
				}

				// For cave_hermit_court_position, lifestyle_mystic trait is required.
				if (ck3Position == "cave_hermit_court_position" && !ck3Official.BaseTraits.Contains("lifestyle_mystic")) {
					continue;
				}

				var courtPositionEffect = new StringOfItem($$"""
					{
						character:{{ck3Official.Id}} = {
							if = {
								limit = { prev = { NOT = { is_employer_of = character:{{ck3Official.Id}} } } }
								set_employer = prev
							}
						}
						appoint_court_position = {
						    recipient = character:{{ck3Official.Id}}
						    court_position = {{ck3Position}}
						}
					}
				""");
				ck3Ruler.History.AddFieldValue(irSaveDate, "effects", "effect", courtPositionEffect);

				// One character should only hold one CK3 position.
				convertibleJobs.Remove(job);
				alreadyEmployedCharacters.Add(ck3Official.Id);

				break;
			}
		}
	}

	private void AppointCouncilMembersFromImperator(ReligionCollection religionCollection,
		Dictionary<string, string[]> councilPositionToSourcesDict,
		List<OfficeJob> convertibleJobs, 
		HashSet<string> alreadyEmployedCharacters,
		Character ck3Ruler,
		Date irSaveDate) {
		Dictionary<string, int> heldTitlesPerCharacterCache = [];

		foreach (var (ck3Position, sources) in councilPositionToSourcesDict) {
			// The order of I:R source position types is important - the first filled one found will be used.
			foreach (var sourceOfficeType in sources) {
				var job = convertibleJobs.Find(o => o.OfficeType == sourceOfficeType);
				if (job is null) {
					continue;
				}

				var ck3Official = job.Character.CK3Character;
				if (ck3Official is null) {
					continue;
				}
				if (alreadyEmployedCharacters.Contains(ck3Official.Id)) {
					continue;
				}

				// A ruler cannot be their own councillor.
				if (ck3Official.Id == ck3Ruler.Id) {
					continue;
				}

				if (!heldTitlesPerCharacterCache.TryGetValue(ck3Official.Id, out int heldTitlesCount)) {
					heldTitlesCount = parentCollection.Count(t => t.GetHolderId(irSaveDate) == ck3Official.Id);
					heldTitlesPerCharacterCache[ck3Official.Id] = heldTitlesCount;
				}

				if (ck3Position == "councillor_court_chaplain") {
					// Court chaplains need to have the same faith as the ruler.
					var rulerFaithId = ck3Ruler.GetFaithId(irSaveDate);
					if (rulerFaithId is null || rulerFaithId != ck3Official.GetFaithId(irSaveDate)) {
						continue;
					}

					// If the faith has Disallowed Clerical Marriage, don't allow married court chaplains.
					var rulerFaith = religionCollection.GetFaith(rulerFaithId);
					if (rulerFaith is null) {
						continue;
					}
					if (rulerFaith.HasDoctrine("doctrine_clerical_marriage_disallowed")) {
						if (ck3Official.GetSpouseIds(irSaveDate).Count > 0) {
							continue;
						}
					}

					// If the court faith has doctrine_theocracy_temporal (Theocratic Clerical Tradition), the court chaplain should
					// be either theocratic or landless.
					// For the purpose of the conversion, we simply require them to be landless.
					if (rulerFaith.HasDoctrine("doctrine_theocracy_temporal")) {
						if (heldTitlesCount > 0) {
							continue;
						}
					}
					
					// Skip if the faith doesn't allow the character's gender to be clergy.
					var clerigalGenderDoctrines = rulerFaith.GetDoctrineIdsForDoctrineCategoryId("doctrine_clerical_gender");
					if (clerigalGenderDoctrines.Any()) {
						if (clerigalGenderDoctrines.Contains("doctrine_clerical_gender_female_only") && !ck3Official.Female) {
							continue;
						}
						if (clerigalGenderDoctrines.Contains("doctrine_clerical_gender_male_only") && ck3Official.Female) {
							continue;
						}
					}
				} else if (ck3Position == "councillor_steward" || ck3Position == "councillor_chancellor" || ck3Position == "councillor_marshal") {
					// Unless they are rulers, stewards, chancellors and marshals need to have the dominant gender of the faith.
					if (heldTitlesCount == 0) {
						var courtFaith = ck3Ruler.GetFaithId(irSaveDate);
						if (courtFaith is not null) {
							var dominantGenderDoctrines = religionCollection.GetFaith(courtFaith)?
								.GetDoctrineIdsForDoctrineCategoryId("doctrine_gender");
							if (dominantGenderDoctrines is null) {
								continue;
							}
							if (dominantGenderDoctrines.Contains("doctrine_gender_male_dominated") && ck3Official.Female) {
								continue;
							}
							if (dominantGenderDoctrines.Contains("doctrine_gender_female_dominated") && !ck3Official.Female) {
								continue;
							}
						}
					}
				}

				// We only need to set the employer when the council member is landless.
				if (heldTitlesCount == 0) {
					ck3Official.History.AddFieldValue(irSaveDate, "employer", "employer", ck3Ruler.Id);
				}
				ck3Official.History.AddFieldValue(irSaveDate, "council_position", "give_council_position", ck3Position);

				// One character should only hold one CK3 position.
				convertibleJobs.Remove(job);
				alreadyEmployedCharacters.Add(ck3Official.Id);

				break;
			}
		}
	}

	// used by county titles only
	[commonItems.Serialization.NonSerialized] public IEnumerable<ulong> CountyProvinceIds => DeJureVassals
		.Where(v => v.Rank == TitleRank.barony && v.ProvinceId.HasValue)
		.Select(v => v.ProvinceId!.Value);
	[commonItems.Serialization.NonSerialized] private string CapitalBaronyId { get; set; } = string.Empty; // used when parsing inside county to save first barony
	[commonItems.Serialization.NonSerialized] public ulong? CapitalBaronyProvinceId { get; private set; } // county barony's province; 0 is not a valid barony ID

	// used by barony titles only
	[SerializedName("province")] public ulong? ProvinceId { get; private set; } // province is area on map. b_barony is its corresponding title.

	public void RemoveHistoryPastDate(Date ck3BookmarkDate) {
		History.RemoveHistoryPastDate(ck3BookmarkDate);
	}
}