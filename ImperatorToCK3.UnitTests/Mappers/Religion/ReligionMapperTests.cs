using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Religion;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Religion; 

public class ReligionMapperTests {
	private const string CK3Root = "TestFiles/CK3/game";
	private readonly ModFilesystem ck3ModFs = new(CK3Root, new Mod[] { });
	
	[Fact]
	public void NonMatchGivesNull() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		var impRegionMapper = new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper();
		var ck3RegionMapper = new ImperatorToCK3.Mappers.Region.CK3RegionMapper();
			
		var reader = new BufferedReader("link = { ck3 = ck3Faith imp = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, impRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("nonMatchingReligion", 0, 0, new Configuration());
		Assert.Null(ck3FaithId);
	}

	[Fact]
	public void CK3FaithCanBeFound() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs);
		var impRegionMapper = new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper();
		var ck3RegionMapper = new ImperatorToCK3.Mappers.Region.CK3RegionMapper();
			
		var reader = new BufferedReader("link = { ck3 = ck3Faith imp = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, impRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("impReligion", 45, 456, new Configuration());
		Assert.Equal("ck3Faith", ck3FaithId);
	}

	[Fact]
	public void MultipleImperatorReligionsCanBeInARule() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs);
		var impRegionMapper = new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper();
		var ck3RegionMapper = new ImperatorToCK3.Mappers.Region.CK3RegionMapper();
			
		var reader = new BufferedReader(
			"link = { ck3 = ck3Faith imp = impReligion imp = impReligion2 }"
		);
		var mapper = new ReligionMapper(reader, ck3Religions, impRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("impReligion2", 45, 456, new Configuration());
		Assert.Equal("ck3Faith", ck3FaithId);
	}

	[Fact]
	public void CorrectRuleMatches() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs);
		var impRegionMapper = new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper();
		var ck3RegionMapper = new ImperatorToCK3.Mappers.Region.CK3RegionMapper();
			
		var reader = new BufferedReader(
			"link = { ck3 = ck3Faith imp = impReligion }" +
			"link = { ck3 = ck3Faith2 imp = impReligion2 }"
		);
		var mapper = new ReligionMapper(reader, ck3Religions, impRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("impReligion2", 45, 456, new Configuration());
		Assert.Equal("ck3Faith2", ck3FaithId);
	}
}