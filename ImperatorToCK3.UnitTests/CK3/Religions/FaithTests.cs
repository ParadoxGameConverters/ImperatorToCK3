using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using FluentAssertions;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class FaithTests {
	private readonly Religion testReligion;
	
	public FaithTests() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		testReligion = new Religion("test_religion", new BufferedReader("{}"), religions, new ColorFactory());
	}
	
	[Fact]
	public void HolySiteIdsAreLoadedAndSerialized() {
		var faithData = new FaithData {HolySiteIds = new List<string> {"rome", "constantinople", "antioch"}};
		var faith = new Faith("chalcedonian", faithData, testReligion);

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
	public void FaithColorIsSerialized() {
		var faithData = new FaithData {Color = new Color(0.15, 1, 0.7)};
		var faith = new Faith("celtic_pagan", faithData, testReligion);

		Assert.Equal(new Color(0.15, 1, 0.7), faith.Color);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().Contain(
			"color=rgb { 178 160 0 }"
		);
	}

	[Fact]
	public void FaithAttributesAreSerialized() {
		var faithData = new FaithData {
			Attributes = new List<KeyValuePair<string, StringOfItem>> {
				new("icon", new StringOfItem("celtic_pagan")),
				new("doctrine", new StringOfItem("tenet_esotericism")),
				new ("doctrine", new StringOfItem("tenet_human_sacrifice # should not replace the line above"))
			}
		};
		var faith = new Faith("celtic_pagan", faithData, testReligion);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().ContainAll(
			"icon = celtic_pagan",
			"doctrine = tenet_esotericism",
			"doctrine = tenet_human_sacrifice"
		);
	}

	[Fact]
	public void ReligiousHeadTitleIdIsCorrectlySerialized() {
		var faithData = new FaithData {ReligiousHeadTitleId = "d_papacy"};
		var faith = new Faith("atheism", faithData, testReligion);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().Contain("religious_head=d_papacy");
	}

	[Fact]
	public void HolySiteIdCanBeReplaced() {
		var faithData = new FaithData {
			HolySiteIds = new List<string> {"rome", "constantinople", "antioch"}
		};
		var faith = new Faith("orthodox", faithData, testReligion);
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "antioch");

		faith.ReplaceHolySiteId("antioch", "jerusalem");
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "jerusalem");
	}

	[Fact]
	public void ReplacingMissingHolySiteIdDoesNotChangeHolySites() {
		var output = new StringWriter();
		Console.SetOut(output);

		var faithData = new FaithData {HolySiteIds = new List<string> {"rome", "constantinople", "antioch"}};
		var faith = new Faith("orthodox", faithData, testReligion);
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "antioch");

		faith.ReplaceHolySiteId("washington", "jerusalem");
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "antioch");
		Assert.Contains("washington does not belong to holy sites of faith orthodox and cannot be replaced!", output.ToString());
	}
	
	[Fact]
	public void ReligiousHeadTitleIdIsCorrectlyReturned() {
		var faithData = new FaithData{ReligiousHeadTitleId = "e_orthodox_head"};
		var orthodox = new Faith("orthodox", faithData, testReligion);
		Assert.Equal("e_orthodox_head", orthodox.ReligiousHeadTitleId);
		
		faithData.ReligiousHeadTitleId = "e_catholic_head";
		var catholic = new Faith("catholic", faithData, testReligion);
		Assert.Equal("e_catholic_head", catholic.ReligiousHeadTitleId);

		faithData.ReligiousHeadTitleId = "e_coptic_head";
		var coptic = new Faith("coptic", faithData, testReligion);
		Assert.Equal("e_coptic_head", coptic.ReligiousHeadTitleId);

		faithData.ReligiousHeadTitleId = null;
		var atheism = new Faith("atheism", faithData, testReligion);
		Assert.Null(atheism.ReligiousHeadTitleId);
	}
}