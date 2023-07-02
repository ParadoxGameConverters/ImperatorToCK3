using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using FluentAssertions;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

public class FaithTests {
	private readonly Religion testReligion;
	
	public FaithTests() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		testReligion = new Religion("test_religion", new BufferedReader("{}"), religions, new ColorFactory());
	}
	
	[Fact]
	public void HolySiteIdsAreLoadedAndSerialized() {
		var reader = new BufferedReader("{ holy_site=rome holy_site=constantinople holy_site=antioch }");
		var faith = new Faith("chalcedonian", reader, testReligion, new ColorFactory());

		Assert.Collection(faith.HolySiteIds,
			site => Assert.Equal("rome", site),
			site => Assert.Equal("constantinople", site),
			site => Assert.Equal("antioch", site));

		var faithStrWithoutWhitespace = Regex.Replace(PDXSerializer.Serialize(faith), @"\s", "");
		faithStrWithoutWhitespace.Should().ContainAll(
			"holy_site=rome",
			"holy_site=constantinople",
			"holy_site=antioch");
	}

	[Fact]
	public void FaithColorIsReadAndSerialized() {
		var reader = new BufferedReader("{ color = hsv { 0.15  1  0.7 } }");
		var faith = new Faith("celtic_pagan", reader, testReligion, new ColorFactory());

		Assert.Equal(new Color(0.15, 1, 0.7), faith.Color);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().Contain(
			"color=rgb { 178 160 0 }"
		);
	}

	[Fact]
	public void FaithAttributesAreReadAndSerialized() {
		var reader = new BufferedReader(@"{
			icon = celtic_pagan
			doctrine = tenet_esotericism
			doctrine = tenet_human_sacrifice # should not replace the line above
		}");
		var faith = new Faith("celtic_pagan", reader, testReligion, new ColorFactory());

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().ContainAll(
			"icon=celtic_pagan",
			"doctrine=tenet_esotericism",
			"doctrine=tenet_human_sacrifice"
		);
	}

	[Fact]
	public void ReligiousHeadTitleIdIsCorrectlySerialized() {
		var reader = new BufferedReader("religious_head      = d_papacy"); // intentional unformatted whitespace
		var faith = new Faith("atheism", reader, testReligion, new ColorFactory());

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().Contain("religious_head=d_papacy");
	}

	[Fact]
	public void HolySiteIdCanBeReplaced() {
		var reader = new BufferedReader("{ holy_site=rome holy_site=constantinople holy_site=antioch }");
		var faith = new Faith("orthodox", reader, testReligion, new ColorFactory());
		Assert.False(faith.ModifiedByConverter);

		faith.ReplaceHolySiteId("antioch", "jerusalem");
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "jerusalem");
		Assert.True(faith.ModifiedByConverter);
	}

	[Fact]
	public void ReplacingMissingHolySiteIdDoesNotChangeHolySites() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader("{ holy_site=rome holy_site=constantinople holy_site=antioch }");
		var faith = new Faith("orthodox", reader, testReligion, new ColorFactory());
		Assert.False(faith.ModifiedByConverter);

		faith.ReplaceHolySiteId("washington", "jerusalem");
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "antioch");
		Assert.False(faith.ModifiedByConverter);
		Assert.Contains("washington does not belong to holy sites of faith orthodox and cannot be replaced!", output.ToString());
	}
	
	[Fact]
	public void ReligiousHeadTitleIdIsCorrectlyRead() {
		var orthodoxHeadReader = new BufferedReader("{ religious_head = e_orthodox_head }");
		var orthodox = new Faith("orthodox", orthodoxHeadReader, testReligion, new ColorFactory());
		Assert.Equal("e_orthodox_head", orthodox.ReligiousHeadTitleId);
		
		var catholicHeadReader = new BufferedReader("{ religious_head = e_catholic_head }");
		var catholic = new Faith("catholic", catholicHeadReader, testReligion, new ColorFactory());
		Assert.Equal("e_catholic_head", catholic.ReligiousHeadTitleId);
		
		var copticHeadReader = new BufferedReader("{ religious_head = e_coptic_head }");
		var coptic = new Faith("coptic", copticHeadReader, testReligion, new ColorFactory());
		Assert.Equal("e_coptic_head", coptic.ReligiousHeadTitleId);
		
		var noHeadFaithReader = new BufferedReader("{}");
		var atheism = new Faith("atheism", noHeadFaithReader, testReligion, new ColorFactory());
		Assert.Null(atheism.ReligiousHeadTitleId);
	}
}