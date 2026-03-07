using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using AwesomeAssertions;
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
		var faithData = new FaithData {HolySiteIds = ["rome", "constantinople", "antioch"]};
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
			HolySiteIds = ["rome", "constantinople", "antioch"]
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

		var faithData = new FaithData {HolySiteIds = ["rome", "constantinople", "antioch"]};
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

	[Fact]
	public void FixupAddsReformedIconForUnreformedFaiths() {
		// Faith has unreformed_faith_doctrine and an icon, but no reformed_icon.
		var faithData = new FaithData {
			Attributes = new List<KeyValuePair<string, StringOfItem>> {
				new("icon", new StringOfItem("celtic_pagan"))
			}
		};
		faithData.DoctrineIds.Add("unreformed_faith_doctrine");
		var faith = new Faith("celtic_pagan", faithData, testReligion);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().Contain("reformed_icon = celtic_pagan");
	}

	[Fact]
	public void TooManyDoctrinesInCategoryAreTrimmedKeepingLastPicks() {
		// Arrange doctrine category with 2 picks allowed and 3 possible doctrines.
		var categoryReader = new BufferedReader(
			"number_of_picks = 2\n" +
			"doc_a1 = {}\n" +
			"doc_a2 = {}\n" +
			"doc_a3 = {}\n"
		);
		testReligion.ReligionCollection.DoctrineCategories.AddOrReplace(new DoctrineCategory("catA", categoryReader));

		var output = new StringWriter();
		Console.SetOut(output);

		// Faith initially has 3 doctrines from the same category (exceeds picks).
		var faithData = new FaithData();
		faithData.DoctrineIds.Add("doc_a1");
		faithData.DoctrineIds.Add("doc_a2");
		faithData.DoctrineIds.Add("doc_a3");
		var faith = new Faith("too_many_doctrines", faithData, testReligion);

		// Should keep only the last 2 among these and drop the first.
		Assert.Contains("doc_a2", faith.DoctrineIds);
		Assert.Contains("doc_a3", faith.DoctrineIds);
		Assert.DoesNotContain("doc_a1", faith.DoctrineIds);

		// Warning is logged.
		output.ToString().Should().Contain("Faith too_many_doctrines has too many doctrines in category catA");
	}

	[Fact]
	public void GetDoctrineIdsForDoctrineCategory_PrefersFaithOverReligion() {
		// Arrange category with two doctrines.
		var categoryReader = new BufferedReader(
			"doc_b1 = {}\n" +
			"doc_b2 = {}\n"
		);
		testReligion.ReligionCollection.DoctrineCategories.AddOrReplace(new DoctrineCategory("catB", categoryReader));

		// Religion has doc_b2, faith has doc_b1.
		testReligion.DoctrineIds.Clear();
		testReligion.DoctrineIds.Add("doc_b2");

		var faithData = new FaithData();
		faithData.DoctrineIds.Add("doc_b1");
		var faith = new Faith("prefers_faith", faithData, testReligion);

		var picks = faith.GetDoctrineIdsForDoctrineCategoryId("catB");
		picks.Should().Equal("doc_b1");
	}

	[Fact]
	public void GetDoctrineIdsForDoctrineCategory_FallsBackToReligion() {
		// Arrange category with two doctrines.
		var categoryReader = new BufferedReader(
			"doc_b1 = {}\n" +
			"doc_b2 = {}\n"
		);
		testReligion.ReligionCollection.DoctrineCategories.AddOrReplace(new DoctrineCategory("catB_fallback", categoryReader));

		// Religion has doc_b2, faith has none from this category.
		testReligion.DoctrineIds.Clear();
		testReligion.DoctrineIds.Add("doc_b2");

		var faithData = new FaithData();
		faithData.DoctrineIds.Add("unrelated_doctrine");
		var faith = new Faith("fallback_faith", faithData, testReligion);

		var picks = faith.GetDoctrineIdsForDoctrineCategoryId("catB_fallback");
		picks.Should().Equal("doc_b2");
	}

	[Fact]
	public void GetDoctrineIdsForDoctrineCategory_UnknownCategoryReturnsEmpty() {
		var faithData = new FaithData();
		faithData.DoctrineIds.Add("anything");
		var faith = new Faith("unknown_cat", faithData, testReligion);

		var picks = faith.GetDoctrineIdsForDoctrineCategoryId("no_such_category");
		Assert.Empty(picks);
	}

	[Fact]
	public void HasDoctrine_TrueWhenFaithHasDoctrine() {
		var categoryReader = new BufferedReader(
			"doc_c1 = {}\n" +
			"doc_c2 = {}\n"
		);
		testReligion.ReligionCollection.DoctrineCategories.AddOrReplace(new DoctrineCategory("catC", categoryReader));

		var faithData = new FaithData();
		faithData.DoctrineIds.Add("doc_c1");
		var faith = new Faith("has_doctrine", faithData, testReligion);

		Assert.True(faith.HasDoctrine("doc_c1"));
		Assert.False(faith.HasDoctrine("doc_c2"));
	}

	[Fact]
	public void HasDoctrine_FallsBackToReligionWhenFaithHasNoneInCategory() {
		var categoryReader = new BufferedReader(
			"doc_c1 = {}\n" +
			"doc_c2 = {}\n"
		);
		testReligion.ReligionCollection.DoctrineCategories.AddOrReplace(new DoctrineCategory("catC_fallback", categoryReader));

		testReligion.DoctrineIds.Clear();
		testReligion.DoctrineIds.Add("doc_c2");

		var faithData = new FaithData();
		faithData.DoctrineIds.Add("unrelated");
		var faith = new Faith("fallback_has_doctrine", faithData, testReligion);

		Assert.True(faith.HasDoctrine("doc_c2"));
	}

	[Fact]
	public void HasDoctrine_ReturnsFalseForUnknownDoctrine() {
		var faithData = new FaithData();
		faithData.DoctrineIds.Add("some");
		var faith = new Faith("unknown_doctrine", faithData, testReligion);

		Assert.False(faith.HasDoctrine("does_not_exist_anywhere"));
	}
}