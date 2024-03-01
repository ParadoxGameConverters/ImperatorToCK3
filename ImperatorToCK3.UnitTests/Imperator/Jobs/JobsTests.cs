using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Region;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Jobs;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class JobsTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private readonly CountryCollection countryCollection = new();
	private readonly ImperatorRegionMapper irRegionMapper;
	private static readonly AreaCollection Areas = new();

	public JobsTests() {
		countryCollection.Add(new Country(1));
		countryCollection.Add(new Country(2));
		
		var areas = new AreaCollection();
		irRegionMapper = new ImperatorRegionMapper(areas, irMapData);

		var region = new ImperatorRegion("galatia_region", new BufferedReader(string.Empty), Areas, new ColorFactory());
		irRegionMapper.Regions.Add(region);
	}
	[Fact]
	public void GovernorshipsDefaultToEmpty() {
		var jobs = new ImperatorToCK3.Imperator.Jobs.JobsDB();
		Assert.Empty(jobs.Governorships);
	}
	[Fact]
	public void GovernorshipsCanBeRead() {
		var reader = new BufferedReader(
			"province_job={who=1 governorship=galatia_region} province_job={who=2 governorship=galatia_region}"
		);
		var jobs = new ImperatorToCK3.Imperator.Jobs.JobsDB(reader, countryCollection, irRegionMapper);
		Assert.Collection(jobs.Governorships,
			item1 => Assert.Equal((ulong)1, item1.Country.Id),
			item2 => Assert.Equal((ulong)2, item2.Country.Id)
		);
	}
	[Fact]
	public void IgnoredTokensAreLogged() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader(
			"useless_job = {}"
		);
		_ = new ImperatorToCK3.Imperator.Jobs.JobsDB(reader, countryCollection, irRegionMapper);

		Assert.Contains("Ignored Jobs tokens: useless_job", output.ToString());
	}
}