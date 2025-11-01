using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Mappers.TagTitle;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle;

[Collection("Sequential")]
public class RankMappingTests {
	[Fact]
	public void MappingCanContainIRGovernmentType() {
		var reader = new BufferedReader("ir=local_power ir_government_type=tribal ck3=d");
		var rankMapping = new RankMapping(reader);
		
		// Should not match for a monarchy or a republic.
		Assert.Null(rankMapping.Match("local_power", territoriesCount: 0, GovernmentType.monarchy));
		Assert.Null(rankMapping.Match("local_power", territoriesCount: 0, GovernmentType.republic));
		
		// Should match for a tribal country.
		Assert.Equal(TitleRank.duchy, rankMapping.Match("local_power", territoriesCount: 0, GovernmentType.tribal));
	}
}