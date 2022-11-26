using ImperatorToCK3.Mappers.UnitType;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.UnitType;

public class UnitTypeMapperTests {
	private const string MappingsFilePath = "TestFiles/MapperTests/UnitTypeMapper/mappings.txt";
	
	[Theory]
	[InlineData("wrong_type", null)]
	[InlineData("archers", "bowmen")]
	[InlineData("cataphracts", "cavalry")]
	[InlineData("shit_on_a_stick", null)]
	public void MatchReturnsCorrectValues(string imperatorType, string? ck3Type) {
		var mapper = new UnitTypeMapper(MappingsFilePath);
		Assert.Equal(ck3Type, mapper.Match(imperatorType));
	}

	[Fact]
	public void GetMenPerCK3UnitTypeReturnsCorrectValues() {
		var mapper = new UnitTypeMapper(MappingsFilePath);

		var imperatorMenPerType = new Dictionary<string, int> {
			{"clibanarii", 1200}, {"cataphracts", 450}, // both mapped to cavalry
			{"archers", 200} // mapped to bowmen
		};
		var ck3MenPerType = mapper.GetMenPerCK3UnitType(imperatorMenPerType);
		Assert.Collection(ck3MenPerType,
			kvp => {
				Assert.Equal("cavalry", kvp.Key);
				Assert.Equal(1650, kvp.Value);
			},
			kvp => {
				Assert.Equal("bowmen", kvp.Key);
				Assert.Equal(200, kvp.Value);
			});
	}
}