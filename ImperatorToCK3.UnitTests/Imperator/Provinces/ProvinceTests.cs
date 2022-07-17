using commonItems;
using commonItems.Mods;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Imperator.Religions;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Provinces;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvinceTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private readonly ModFilesystem imperatorModFS = new(ImperatorRoot, new Mod[] { });
	
	[Fact]
	public void IdCanBeSet() {
		var reader = new BufferedReader(
			"= {}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Equal((ulong)42, theProvince.Id);
	}

	[Fact]
	public void CultureIdCanBeSet() {
		var reader = new BufferedReader(
			"= {\n" +
			"\tculture=\"paradoxian\"" +
			"}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Equal("paradoxian", theProvince.Culture);
	}

	[Fact]
	public void CultureIdDefaultsToBlank() {
		var reader = new BufferedReader(
			"= {}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.True(string.IsNullOrEmpty(theProvince.Culture));
	}

	[Fact]
	public void ReligionIdCanBeSet() {
		var reader = new BufferedReader(
			"= {\n" +
			"\treligion=\"paradoxian\"" +
			"}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Equal("paradoxian", theProvince.Religion);
	}

	[Fact]
	public void ReligionIdDefaultsToBlank() {
		var reader = new BufferedReader(
			"= {}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.True(string.IsNullOrEmpty(theProvince.Religion));
	}

	[Fact]
	public void GetReligionReturnsCorrectReligion() {
		var religions = new ReligionCollection(new ScriptValueCollection());
		religions.LoadReligions(imperatorModFS);
		
		var province = new Province(1) {Religion = "roman_pantheon"};

		var religion = province.GetReligion(religions);
		Assert.NotNull(religion);
		Assert.Equal("roman_pantheon", religion.Id);
	}

	[Fact]
	public void GetReligionReturnsNullWhenReligionIsNotFound() {
		var religions = new ReligionCollection(new ScriptValueCollection());
		religions.LoadReligions(imperatorModFS);
		
		var province = new Province(1) {Religion = "missing_religion"};

		var religion = province.GetReligion(religions);
		Assert.Null(religion);
	}

	[Fact]
	public void NameCanBeSet() {
		var reader = new BufferedReader(
			"= {\n" +
			"province_name = {\n" +
			"name=\"Biggus Dickus\"\n" +
			"}\n" +
			"}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Equal("Biggus Dickus", theProvince.Name);
	}

	[Fact]
	public void NameDefaultsToBlank() {
		var reader = new BufferedReader(
			"= {}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.True(string.IsNullOrEmpty(theProvince.Name));
	}

	[Fact]
	public void OwnerCanBeSet() {
		var reader = new BufferedReader(
			"= {\n" +
			"\towner=69\n" +
			"}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Null(theProvince.OwnerCountry); // not linked yet

		var countries = new CountryCollection(new BufferedReader("69 = {}"));
		theProvince.TryLinkOwnerCountry(countries);

		Assert.NotNull(theProvince.OwnerCountry);
		Assert.Equal((ulong)69, theProvince.OwnerCountry.Id);
	}

	[Fact]
	public void OwnerDefaultsToNull() {
		var reader = new BufferedReader(
			"= {}"
		);
		var theProvince = Province.Parse(reader, 42);

		Assert.Null(theProvince.OwnerCountry);
	}

	[Fact]
	public void ControllerCanBeSet() {
		var reader = new BufferedReader(
			"= {\n" +
			"\tcontroller=69\n" +
			"}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Equal((ulong)69, theProvince.Controller);
	}

	[Fact]
	public void PopsCanBeSet() {
		var reader = new BufferedReader(
			"= {\n" +
			"\tpop=69\n" +
			"\tpop=68\n" +
			"\tpop=12213\n" +
			"\tpop=23\n" +
			"}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Equal(0, theProvince.GetPopCount()); // pops not linked yet

		var pops = new ImperatorToCK3.Imperator.Pops.PopCollection();
		var pop1 = new ImperatorToCK3.Imperator.Pops.Pop(69);
		var pop2 = new ImperatorToCK3.Imperator.Pops.Pop(68);
		var pop3 = new ImperatorToCK3.Imperator.Pops.Pop(12213);
		var pop4 = new ImperatorToCK3.Imperator.Pops.Pop(23);
		pops.Add(pop1);
		pops.Add(pop2);
		pops.Add(pop3);
		pops.Add(pop4);
		theProvince.LinkPops(pops);
		Assert.Equal(4, theProvince.GetPopCount());
	}

	[Fact]
	public void ProvinceRankDefaultsToSettlement() {
		var reader = new BufferedReader(string.Empty);
		var province = Province.Parse(reader, 42);

		Assert.Equal(ProvinceRank.settlement, province.ProvinceRank);
	}

	[Fact]
	public void ProvinceRankCanBeSet() {
		var reader = new BufferedReader("= { province_rank=settlement }");
		var reader2 = new BufferedReader("= { province_rank=city }");
		var reader3 = new BufferedReader("= { province_rank=city_metropolis }");

		var province = Province.Parse(reader, 42);
		var province2 = Province.Parse(reader2, 43);
		var province3 = Province.Parse(reader3, 44);

		Assert.Equal(ProvinceRank.settlement, province.ProvinceRank);
		Assert.Equal(ProvinceRank.city, province2.ProvinceRank);
		Assert.Equal(ProvinceRank.city_metropolis, province3.ProvinceRank);
	}

	[Fact]
	public void FortDefaultsToFalse() {
		var reader = new BufferedReader(string.Empty);
		var province = Province.Parse(reader, 42);

		Assert.False(province.Fort);
	}

	[Fact]
	public void FortCanBeSet() {
		var reader = new BufferedReader(" = { fort=yes }");
		var province = Province.Parse(reader, 42);

		Assert.True(province.Fort);
	}

	[Fact]
	public void HolySiteIdDefaultsToNull() {
		var reader = new BufferedReader(" = { }");
		var province = Province.Parse(reader, 42);

		Assert.False(province.IsHolySite);
		Assert.Null(province.HolySiteId);
	}

	[Fact]
	public void HolySiteIdCanBeSet() {
		var reader = new BufferedReader(" = { holy_site=4294967295 }"); // this value means no holy site
		var reader2 = new BufferedReader(" = { holy_site=56 }");
		var province = Province.Parse(reader, 42);
		var province2 = Province.Parse(reader2, 43);

		Assert.False(province.IsHolySite);
		Assert.Null(province.HolySiteId);
		Assert.True(province2.IsHolySite);
		Assert.Equal((ulong)56, province2.HolySiteId);
	}

	[Fact]
	public void GetHolySiteDeityReturnsCorrectDeity() {
		var religions = new ReligionCollection(new ScriptValueCollection());
		religions.LoadDeities(imperatorModFS);

		var holySiteManager = new DeityManager();
		var holySitesReader = new BufferedReader(@"deities_database = {
				1 = { deity=""deity1"" }
				34 = { deity=""deity3"" }
				2 = { deity=""deity4"" }
			}");
		holySiteManager.LoadHolySiteDatabase(holySitesReader);
		// holy site 34 belongs to deity "deity3"
		var province = new Province(1) {HolySiteId = 34};

		var deity = province.GetHolySiteDeity(holySiteManager, religions.Deities);
		Assert.NotNull(deity);
		Assert.Equal("deity3", deity.Id);
	}
	
	[Fact]
	public void GetHolySiteDeityReturnsNullHolySiteIdIsNull() {
		var religions = new ReligionCollection(new ScriptValueCollection());
		religions.LoadDeities(imperatorModFS);

		var holySiteManager = new DeityManager();
		var holySitesReader = new BufferedReader(@"deities_database = {
				1 = { deity=""deity1"" }
				34 = { deity=""deity3"" }
				2 = { deity=""deity4"" }
			}");
		holySiteManager.LoadHolySiteDatabase(holySitesReader);
		var province = new Province(1) {HolySiteId = null};

		var deity = province.GetHolySiteDeity(holySiteManager, religions.Deities);
		Assert.Null(deity);
	}

	[Fact]
	public void BuildingsCountCanBeSet() {
		var reader = new BufferedReader(
			"= {\n" +
			"\tbuildings = {0 1 0 65 3}\n" +
			"}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Equal((ulong)69, theProvince.BuildingCount);
	}

	[Fact]
	public void BuildingsCountDefaultsTo0() {
		var reader = new BufferedReader(
			"={}"
		);

		var theProvince = Province.Parse(reader, 42);

		Assert.Equal((ulong)0, theProvince.BuildingCount);
	}

	[Fact]
	public void LinkingCountryWithoutMatchingIdIsLogged() {
		var reader = new BufferedReader("= { owner = 50 }");
		var province = Province.Parse(reader, 42);

		var countryReader = new BufferedReader(string.Empty);
		var country = Country.Parse(countryReader, 49);

		var output = new StringWriter();
		Console.SetOut(output);
		province.LinkOwnerCountry(country);
		var logStr = output.ToString();
		Assert.Contains("[WARN] Province 42: linking owner 49 that doesn't match owner from save (50)!", logStr);
	}

	[Fact]
	public void CountryLinkingWorks() {
		var reader = new BufferedReader("= { owner = 50 }");
		var province = Province.Parse(reader, 42);

		var countryReader = new BufferedReader(string.Empty);
		var country = Country.Parse(countryReader, 50);

		province.LinkOwnerCountry(country);
		Assert.NotNull(province.OwnerCountry);
		Assert.Equal((ulong)50, province.OwnerCountry.Id);
	}

	[Fact]
	public void IgnoredTokensAreSaved() {
		var reader1 = new BufferedReader("= { culture=paradoxian ignoredKeyword1=something ignoredKeyword2={} }");
		var reader2 = new BufferedReader("= { ignoredKeyword1=stuff ignoredKeyword3=stuff }");
		_ = Province.Parse(reader1, 1);
		_ = Province.Parse(reader2, 2);

		var expectedIgnoredTokens = new HashSet<string> {
			"ignoredKeyword1", "ignoredKeyword2", "ignoredKeyword3"
		};
		Assert.True(Province.IgnoredTokens.SetEquals(expectedIgnoredTokens));
	}
}