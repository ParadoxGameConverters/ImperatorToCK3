using commonItems;
using ImperatorToCK3.Mappers.Province;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Province; 

public class ProvinceMappingsVersionTests {
	[Fact]
	public void MappingsDefaultToEmpty() {
		var reader = new BufferedReader(
			"= {}"
		);
		var theMappingVersion = new ProvinceMappingsVersion(reader);
		Assert.Empty(theMappingVersion.Mappings);
	}

	[Fact]
	public void MappingsCanBeLoaded() {
		var reader = new BufferedReader(
			"= {\n" +
			"	link = { ck3 = 1 imp = 1 }\n" +
			"	link = { ck3 = 2 imp = 2 }\n" +
			"}"
		);
		var theMappingVersion = new ProvinceMappingsVersion(reader);
		Assert.Equal(2, theMappingVersion.Mappings.Count);
	}
}