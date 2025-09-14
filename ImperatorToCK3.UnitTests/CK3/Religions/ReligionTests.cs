using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using AwesomeAssertions;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using System;
using System.IO;
using System.Linq;
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
			"pagan_roots = yes",
			"doctrine=doctrine_no_head",
			"doctrine=doctrine_gender_male_dominated"
		);
	}

	[Fact]
	public void DoctrinesInSameCategoryAreLimitedToNumberOfPicks() {
		// Arrange: Create a doctrine category with only 2 picks allowed
		var categoryReader = new BufferedReader(
			"number_of_picks = 2\n" +
			"doctrine_head_1 = {}\n" +
			"doctrine_head_2 = {}\n" +
			"doctrine_head_3 = {}\n" +
			"doctrine_head_4 = {}\n"
		);
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.DoctrineCategories.AddOrReplace(new DoctrineCategory("head_category", categoryReader));

		// Act: Create a religion with 4 doctrines from the same category (exceeds limit)
		var reader = new BufferedReader(@"{
			doctrine = doctrine_head_1
			doctrine = doctrine_head_2
			doctrine = doctrine_head_3
			doctrine = doctrine_head_4
		}");
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		// Assert: Should keep only the last 2 doctrines (doctrine_head_3 and doctrine_head_4)
		Assert.Equal(2, religion.DoctrineIds.Count);
		Assert.Contains("doctrine_head_3", religion.DoctrineIds);
		Assert.Contains("doctrine_head_4", religion.DoctrineIds);
		Assert.DoesNotContain("doctrine_head_1", religion.DoctrineIds);
		Assert.DoesNotContain("doctrine_head_2", religion.DoctrineIds);
	}

	[Fact]
	public void InvalidColorInFaithLogsWarning() {
		// Arrange: Create a religion with a faith that has an invalid color format.
		var reader = new BufferedReader(@"{
			faiths = {
				test_faith = {
					color = hex #345345345
				}
			}
		}");
		var religions = new ReligionCollection(new Title.LandedTitles());

		// Act: Capture log output during religion creation.
		var logWriter = new StringWriter();
		Console.SetOut(logWriter);
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		// Assert: Faith should still be created despite the invalid color.
		Assert.Single(religion.Faiths);
		var faith = religion.Faiths.First(f => f.Id == "test_faith");
		Assert.Null(faith.Color);

		// Assert: Warning should be logged.
		var logOutput = logWriter.ToString();
		logOutput.Should().Contain("Found invalid color");
	}

	[Fact]
	public void ReligiousHeadIsSetWhenNotNone() {
		// Arrange: Create a religion with a faith that has a religious head title
		var reader = new BufferedReader(@"{
			faiths = {
				test_faith = {
					religious_head = k_papal_state
				}
			}
		}");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		// Assert: Faith should be created with the religious head title
		Assert.Single(religion.Faiths);
		var faith = religion.Faiths.First(f => f.Id == "test_faith");
		Assert.Equal("k_papal_state", faith.ReligiousHeadTitleId);
	}

	[Fact]
	public void ReligiousHeadIsNotSetWhenNone() {
		// Arrange: Create a religion with a faith that has religious_head = none
		var reader = new BufferedReader(@"{
			faiths = {
				test_faith = {
					religious_head = none
				}
			}
		}");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		// Assert: Faith should be created but ReligiousHeadTitleId should be null
		Assert.Single(religion.Faiths);
		var faith = religion.Faiths.First(f => f.Id == "test_faith");
		Assert.Null(faith.ReligiousHeadTitleId);
	}

	[Fact]
	public void ReligiousHeadIsSerializedWhenSet() {
		// Arrange: Create a religion with a faith that has a religious head title
		var reader = new BufferedReader(@"{
			faiths = {
				test_faith = {
					religious_head = k_papal_state
				}
			}
		}");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		// Act: Serialize the religion
		var religionStr = PDXSerializer.Serialize(religion);

		// Assert: The religious head should be serialized in the faith
		religionStr.Should().Contain("religious_head=k_papal_state");
	}

	[Fact]
	public void FaithAttributesAreParsedAndStored() {
		// Arrange: Create a religion with a faith that has custom attributes (not specially handled keywords)
		var reader = new BufferedReader(@"{
			faiths = {
				test_faith = {
					icon = custom_faith_icon
					localization = test_faith_loc
					custom_modifier = some_value
					special_mechanic = yes
				}
			}
		}");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		// Assert: Faith should be created and attributes should be accessible through serialization
		Assert.Single(religion.Faiths);
		var faith = religion.Faiths.First(f => f.Id == "test_faith");

		// Serialize to verify attributes are stored and output correctly
		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().ContainAll(
			"icon = custom_faith_icon",
			"localization = test_faith_loc", 
			"custom_modifier = some_value",
			"special_mechanic = yes"
		);
	}
}