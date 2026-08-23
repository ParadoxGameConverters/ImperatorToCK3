using commonItems;
using commonItems.Localization;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Families;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Countries;

public class CountryNameTests {
	[Fact]
	public void NameDefaultsToEmpty() {
		var reader = new BufferedReader(string.Empty);
		var countryName = CountryName.Parse(reader);

		Assert.Empty(countryName.Name);
	}

	[Fact]
	public void NameCanBeSet() {
		var reader = new BufferedReader(
			"name = someName adjective = someAdjective"
		);
		var countryName = CountryName.Parse(reader);

		Assert.Equal("someName", countryName.Name);
	}

	[Fact]
	public void AdjectiveLocKeyDefaultsTo_ADJ() {
		var reader = new BufferedReader(string.Empty);
		var countryName = CountryName.Parse(reader);

		Assert.Equal("_ADJ", countryName.GetAdjectiveLocKey());
	}

	[Fact]
	public void AdjectiveLocKeyCanBeSet() {
		var reader = new BufferedReader(
			"name = someName adjective = someAdjective"
		);
		var countryName = CountryName.Parse(reader);

		Assert.Equal("someAdjective", countryName.GetAdjectiveLocKey());
	}

	[Fact]
	public void BaseDefaultsToNullptr() {
		var reader = new BufferedReader(string.Empty);
		var countryName = CountryName.Parse(reader);

		Assert.Null(countryName.BaseName);
	}

	[Fact]
	public void BaseCanBeSet() {
		var reader = new BufferedReader(
			"name = revolt\n base = { name = someName adjective = someAdjective }"
		);
		var countryName = CountryName.Parse(reader);

		Assert.Equal("someName", countryName.BaseName!.Name);
		Assert.Equal("someAdjective", countryName.BaseName.GetAdjectiveLocKey());
		Assert.Null(countryName.BaseName.BaseName);
	}

	[Fact]
	public void AdjLocBlockDefaultsToNull() {
		var reader = new BufferedReader(string.Empty);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		Assert.Null(countryName.GetAdjectiveLocBlock(locDB, new()));
	}

	[Fact]
	public void AdjLocBlockReturnsCorrectLocForRevolts() {
		var reader = new BufferedReader(
			"adjective = CIVILWAR_FACTION_ADJECTIVE \n base = { name = someName adjective = someAdjective }"
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var locBlock1 = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		locBlock1["english"] = "$ADJ$";
		var locBlock2 = locDB.AddLocBlock("someAdjective");
		locBlock2["english"] = "Roman";
		Assert.Equal("Roman", countryName.GetAdjectiveLocBlock(locDB, new())!["english"]);
	}

	[Fact]
	public void GetNameLocBlockDefaultsToNull() {
		var reader = new BufferedReader(string.Empty);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		Assert.Null(countryName.GetNameLocBlock(locDB, imperatorCountries: []));
	}

	[Fact]
	public void GetNameLocBlockCorrectlyHandlesCompositeNames() {
		var reader = new BufferedReader("name=\"egyptian PROV4791_persia\"");
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		
		var egyptianLocBlock = locDB.AddLocBlock("egyptian");
		egyptianLocBlock["english"] = "Memphite";
		egyptianLocBlock["german"] = "Memphit";
		
		var provLocBlock = locDB.AddLocBlock("PROV4791_persia");
		provLocBlock["english"] = "Hormirzad";
		provLocBlock["german"] = "Hormirzad";
		
		var nameLocBlock = countryName.GetNameLocBlock(locDB, []);
		Assert.NotNull(nameLocBlock);
		Assert.Equal("Memphite Hormirzad", nameLocBlock["english"]);
		Assert.Equal("Memphit Hormirzad", nameLocBlock["german"]);
	}

	[Fact]
	public void GetNameLocBlockReturnsCorrectLocForRevolts() {
		var reader = new BufferedReader(
			"name = CIVILWAR_FACTION_NAME\n base = { name = someName adjective = someAdjective }"
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var locBlock1 = locDB.AddLocBlock("CIVILWAR_FACTION_NAME");
		locBlock1["english"] = "$ADJ$ Revolt";
		var locBlock2 = locDB.AddLocBlock("someAdjective");
		locBlock2["english"] = "Roman";
		Assert.Equal("Roman Revolt", countryName.GetNameLocBlock(locDB, [])!["english"]);
	}

	[Fact]
	public void DataTypesInCountryNamesAreReplaced() {
		var reader = new BufferedReader(
			"""
				name="CIVILWAR_FACTION_NAME"
				adjective="CIVILWAR_FACTION_ADJECTIVE"
				base={
					name="PRY_DYN"
					adjective="PRY_DYN_ADJ"
				}
			"""
		);
		
		var countryName = CountryName.Parse(reader);
		
		var locDB = new LocDB("english");
		var civilWarLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_NAME");
		civilWarLocBlock["english"] = "$ADJ$ Revolt";
		var pryAdjLocBlock = locDB.AddLocBlock("PRY_DYN_ADJ");
		pryAdjLocBlock["english"] = "[GetCountry('PRY').Custom('get_pry_adj')]";
		var antigonidPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fetch"); // used when the PRY monarch family is Antigonid
		antigonidPryAdjLocBlock["english"] = "Antigonid";
		var fallbackPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fallback"); // used when the PRY monarch family is not Antigonid
		fallbackPryAdjLocBlock["english"] = "Phrygian";
		
		Assert.Equal("Phrygian Revolt", countryName.GetNameLocBlock(locDB, [])!["english"]);
	}

	[Fact]
	public void DataTypesInCountryAdjectivesAreReplaced() {
		var reader = new BufferedReader(
			"""
				name="CIVILWAR_FACTION_NAME"
				adjective="CIVILWAR_FACTION_ADJECTIVE"
				base={
					name="PRY_DYN"
					adjective="PRY_DYN_ADJ"
				}
			"""
		);
		var countryName = CountryName.Parse(reader);
		
		var locDB = new LocDB("english");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		var pryAdjLocBlock = locDB.AddLocBlock("PRY_DYN_ADJ");
		pryAdjLocBlock["english"] = "[GetCountry('PRY').Custom('get_pry_adj')]";
		var antigonidPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fetch");
		antigonidPryAdjLocBlock["english"] = "Antigonid";
		var fallbackPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fallback");
		fallbackPryAdjLocBlock["english"] = "Phrygian";
		
		Assert.Equal("Phrygian", countryName.GetAdjectiveLocBlock(locDB, [])!["english"]);
		
		// Check if get_pry_adj_fetch is used instead of get_pry_adj_fallback when the monarch family is Antigonid.
		var families = new FamilyCollection();
		families.LoadFamilies(new BufferedReader("1 = { key=\"Antigonid\" }"));
		
		var characters = new CharacterCollection();
		characters.LoadCharacters(new BufferedReader("1 = { family=1 country=1 }"));
		characters.LinkFamilies(families);
		
		var countries = new CountryCollection();
		var phrygia = Country.Parse(new BufferedReader("{ tag=PRY monarch=1 }"), 1);
		countries.Add(phrygia);
		characters.LinkCountries(countries);
		
		Assert.Equal("Antigonid", countryName.GetAdjectiveLocBlock(locDB, countries)!["english"]);
	}

	[Fact]
	public void ProvinceNameCanBeUsedForRevoltTagNameAndAdjective() {
		var reader = new BufferedReader(
			"""
			name="CIVILWAR_FACTION_NAME"
			adjective="CIVILWAR_FACTION_ADJECTIVE"
			base={
				name="PROV4526_hellenic"
			}
			""");
		var countryName = CountryName.Parse(reader);
		
		var locDB = new LocDB("english");
		var civilWarLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_NAME");
		civilWarLocBlock["english"] = "$ADJ$ Revolt";
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		var provinceLocBlock = locDB.AddLocBlock("PROV4526_hellenic");
		provinceLocBlock["english"] = "Nikonia";
		
		Assert.Equal("Nikonia Revolt", countryName.GetNameLocBlock(locDB, [])!["english"]);
		Assert.Equal("Nikonia", countryName.GetAdjectiveLocBlock(locDB, [])!["english"]);
	}

	[Fact]
	public void RawBaseNameCanBeUsedForRevoltTagNameAndAdjective() {
		var reader = new BufferedReader(
			"""
			name="CIVILWAR_FACTION_NAME"
			adjective="CIVILWAR_FACTION_ADJECTIVE"
			base={
				name="Tamilakam"
			}
			""");
		var countryName = CountryName.Parse(reader);
		
		var locDB = new LocDB("english", "french");
		var civilWarLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_NAME");
		civilWarLocBlock["english"] = "$ADJ$ Revolt";
		civilWarLocBlock["french"] = "Rébellion $ADJ$";
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		civilWarAdjLocBlock["french"] = "$ADJ$";
		
		Assert.Equal("Tamilakam Revolt", countryName.GetNameLocBlock(locDB, [])!["english"]);
		Assert.Equal("Tamilakam", countryName.GetAdjectiveLocBlock(locDB, [])!["english"]);

		Assert.Equal("Rébellion Tamilakam", countryName.GetNameLocBlock(locDB, [])!["french"]);
		Assert.Equal("Tamilakam", countryName.GetAdjectiveLocBlock(locDB, [])!["french"]);
	}

	[Fact]
	public void GetNameLocBlockReturnsDirectLocBlockForRegularName() {
		var reader = new BufferedReader("name = someName");
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var nameLocBlock = locDB.AddLocBlock("someName");
		nameLocBlock["english"] = "Some Country";

		Assert.Equal("Some Country", countryName.GetNameLocBlock(locDB, [])!["english"]);
	}

	[Fact]
	public void RevoltNameWithoutBaseIsReturnedUnmodified() {
		var reader = new BufferedReader("name = CIVILWAR_FACTION_NAME");
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var civilWarLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_NAME");
		civilWarLocBlock["english"] = "$ADJ$ Revolt";

		Assert.Equal("$ADJ$ Revolt", countryName.GetNameLocBlock(locDB, [])!["english"]);
	}

	[Fact]
	public void CompositeNamePartsMissingFromLocDBAreSkipped() {
		var reader = new BufferedReader("name=\"egyptian PROV4791_unknown\"");
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");

		var egyptianLocBlock = locDB.AddLocBlock("egyptian");
		egyptianLocBlock["english"] = "Memphite";
		egyptianLocBlock["german"] = "Memphit";

		var nameLocBlock = countryName.GetNameLocBlock(locDB, []);
		Assert.NotNull(nameLocBlock);
		Assert.Equal("Memphite", nameLocBlock["english"]);
		Assert.Equal("Memphit", nameLocBlock["german"]);
	}

	[Fact]
	public void RevoltNameLocDefaultsToNullForLanguageMissingFromSourceLocBlock() {
		var reader = new BufferedReader(
			"name = CIVILWAR_FACTION_NAME\n base = { name = someName adjective = someAdjective }"
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english", "french");
		var civilWarLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_NAME");
		civilWarLocBlock["french"] = "Révolte $ADJ$";
		var adjLocBlock = locDB.AddLocBlock("someAdjective");
		adjLocBlock["french"] = "Romain";

		var nameLocBlock = countryName.GetNameLocBlock(locDB, []);
		Assert.Equal("Révolte Romain", nameLocBlock!["french"]);
		Assert.Null(nameLocBlock["english"]);
	}

	[Fact]
	public void AdjectiveLocBlockReturnsDirectMatchForNonRevoltTags() {
		var reader = new BufferedReader("adjective = someAdjective");
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var adjLocBlock = locDB.AddLocBlock("someAdjective");
		adjLocBlock["english"] = "Roman";

		Assert.Equal("Roman", countryName.GetAdjectiveLocBlock(locDB, [])!["english"]);
	}

	[Fact]
	public void RevoltAdjectiveWithoutBaseGivesUp() {
		var reader = new BufferedReader("adjective = CIVILWAR_FACTION_ADJECTIVE");
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";

		Assert.Null(countryName.GetAdjectiveLocBlock(locDB, []));
	}

	[Fact]
	public void RevoltAdjectiveLocDefaultsToNullForLanguageMissingFromSourceLocBlock() {
		var reader = new BufferedReader(
			"adjective = CIVILWAR_FACTION_ADJECTIVE \n base = { name = someName adjective = someAdjective }"
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english", "french");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["french"] = "$ADJ$";
		var adjLocBlock = locDB.AddLocBlock("someAdjective");
		adjLocBlock["french"] = "Romain";

		var returnedAdjLocBlock = countryName.GetAdjectiveLocBlock(locDB, []);
		Assert.Equal("Romain", returnedAdjLocBlock!["french"]);
		Assert.Null(returnedAdjLocBlock["english"]);
	}

	[Fact]
	public void AdjectiveCanBeInheritedFromAnotherCountryWithSameName() {
		var countries = new CountryCollection();
		countries.Add(Country.Parse(new BufferedReader("country_name = { name = Rome }"), 1)); // different name, skipped
		countries.Add(Country.Parse(new BufferedReader("country_name = { name = Kush }"), 2)); // no adj loc, skipped
		countries.Add(Country.Parse(new BufferedReader("country_name = { name = Kush adjective = custom_adj }"), 3));

		var reader = new BufferedReader("name = Kush");
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var adjLocBlock = locDB.AddLocBlock("custom_adj");
		adjLocBlock["english"] = "Kushan";

		Assert.Equal("Kushan", countryName.GetAdjectiveLocBlock(locDB, countries)!["english"]);
	}

	[Fact]
	public void DataTypeAdjectivesUseFetchLocWhenMonarchFamilyMatches() {
		var reader = new BufferedReader(
			"""
			name="CIVILWAR_FACTION_NAME"
			adjective="CIVILWAR_FACTION_ADJECTIVE"
			base={
				name="PRY_DYN"
				adjective="PRY_DYN_ADJ"
			}
			"""
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		var dynAdjLocBlock = locDB.AddLocBlock("PRY_DYN_ADJ");
		dynAdjLocBlock["english"] =
			"[GetCountry('PRY').Custom('get_pry_adj')] [GetCountry('MRY').Custom('get_mry_adj')] [GetCountry('SEL').Custom('get_sel_adj')]";
		var antigonidPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fetch");
		antigonidPryAdjLocBlock["english"] = "Antigonid";
		var mauryaMryAdjLocBlock = locDB.AddLocBlock("get_mry_adj_fetch");
		mauryaMryAdjLocBlock["english"] = "Mauryan";
		var seleukidSelAdjLocBlock = locDB.AddLocBlock("get_sel_adj_fetch");
		seleukidSelAdjLocBlock["english"] = "Seleucid";

		var families = new FamilyCollection();
		families.LoadFamilies(new BufferedReader("""
			1 = { key="Antigonid" }
			2 = { key="Maurya" }
			3 = { key="Seleukid" }
			"""));
		var characters = new CharacterCollection();
		characters.LoadCharacters(new BufferedReader("""
			1 = { family=1 country=1 }
			2 = { family=2 country=2 }
			3 = { family=3 country=3 }
			"""));
		characters.LinkFamilies(families);

		var countries = new CountryCollection();
		countries.Add(Country.Parse(new BufferedReader("{ tag=PRY monarch=1 }"), 1));
		countries.Add(Country.Parse(new BufferedReader("{ tag=MRY monarch=2 }"), 2));
		countries.Add(Country.Parse(new BufferedReader("{ tag=SEL monarch=3 }"), 3));
		characters.LinkCountries(countries);

		Assert.Equal("Antigonid Mauryan Seleucid", countryName.GetAdjectiveLocBlock(locDB, countries)!["english"]);
	}

	[Fact]
	public void DataTypeAdjectivesFallBackWhenMonarchFamilyDiffers() {
		var reader = new BufferedReader(
			"""
			name="CIVILWAR_FACTION_NAME"
			adjective="CIVILWAR_FACTION_ADJECTIVE"
			base={
				name="PRY_DYN"
				adjective="PRY_DYN_ADJ"
			}
			"""
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		var dynAdjLocBlock = locDB.AddLocBlock("PRY_DYN_ADJ");
		dynAdjLocBlock["english"] =
			"[GetCountry('PRY').Custom('get_pry_adj')] [GetCountry('MRY').Custom('get_mry_adj')] [GetCountry('SEL').Custom('get_sel_adj')]";
		var fallbackPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fallback");
		fallbackPryAdjLocBlock["english"] = "Phrygian";
		var fallbackMryAdjLocBlock = locDB.AddLocBlock("get_mry_adj_fallback");
		fallbackMryAdjLocBlock["english"] = "Mauryan";
		var fallbackSelAdjLocBlock = locDB.AddLocBlock("get_sel_adj_fallback");
		fallbackSelAdjLocBlock["english"] = "Seleucid";

		var families = new FamilyCollection();
		families.LoadFamilies(new BufferedReader("""
			1 = { key="Ptolemaios" }
			2 = { key="Nanda" }
			3 = { key="Attalid" }
			"""));
		var characters = new CharacterCollection();
		characters.LoadCharacters(new BufferedReader("""
			1 = { family=1 country=1 }
			2 = { family=2 country=2 }
			3 = { family=3 country=3 }
			"""));
		characters.LinkFamilies(families);

		var countries = new CountryCollection();
		countries.Add(Country.Parse(new BufferedReader("{ tag=PRY monarch=1 }"), 1));
		countries.Add(Country.Parse(new BufferedReader("{ tag=MRY monarch=2 }"), 2));
		countries.Add(Country.Parse(new BufferedReader("{ tag=SEL monarch=3 }"), 3));
		characters.LinkCountries(countries);

		Assert.Equal("Phrygian Mauryan Seleucid", countryName.GetAdjectiveLocBlock(locDB, countries)!["english"]);
	}

	[Fact]
	public void DataTypeAdjectivesFallBackWhenMonarchOrFamilyIsNotLinked() {
		const string dataTypeAdjs =
			"[GetCountry('PRY').Custom('get_pry_adj')] [GetCountry('MRY').Custom('get_mry_adj')] [GetCountry('SEL').Custom('get_sel_adj')]";
		var reader = new BufferedReader(
			"""
			name="CIVILWAR_FACTION_NAME"
			adjective="CIVILWAR_FACTION_ADJECTIVE"
			base={
				name="PRY_DYN"
				adjective="PRY_DYN_ADJ"
			}
			"""
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		var dynAdjLocBlock = locDB.AddLocBlock("PRY_DYN_ADJ");
		dynAdjLocBlock["english"] = dataTypeAdjs;
		var fallbackPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fallback");
		fallbackPryAdjLocBlock["english"] = "Phrygian";
		var fallbackMryAdjLocBlock = locDB.AddLocBlock("get_mry_adj_fallback");
		fallbackMryAdjLocBlock["english"] = "Mauryan";
		var fallbackSelAdjLocBlock = locDB.AddLocBlock("get_sel_adj_fallback");
		fallbackSelAdjLocBlock["english"] = "Seleucid";

		// PRY without a monarch, MRY and SEL with monarchs that have no linked family.
		var familiesLessCharacters = new CharacterCollection();
		familiesLessCharacters.LoadCharacters(new BufferedReader("""
			1 = { country=2 }
			2 = { country=3 }
			"""));
		var countries = new CountryCollection();
		countries.Add(Country.Parse(new BufferedReader("{ tag=PRY }"), 1));
		countries.Add(Country.Parse(new BufferedReader("{ tag=MRY monarch=1 }"), 2));
		countries.Add(Country.Parse(new BufferedReader("{ tag=SEL monarch=2 }"), 3));
		familiesLessCharacters.LinkCountries(countries);

		Assert.Equal("Phrygian Mauryan Seleucid", countryName.GetAdjectiveLocBlock(locDB, countries)!["english"]);

		// PRY with a familyless monarch, MRY and SEL without monarchs.
		var characters = new CharacterCollection();
		characters.LoadCharacters(new BufferedReader(
			"1 = { country=1 }"
		));
		countries = new CountryCollection();
		countries.Add(Country.Parse(new BufferedReader("{ tag=PRY monarch=1 }"), 1));
		countries.Add(Country.Parse(new BufferedReader("{ tag=MRY }"), 2));
		countries.Add(Country.Parse(new BufferedReader("{ tag=SEL }"), 3));
		characters.LinkCountries(countries);

		Assert.Equal("Phrygian Mauryan Seleucid", countryName.GetAdjectiveLocBlock(locDB, countries)!["english"]);
	}

	[Fact]
	public void DataTypeAdjectivesFallBackWhenCountriesAreMissing() {
		var reader = new BufferedReader(
			"""
			name="CIVILWAR_FACTION_NAME"
			adjective="CIVILWAR_FACTION_ADJECTIVE"
			base={
				name="PRY_DYN"
				adjective="PRY_DYN_ADJ"
			}
			"""
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		var dynAdjLocBlock = locDB.AddLocBlock("PRY_DYN_ADJ");
		dynAdjLocBlock["english"] =
			"[GetCountry('PRY').Custom('get_pry_adj')] [GetCountry('MRY').Custom('get_mry_adj')] [GetCountry('SEL').Custom('get_sel_adj')]";
		var fallbackPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fallback");
		fallbackPryAdjLocBlock["english"] = "Phrygian";
		var fallbackMryAdjLocBlock = locDB.AddLocBlock("get_mry_adj_fallback");
		fallbackMryAdjLocBlock["english"] = "Mauryan";
		var fallbackSelAdjLocBlock = locDB.AddLocBlock("get_sel_adj_fallback");
		fallbackSelAdjLocBlock["english"] = "Seleucid";

		Assert.Equal("Phrygian Mauryan Seleucid", countryName.GetAdjectiveLocBlock(locDB, [])!["english"]);
	}

	[Fact]
	public void DataTypeAdjectivesAreKeptUntouchedWhenFetchAndFallbackLocsAreMissing() {
		const string dataTypeAdjs =
			"[GetCountry('PRY').Custom('get_pry_adj')] [GetCountry('MRY').Custom('get_mry_adj')] [GetCountry('SEL').Custom('get_sel_adj')]";
		var reader = new BufferedReader(
			"""
			name="CIVILWAR_FACTION_NAME"
			adjective="CIVILWAR_FACTION_ADJECTIVE"
			base={
				name="PRY_DYN"
				adjective="PRY_DYN_ADJ"
			}
			"""
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		var dynAdjLocBlock = locDB.AddLocBlock("PRY_DYN_ADJ");
		dynAdjLocBlock["english"] = dataTypeAdjs;

		Assert.Equal(dataTypeAdjs, countryName.GetAdjectiveLocBlock(locDB, [])!["english"]);
	}

	[Fact]
	public void DataTypeAdjectivesAreKeptForLanguagesMissingFromDynastyLocBlocks() {
		const string dataTypeAdjs =
			"[GetCountry('PRY').Custom('get_pry_adj')] [GetCountry('MRY').Custom('get_mry_adj')] [GetCountry('SEL').Custom('get_sel_adj')]";
		var reader = new BufferedReader(
			"""
			name="CIVILWAR_FACTION_NAME"
			adjective="CIVILWAR_FACTION_ADJECTIVE"
			base={
				name="PRY_DYN"
				adjective="PRY_DYN_ADJ"
			}
			"""
		);
		var countryName = CountryName.Parse(reader);

		var locDB = new LocDB("english", "french");
		var civilWarAdjLocBlock = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
		civilWarAdjLocBlock["english"] = "$ADJ$";
		civilWarAdjLocBlock["french"] = "$ADJ$";
		var dynAdjLocBlock = locDB.AddLocBlock("PRY_DYN_ADJ");
		dynAdjLocBlock["english"] = dataTypeAdjs;
		dynAdjLocBlock["french"] = dataTypeAdjs;
		// None of the blocks below get a French translation.
		var antigonidPryAdjLocBlock = locDB.AddLocBlock("get_pry_adj_fetch");
		antigonidPryAdjLocBlock["english"] = "Antigonid";
		var fallbackMryAdjLocBlock = locDB.AddLocBlock("get_mry_adj_fallback");
		fallbackMryAdjLocBlock["english"] = "Mauryan";
		var fallbackSelAdjLocBlock = locDB.AddLocBlock("get_sel_adj_fallback");
		fallbackSelAdjLocBlock["english"] = "Seleucid";

		var families = new FamilyCollection();
		families.LoadFamilies(new BufferedReader("""
			1 = { key="Antigonid" }
			2 = { key="Nanda" }
			"""));
		var characters = new CharacterCollection();
		characters.LoadCharacters(new BufferedReader("""
			1 = { family=1 country=1 }
			2 = { family=2 country=2 }
			"""));
		characters.LinkFamilies(families);

		var countries = new CountryCollection();
		countries.Add(Country.Parse(new BufferedReader("{ tag=PRY monarch=1 }"), 1));
		countries.Add(Country.Parse(new BufferedReader("{ tag=MRY monarch=2 }"), 2));
		characters.LinkCountries(countries);

		Assert.Equal("Antigonid Mauryan Seleucid", countryName.GetAdjectiveLocBlock(locDB, countries)!["english"]);
		Assert.Equal(dataTypeAdjs, countryName.GetAdjectiveLocBlock(locDB, countries)!["french"]);
	}
}