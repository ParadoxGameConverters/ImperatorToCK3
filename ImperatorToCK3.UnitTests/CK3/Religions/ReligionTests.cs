using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using FluentAssertions;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using System.Text.RegularExpressions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

public class ReligionTests {
	[Fact]
	public void FaithsAreLoadedAndSerialized() {
		var reader = new BufferedReader("{ faiths={ orthodox={} catholic={} } }");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("christianity", reader, religions, new ColorFactory());

		Assert.Collection(religion.Faiths,
			faith => Assert.Equal("orthodox", faith.Id),
			faith => Assert.Equal("catholic", faith.Id));

		var religionStrWithoutWhitespace = Regex.Replace(PDXSerializer.Serialize(religion), @"\s", "");
		religionStrWithoutWhitespace.Should().ContainAll("orthodox={}", "catholic={}");
	}

	[Fact]
	public void ReligionAttributesAreReadAndSerialized() {
		var reader = new BufferedReader(@"{
			pagan_roots = yes
			doctrine = doctrine_no_head
			doctrine = doctrine_gender_male_dominated # should not replace the line above
		}");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("celtic_religion", reader, religions, new ColorFactory());

		var religionStr = PDXSerializer.Serialize(religion);
		religionStr.Should().ContainAll(
			"pagan_roots=yes",
			"doctrine=doctrine_no_head",
			"doctrine=doctrine_gender_male_dominated"
		);
	}
}