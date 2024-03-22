using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.Region;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Jobs;

public class GovernorshipTests {
	private readonly CountryCollection countryCollection = new();
	private readonly ImperatorRegionMapper irRegionMapper;
	private static readonly AreaCollection Areas = new();

	public GovernorshipTests() {
		countryCollection.Add(new Country(589));
		
		var areas = new AreaCollection();
		const string imperatorRoot = "TestFiles/Imperator/root";
		ModFilesystem irModFS = new(imperatorRoot, Array.Empty<Mod>());
		var irMapData = new MapData(irModFS);
		irRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		
		var region = new ImperatorRegion("galatia_region", new BufferedReader(string.Empty), Areas, new ColorFactory());
		irRegionMapper.Regions.Add(region);
	}
	
	[Fact]
	public void FieldsCanBeSet() {
		var reader = new BufferedReader(
			"who=589\n" +
			"character=25212\n" +
			"start_date=450.10.1\n" +
			"governorship = \"galatia_region\""
		);
		var governorship = new Governorship(reader, countryCollection, irRegionMapper);
		Assert.Equal((ulong)589, governorship.Country.Id);
		Assert.Equal((ulong)25212, governorship.CharacterId);
		Assert.Equal(new Date(450, 10, 1, AUC: true), governorship.StartDate);
		Assert.Equal("galatia_region", governorship.Region.Id);
	}

	[Fact]
	public void FormatExceptionIsThrownWhenCountryIdIsMissing() {
		var reader = new BufferedReader(
			"character=25212\n" +
			"start_date=450.10.1\n" +
			"governorship = \"galatia_region\""
		);
		Assert.Throws<FormatException>(() => new Governorship(reader, countryCollection, irRegionMapper));
	}

	[Fact]
	public void FormatExceptionIsThrownWhenRegionIdIsMissing() {
		var reader = new BufferedReader(
			"who=589\n" +
			"character=25212\n" +
			"start_date=450.10.1\n"
		);
		Assert.Throws<FormatException>(() => new Governorship(reader, countryCollection, irRegionMapper));
	}
}