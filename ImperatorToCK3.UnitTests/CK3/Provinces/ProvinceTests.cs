using commonItems;
using commonItems.Colors;
using commonItems.Collections;
using commonItems.Mods;
using AwesomeAssertions;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;using ImperatorToCK3.UnitTests.TestHelpers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;
using System;

namespace ImperatorToCK3.UnitTests.CK3.Provinces;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvinceTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem IRModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(IRModFS);
	private static readonly ImperatorRegionMapper IRRegionMapper;
	private readonly Date ck3BookmarkDate = "476.1.1";
	private readonly StateCollection states = new();
	private static readonly CountryCollection Countries = new();
	private static readonly TestCK3CultureCollection Cultures = new();

	static ProvinceTests() {
		var irProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection {new(1), new(2), new(3)};
		AreaCollection areas = new();
		areas.LoadAreas(IRModFS, irProvinces);
		IRRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		IRRegionMapper.LoadRegions(IRModFS, new ColorFactory());
		
		Countries.LoadCountries(new BufferedReader("1={}"));
	}

	[Fact]
	public void FieldsCanBeSet() {
		// ReSharper disable once UseObjectOrCollectionInitializer
		var province = new Province(1);
		province.SetFaithId("orthodox", null);
		Assert.Equal("orthodox", province.GetFaithId(ck3BookmarkDate));
		province.SetCultureId("roman", ck3BookmarkDate);
		Assert.Equal("roman", province.GetCultureId(ck3BookmarkDate));
	}

	[Fact]
	public void ImperatorProvincesContainPrimaryAndSecondarySources() {
		// ImperatorProvinces is cached on first access, so use separate instances.
		Assert.Empty(new Province(1).ImperatorProvinces);

		var provinceWithPrimary = new Province(2);
		var primary = new ImperatorToCK3.Imperator.Provinces.Province(10);
		provinceWithPrimary.PrimaryImperatorProvince = primary;
		Assert.Equal(primary, Assert.Single(provinceWithPrimary.ImperatorProvinces));
	}

	private Province InitializeProvinceFromImperator(
		ImperatorToCK3.Imperator.Provinces.Province primarySource,
		OrderedSet<ImperatorToCK3.Imperator.Provinces.Province> secondarySources,
		CultureMapper cultureMapper,
		ReligionMapper religionMapper) {
		var ck3Province = new Province(100);
		ck3Province.InitializeFromImperator(
			primarySource,
			secondarySources,
			new Title.LandedTitles(),
			cultureMapper,
			religionMapper,
			ck3BookmarkDate,
			new Configuration()
		);
		return ck3Province;
	}

	private static ImperatorToCK3.Imperator.Provinces.Province MakeIRProvince(
		ulong id, string culture, string religionId, Country? owner = null) {
		return new ImperatorToCK3.Imperator.Provinces.Province(id) {
			Culture = culture,
			ReligionId = religionId,
			OwnerCountry = owner
		};
	}

	private (CultureMapper, ReligionMapper) MakeLinkedMappers() {
		if (!Cultures.ContainsKey("roman")) {
			Cultures.GenerateTestCulture("roman");
		}
		var cultureMapper = new CultureMapper(
			new BufferedReader("link = { ir=roman ck3=roman }"),
			IRRegionMapper,
			new CK3RegionMapper(),
			Cultures);
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion",
			new BufferedReader("faiths={ hellenic={} }"), ck3Religions, new ColorFactory());
		ck3Religions.AddOrReplace(religion);
		var religionMapper = new ReligionMapper(
			new BufferedReader("link={ir=hellenic ck3=hellenic}"),
			ck3Religions,
			IRRegionMapper,
			new CK3RegionMapper());
		return (cultureMapper, religionMapper);
	}

	[Fact]
	public void PrimaryReligionAndCultureAreUsedWhenMapped() {
		var (cultureMapper, religionMapper) = MakeLinkedMappers();
		var primary = MakeIRProvince(1, "roman", "hellenic");

		var ck3Province = InitializeProvinceFromImperator(primary, [], cultureMapper, religionMapper);

		Assert.Equal("hellenic", ck3Province.GetFaithId(ck3BookmarkDate));
		Assert.Equal("roman", ck3Province.GetCultureId(ck3BookmarkDate));
	}

	[Fact]
	public void SecondaryReligionAndCultureAreUsedWhenPrimaryIsUnmapped() {
		var (cultureMapper, religionMapper) = MakeLinkedMappers();
		var primary = MakeIRProvince(1, "unmapped_culture", "unmapped_faith");
		var secondary = MakeIRProvince(2, "roman", "hellenic");

		var ck3Province = InitializeProvinceFromImperator(
			primary, [secondary], cultureMapper, religionMapper);

		Assert.Equal("hellenic", ck3Province.GetFaithId(ck3BookmarkDate));
		Assert.Equal("roman", ck3Province.GetCultureId(ck3BookmarkDate));
	}

	[Fact]
	public void CountryReligionAndCultureAreUsedAsFallback() {
		var (cultureMapper, religionMapper) = MakeLinkedMappers();
		var owner = Country.Parse(new BufferedReader(
			"= { religion=hellenic primary_culture=roman }"), 1);
		var primary = MakeIRProvince(1, "unmapped_culture", "unmapped_faith", owner);

		var ck3Province = InitializeProvinceFromImperator(primary, [], cultureMapper, religionMapper);

		Assert.Equal("hellenic", ck3Province.GetFaithId(ck3BookmarkDate));
		Assert.Equal("roman", ck3Province.GetCultureId(ck3BookmarkDate));
	}

	[Fact]
	public void FaithAndCultureStayEmptyWhenNothingIsMapped() {
		var (cultureMapper, religionMapper) = MakeLinkedMappers();
		var primary = MakeIRProvince(1, "unmapped_culture", "unmapped_faith");

		var output = new StringWriter();
		Console.SetOut(output);
		var ck3Province = InitializeProvinceFromImperator(primary, [], cultureMapper, religionMapper);
		var log = output.ToString();

		Assert.Null(ck3Province.GetFaithId(ck3BookmarkDate));
		Assert.Null(ck3Province.GetCultureId(ck3BookmarkDate));
		Assert.Contains("Couldn't determine faith", log);
		Assert.Contains("Couldn't determine culture", log);
	}

	[Fact]
	public void ProvinceCanBeLoadedFromStream() {
		var reader = new BufferedReader(
			"{ culture=roman random_key=random_value religion=orthodox holding=castle_holding }"
		);
		var province = new Province(42, reader);
		Assert.Equal((ulong)42, province.Id);
		Assert.Equal("orthodox", province.GetFaithId(ck3BookmarkDate));
		Assert.Equal("roman", province.GetCultureId(ck3BookmarkDate));
		Assert.Equal("castle_holding", province.GetHoldingType(ck3BookmarkDate));
	}

	private IReadOnlyCollection<ImperatorToCK3.Imperator.Provinces.Province> GetIRProvincesFromStrings(ICollection<string> strings) {
		var provincesToReturn = new List<ImperatorToCK3.Imperator.Provinces.Province>();

		ulong id = 1;
		foreach (var provinceStr in strings) {
			var reader = new BufferedReader(provinceStr);
			var province = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader, id, states, Countries);
			provincesToReturn.Add(province);
			++id;
		}

		return provincesToReturn.AsReadOnly();
	}

	private IReadOnlyCollection<Province> GetCK3ProvincesForIRGovernment(IReadOnlyCollection<ImperatorToCK3.Imperator.Provinces.Province> irProvinces, string irGovernmentId) {
		var countryReader = new BufferedReader($"government_key = {irGovernmentId}");
		var imperatorCountry = Country.Parse(countryReader, 1);

		foreach (var irProvince in irProvinces) {
			irProvince.OwnerCountry = imperatorCountry;
		}

		var landedTitles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(landedTitles);
		var ck3RegionMapper = new CK3RegionMapper();
		var cultureMapper = new CultureMapper(IRRegionMapper, ck3RegionMapper, Cultures);
		var religionMapper = new ReligionMapper(ck3Religions, IRRegionMapper, ck3RegionMapper);
		var config = new Configuration();

		var ck3Provinces = new List<Province>();
		foreach (var irProvince in irProvinces) {
			var ck3Province = new Province(irProvince.Id);
			ck3Provinces.Add(ck3Province);
			ck3Province.InitializeFromImperator(
				irProvince,
				new OrderedSet<ImperatorToCK3.Imperator.Provinces.Province>(),
				landedTitles,
				cultureMapper,
				religionMapper,
				ck3BookmarkDate,
				config
			);
		}

		return ck3Provinces.AsReadOnly();
	}

	[Fact]
	public void SetHoldingLogicWorksCorrectlyForAllGovernmentTypes() {
		const string imperatorRoot = "TestFiles/Imperator/game";
		var mods = new List<Mod> {
			new("cool_mod", "TestFiles/documents/Imperator/mod/cool_mod")
		};
		var imperatorModFS = new ModFilesystem(imperatorRoot, mods);

		Country.LoadGovernments(imperatorModFS);

		// Monarchy.
		var irProvinces = GetIRProvincesFromStrings(new[] {
			" = { province_rank=city_metropolis }",
			" = { province_rank=city holy_site=69 fort=yes }",
			" = { province_rank=city fort=yes }",
			" = { province_rank=city }",
			" = { province_rank=settlement holy_site=69 fort=yes }",
			" = { province_rank=settlement fort=yes }",
			" = { province_rank=settlement }"
		});
		var ck3Provinces = GetCK3ProvincesForIRGovernment(irProvinces, "super_monarchy");
		var holdingTypes = ck3Provinces.Select(p => p.GetHoldingType(ck3BookmarkDate));
		holdingTypes.Should().Equal(
			"city_holding",
			"church_holding",
			"castle_holding",
			"city_holding",
			"church_holding",
			"castle_holding",
			"none"
		);

		// Republic.
		irProvinces = GetIRProvincesFromStrings(new[] {
			" = { province_rank=city_metropolis holy_site=69 }",
			" = { province_rank=city fort=yes }",
			" = { province_rank=city }",
			" = { province_rank=settlement holy_site=69 fort=yes }",
			" = { province_rank=settlement fort=yes }",
			" = { province_rank=settlement }"
		});
		ck3Provinces = GetCK3ProvincesForIRGovernment(irProvinces, "aristocratic_republic");
		holdingTypes = ck3Provinces.Select(p => p.GetHoldingType(ck3BookmarkDate));
		holdingTypes.Should().Equal(
			"church_holding",
			"city_holding",
			"city_holding",
			"church_holding",
			"city_holding",
			"none"
		);

		// Tribal.
		irProvinces = GetIRProvincesFromStrings(new[] {
			" = { province_rank=city_metropolis holy_site=69 fort=yes }",
			" = { province_rank=city_metropolis fort=yes }",
			" = { province_rank=city_metropolis }",
			" = { province_rank=settlement fort=yes}",
			" = { province_rank=settlement }",
		});
		ck3Provinces = GetCK3ProvincesForIRGovernment(irProvinces, "tribal_federation");
		holdingTypes = ck3Provinces.Select(p => p.GetHoldingType(ck3BookmarkDate));
		holdingTypes.Should().Equal(
			"church_holding",
			"city_holding", // For non-capital baronies of tribal realms, forts are not converted to castles.
			"city_holding",
			"none", // For non-capital baronies of tribal realms, forts are not converted to castles.
			"none"
		);
	}
}