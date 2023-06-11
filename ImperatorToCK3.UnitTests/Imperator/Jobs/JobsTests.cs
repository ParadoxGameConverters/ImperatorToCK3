using commonItems;
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
	private readonly CountryCollection countryCollection = new();
	private readonly ImperatorRegionMapper irRegionMapper;

	public JobsTests() {
		countryCollection.Add(new Country(1));
		countryCollection.Add(new Country(2));
		
		var areas = new AreaCollection();
		irRegionMapper = new ImperatorRegionMapper(areas);
		
		irRegionMapper.Regions.Add(new ImperatorRegion("galatia_region", new BufferedReader(string.Empty)));
	}
	[Fact]
	public void GovernorshipsDefaultToEmpty() {
		var jobs = new ImperatorToCK3.Imperator.Jobs.Jobs();
		Assert.Empty(jobs.Governorships);
	}
	[Fact]
	public void GovernorshipsCanBeRead() {
		var reader = new BufferedReader(
			"province_job={who=1} province_job={who=2}"
		);
		var jobs = new ImperatorToCK3.Imperator.Jobs.Jobs(reader, countryCollection, irRegionMapper);
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
		_ = new ImperatorToCK3.Imperator.Jobs.Jobs(reader, countryCollection, irRegionMapper);

		Assert.Contains("Ignored Jobs tokens: useless_job", output.ToString());
	}
}