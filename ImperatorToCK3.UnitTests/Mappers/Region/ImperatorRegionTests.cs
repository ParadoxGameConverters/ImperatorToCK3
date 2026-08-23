using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.Region;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region;

public class ImperatorRegionTests {
	private readonly ProvinceCollection provinces = [];
	private static readonly ColorFactory colorFactory = new();
	private static readonly MapData irMapData = new(new ModFilesystem("TestFiles/RegionTests", []));

	public ImperatorRegionTests() {
		provinces.LoadProvinces(new BufferedReader(
			"1={} 2={} 3={} 4={} 5={} 6={} 7={} 8={} 9={} 69={}")
		, new StateCollection(), new CountryCollection(), irMapData);
	}

	[Fact]
	public void BlankRegionLoadsWithNoAreas() {
		var reader = new BufferedReader(string.Empty);
		var region = new ImperatorRegion("region1", reader, new AreaCollection(), colorFactory);

		Assert.Empty(region.Areas);
	}

	[Fact]
	public void RegionCanBeLinkedToArea() {
		var reader2 = new BufferedReader("{ provinces  = { 3 6 2 }}");
		var area = new Area("test1", reader2, provinces);
		var areas = new IdObjectCollection<string, Area> { area };
		
		var reader1 = new BufferedReader("areas = { test1 }");
		var region = new ImperatorRegion("region1", reader1, areas, colorFactory);

		Assert.NotNull(region.Areas["test1"]);
	}

	[Fact]
	public void MultipleAreasCanBeLoaded() {var emptyReader = new BufferedReader(string.Empty);
		var area1 = new Area("test1", emptyReader, provinces);
		var area2 = new Area("test2", emptyReader, provinces);
		var area3 = new Area("test3", emptyReader, provinces);
		var areas = new IdObjectCollection<string, Area> { area1, area2, area3 };

		var reader = new BufferedReader("areas = { test1 test2 test3 }");
		var region = new ImperatorRegion("region1", reader, areas, colorFactory);
		
		Assert.Collection(region.Areas,
			item => Assert.Equal("test1", item.Id),
			item => Assert.Equal("test2", item.Id),
			item => Assert.Equal("test3", item.Id)
		);
	}

	[Fact]
	public void LinkedRegionCanLocateProvince() {
		var reader2 = new BufferedReader("{ provinces = { 3 6 2 }}");
		var area = new Area("area1", reader2, provinces);
		var areas = new IdObjectCollection<string, Area> { area };
		
		var reader1 = new BufferedReader("{ areas={area1} }");
		var region = new ImperatorRegion("region1", reader1, areas, colorFactory);
		
		Assert.True(region.ContainsProvince(6));
	}

	[Fact]
	public void LinkedRegionWillFailForProvinceMismatch() {
		var reader2 = new BufferedReader("{ provinces  = { 3 6 2 }}");
		var area = new Area("area1", reader2, provinces);
		var areas = new IdObjectCollection<string, Area> { area };

		var reader1 = new BufferedReader("{ areas={area1} }");
		var region = new ImperatorRegion("region1", reader1, areas, colorFactory);

		Assert.False(region.ContainsProvince(7));
	}
}