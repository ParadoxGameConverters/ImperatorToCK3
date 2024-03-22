using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvincesTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	
	private const string CK3Root = "TestFiles/CK3ProvincesTests";
	private readonly ModFilesystem ck3ModFs = new(CK3Root, new List<Mod>());
	private readonly Date ck3BookmarkDate = new("867.1.1");

	[Fact]
	public void ProvincesDefaultToEmpty() {
		var provinces = new ImperatorToCK3.CK3.Provinces.ProvinceCollection();

		Assert.Empty(provinces);
	}

	[Fact]
	public void ProvincesAreProperlyLoadedFromFilesystem() {
		var provinces = new ImperatorToCK3.CK3.Provinces.ProvinceCollection(ck3ModFs);

		Assert.Collection(provinces.OrderBy(p => p.Id),
			prov => {
				Assert.Equal((ulong)3080, prov.Id);
				Assert.Equal("slovien", prov.GetCultureId(ck3BookmarkDate));
				Assert.Equal("catholic", prov.GetFaithId(ck3BookmarkDate));
			},
			prov => {
				Assert.Equal((ulong)4125, prov.Id);
				Assert.Equal("czech", prov.GetCultureId(ck3BookmarkDate));
				Assert.Equal("slavic_pagan", prov.GetFaithId(ck3BookmarkDate));
			},
			prov => {
				Assert.Equal((ulong)4161, prov.Id);
				Assert.Equal("czech", prov.GetCultureId(ck3BookmarkDate));
				Assert.Equal("slavic_pagan", prov.GetFaithId(ck3BookmarkDate));
			},
			prov => {
				Assert.Equal((ulong)4165, prov.Id);
				Assert.Equal("slovien", prov.GetCultureId(ck3BookmarkDate));
				Assert.Equal("catholic", prov.GetFaithId(ck3BookmarkDate));
			}
		);
	}

	[Fact]
	public void PrimaryImperatorProvinceIsProperlyDeterminedForCK3Province() {
		var conversionDate = new Date(476, 1, 1);
		var config = new Configuration { CK3BookmarkDate = conversionDate };
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader(
			"""
			c_county1={
				b_barony1={province=1}
			}
			""");
		titles.LoadTitles(titlesReader);
		
		// Scenario 1: Sum of civilisation in country 1 outweighs single more civilized province in country 2.
		var irWorld = new TestImperatorWorld(config);
		// Country 1 (civilisation 9 in total)
		var country1 = new Country(1);
		var irProvince1 = new ImperatorToCK3.Imperator.Provinces.Province(1) { CivilizationValue = 1, OwnerCountry = country1};
		irWorld.Provinces.Add(irProvince1);
		var irProvince2 = new ImperatorToCK3.Imperator.Provinces.Province(2) { CivilizationValue = 2, OwnerCountry = country1 };
		irWorld.Provinces.Add(irProvince2);
		var irProvince3 = new ImperatorToCK3.Imperator.Provinces.Province(3) { CivilizationValue = 3, OwnerCountry = country1 };
		irWorld.Provinces.Add(irProvince3);
		var irProvince4 = new ImperatorToCK3.Imperator.Provinces.Province(4) { CivilizationValue = 2, OwnerCountry = country1 };
		irWorld.Provinces.Add(irProvince4);
		var irProvince5 = new ImperatorToCK3.Imperator.Provinces.Province(5) { CivilizationValue = 1, OwnerCountry = country1 };
		irWorld.Provinces.Add(irProvince5);
		// Country 2 (civilisation 5 in total)
		var country2 = new Country(2);
		var irProvince6 = new ImperatorToCK3.Imperator.Provinces.Province(6) { CivilizationValue = 5, OwnerCountry = country2 };
		irWorld.Provinces.Add(irProvince6);

		var provinceMapper = new ProvinceMapper();
		const string provinceMappingsPath = "TestFiles/LandedTitlesTests/province_mappings.txt";
		provinceMapper.LoadMappings(provinceMappingsPath, "6_to_1");

		var ck3Provinces = new ProvinceCollection { new(1) };
		var ck3RegionMapper = new CK3RegionMapper();
		AreaCollection areas = new();
		areas.LoadAreas(irModFS, irWorld.Provinces);
		var irRegionMapper = new ImperatorRegionMapper(areas, new MapData(irModFS));
		var colorFactory = new ColorFactory();
		irRegionMapper.LoadRegions(irModFS, colorFactory);
		var cultures = new CultureCollection(colorFactory, new PillarCollection(colorFactory, []), []);
		var cultureMapper = new CultureMapper(irRegionMapper, ck3RegionMapper, cultures);
		var religions = new ReligionCollection(titles);
		var religionMapper = new ReligionMapper(religions, irRegionMapper, ck3RegionMapper);
		ck3Provinces.ImportImperatorProvinces(irWorld, titles, cultureMapper, religionMapper, provinceMapper, conversionDate, config);
		
		var targetProvince = ck3Provinces[1];
		Assert.Equal((ulong)1, targetProvince.Id);
		var primarySourceProvince = targetProvince.PrimaryImperatorProvince;
		Assert.NotNull(primarySourceProvince);
		Assert.Equal((ulong)3, primarySourceProvince.Id); // most developed province in country 1
		
		// Scenario 2: Single developed province in country 2 outweighs sum of civilisation in country 1.
		irProvince6.CivilizationValue = 100;
		ck3Provinces = new ProvinceCollection { new(1) };
		ck3Provinces.ImportImperatorProvinces(irWorld, titles, cultureMapper, religionMapper, provinceMapper, conversionDate, config);
		
		targetProvince = ck3Provinces[1];
		Assert.Equal((ulong)1, targetProvince.Id);
		primarySourceProvince = targetProvince.PrimaryImperatorProvince;
		Assert.NotNull(primarySourceProvince);
		Assert.Equal((ulong)6, primarySourceProvince.Id); // province of country 2
	}
}