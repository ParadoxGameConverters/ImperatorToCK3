using commonItems;
using commonItems.Serialization;
using FluentAssertions;
using ImperatorToCK3.CK3.Religions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions; 

public class ReligionTests {
	[Fact]
	public void FaithsAreLoaded() {
		var reader = new BufferedReader("{ faiths={ orthodox={} catholic={} } }");
		var religion = new Religion("christianity", reader);
		
		Assert.Collection(religion.Faiths,
			faith=>Assert.Equal("orthodox", faith.Id),
			faith=>Assert.Equal("catholic", faith.Id));
	}

	[Fact]
	public void ReligionAttributesAreReadAndSerialized() {
		var reader = new BufferedReader(@"{
			pagan_roots = yes
			doctrine = doctrine_no_head
			doctrine = doctrine_gender_male_dominated # should not replace the line above
		}");
		var religion = new Religion("celtic_religion", reader);

		var religionStr = PDXSerializer.Serialize(religion);
		religionStr.Should().ContainAll(
			"pagan_roots=yes",
			"doctrine=doctrine_no_head",
			"doctrine=doctrine_gender_male_dominated"
		);
	}
}