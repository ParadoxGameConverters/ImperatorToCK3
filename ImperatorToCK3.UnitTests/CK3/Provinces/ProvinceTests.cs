using commonItems;
using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using System;

namespace ImperatorToCK3.UnitTests.CK3.Provinces;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvinceTests {
	private const string ImperatorRoot = "TestFiles/Imperator/root";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly AreaCollection areas = new();
	private static readonly ImperatorRegionMapper irRegionMapper = new(irModFS, areas);
	private readonly Date ck3BookmarkDate = "476.1.1";
	private readonly StateCollection states = new();
	private static readonly CountryCollection countries = new();

	static ProvinceTests() {
		countries.LoadCountries(new BufferedReader("1={}"));
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
			var province = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader, id, states, countries);
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
		var cultureMapper = new CultureMapper(irRegionMapper, ck3RegionMapper);
		var religionMapper = new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper);
		var config = new Configuration();

		var ck3Provinces = new List<Province>();
		foreach (var irProvince in irProvinces) {
			var ck3Province = new Province(irProvince.Id);
			ck3Provinces.Add(ck3Province);
			ck3Province.InitializeFromImperator(
				irProvince,
				ImmutableHashSet<ImperatorToCK3.Imperator.Provinces.Province>.Empty,
				landedTitles,
				cultureMapper,
				religionMapper,
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
			" = { province_rank=settlement }",
		});
		ck3Provinces = GetCK3ProvincesForIRGovernment(irProvinces, "tribal_federation");
		holdingTypes = ck3Provinces.Select(p => p.GetHoldingType(ck3BookmarkDate));
		holdingTypes.Should().Equal(
			"church_holding",
			"castle_holding",
			"city_holding",
			"none"
		);
	}
}