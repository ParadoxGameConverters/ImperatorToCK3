using commonItems;
using ImperatorToCK3.CK3.Dynasties;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Dynasties;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class HouseTests {
	[Fact]
	public void HouseIsCorrectlyLoaded() {
		var houseReader = new BufferedReader("""
		     = {
		        prefix = "dynnp_ua"
		        name = "dynn_Wessex" # (100072)
		        motto = dynn_Wessex_motto
		        dynasty = 1047006
		        forced_coa_religiongroup = "zoroastrian_group"
		     }
		""");
		var house = new House("test_house", houseReader);
		
		Assert.Equal("test_house", house.Id);
		Assert.Equal("dynnp_ua", house.Prefix);
		Assert.Equal("dynn_Wessex", house.Name);
		Assert.Equal("1047006", house.DynastyId);
		Assert.Equal("dynn_Wessex_motto", house.Motto);
		Assert.Equal("zoroastrian_group", house.ForcedCoaReligionGroup);
	}
}