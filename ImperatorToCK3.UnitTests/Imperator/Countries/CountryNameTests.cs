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
}