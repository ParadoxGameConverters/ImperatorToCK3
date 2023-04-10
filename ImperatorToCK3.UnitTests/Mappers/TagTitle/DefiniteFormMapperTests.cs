using ImperatorToCK3.Mappers.TagTitle;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle;

public class DefiniteFormMapperTests {
	[Fact]
	public void MapperReturnsTrueForMatchingName() {
		var mapper = new DefiniteFormMapper("TestFiles/configurables/definite_form_names.txt");
		Assert.True(mapper.IsDefiniteForm("PRY_DYN"));
	}
	[Fact]
	public void MapperReturnsFalseForNonMatchingName() {
		var mapper = new DefiniteFormMapper("TestFiles/configurables/definite_form_names.txt");
		Assert.False(mapper.IsDefiniteForm("Atlantis"));
	}
}