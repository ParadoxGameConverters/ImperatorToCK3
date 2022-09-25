using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvinceTests {
	private const string ImperatorRoot = "TestFiles/Imperator/root";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, new Mod[] { });
	private static readonly AreaCollection areas = new();
	private static readonly ImperatorRegionMapper irRegionMapper = new(irModFS, areas);
	private readonly Date ck3BookmarkDate = "476.1.1";
	private readonly StateCollection states = new();
	private readonly CountryCollection countries = new(new BufferedReader("1={}"));
	
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

	[Fact]
	public void SetHoldingLogicWorks() {
		var reader1 = new BufferedReader(" = { owner=1 province_rank=city_metropolis }");
		var reader2 = new BufferedReader(" = { owner=1 province_rank=city fort=yes }");
		var reader3 = new BufferedReader(" = { owner=1 province_rank=city }");
		var reader4 = new BufferedReader(" = { owner=1 province_rank=settlement holy_site = 69 fort=yes }");
		var reader5 = new BufferedReader(" = { owner=1 province_rank=settlement fort=yes }");
		var reader6 = new BufferedReader(" = { owner=1 province_rank=settlement }");
		
		var impProvince = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader1, 42, states, countries);
		var impProvince2 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader2, 43, states, countries);
		var impProvince3 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader3, 44, states, countries);
		var impProvince4 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader4, 45, states, countries);
		var impProvince5 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader5, 46, states, countries);
		var impProvince6 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader6, 47, states, countries);

		var province1 = new Province(1);
		var province2 = new Province(2);
		var province3 = new Province(3);
		var province4 = new Province(4);
		var province5 = new Province(5);
		var province6 = new Province(6);

		var ck3Religions = new ReligionCollection();
		var landedTitles = new Title.LandedTitles();
		var ck3RegionMapper = new CK3RegionMapper();
		var cultureMapper = new CultureMapper(irRegionMapper, ck3RegionMapper);
		var religionMapper = new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper);
		var config = new Configuration();

		province1.InitializeFromImperator(impProvince, landedTitles, cultureMapper, religionMapper, config);
		province2.InitializeFromImperator(impProvince2, landedTitles, cultureMapper, religionMapper, config);
		province3.InitializeFromImperator(impProvince3, landedTitles, cultureMapper, religionMapper, config);
		province4.InitializeFromImperator(impProvince4, landedTitles, cultureMapper, religionMapper, config);
		province5.InitializeFromImperator(impProvince5, landedTitles, cultureMapper, religionMapper, config);
		province6.InitializeFromImperator(impProvince6, landedTitles, cultureMapper, religionMapper, config);

		Assert.Equal("city_holding", province1.GetHoldingType(ck3BookmarkDate));
		Assert.Equal("castle_holding", province2.GetHoldingType(ck3BookmarkDate));
		Assert.Equal("city_holding", province3.GetHoldingType(ck3BookmarkDate));
		Assert.Equal("church_holding", province4.GetHoldingType(ck3BookmarkDate));
		Assert.Equal("castle_holding", province5.GetHoldingType(ck3BookmarkDate));
		Assert.Equal("none", province6.GetHoldingType(ck3BookmarkDate));
	}
}