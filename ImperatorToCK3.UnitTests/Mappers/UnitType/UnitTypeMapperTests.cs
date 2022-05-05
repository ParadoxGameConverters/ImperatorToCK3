using ImperatorToCK3.Mappers.UnitType;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.UnitType;

public class UnitTypeMapperTests {
	[Theory]
	[InlineData("wrong_type", null)]
	[InlineData("archers", "bowmen")]
	[InlineData("cataphracts", "cavalry")]
	[InlineData("shit_on_a_stick", null)]
	public void MapperReturnsCorrectValues(string imperatorType, string? ck3Type) {
		const string mappingsFilePath = "TestFiles/UnitTypeMapperTests/mappings.txt";
		var mapper = new UnitTypeMapper(mappingsFilePath);
		Assert.Equal(ck3Type, mapper.Match(imperatorType));
	}
}