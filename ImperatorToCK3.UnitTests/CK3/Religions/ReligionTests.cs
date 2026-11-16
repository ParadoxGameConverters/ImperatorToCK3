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
using System.Collections.Generic;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

[Collection("Sequential")] // Tests in this class manipulate global log output, so they must run sequentially to avoid interference.
public class ReligionTests {
	[Fact]
	public void LegacyNestedFaithsAreLoadedAndReligionIsSerializedInNewFormat() {
		var reader = new BufferedReader("{ faiths={ orthodox={} catholic={} } }");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("christianity", reader, religions, new ColorFactory());

		Assert.Collection(religion.Faiths,
			faith => Assert.Equal("orthodox", faith.Id),
			faith => Assert.Equal("catholic", faith.Id));

		// In CK3 1.20+ faiths are not nested inside religions.
		var religionStrWithoutWhitespace = Regex.Replace(PDXSerializer.Serialize(religion), @"\s", "");
		religionStrWithoutWhitespace.Should().Contain("religion_details={}");
		religionStrWithoutWhitespace.Should().NotContain("faiths=");
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
			"doctrine_types = { doctrine_head_1 doctrine_head_2 doctrine_head_3 doctrine_head_4 }"
		);
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.DoctrineGroups.AddOrReplace(new DoctrineGroup("head_category", categoryReader));

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
		Assert.Contains("doctrine_head_3", religion.DoctrineIds as IReadOnlyCollection<string>);
		Assert.Contains("doctrine_head_4", religion.DoctrineIds as IReadOnlyCollection<string>);
		Assert.DoesNotContain("doctrine_head_1", religion.DoctrineIds as IReadOnlyCollection<string>);
		Assert.DoesNotContain("doctrine_head_2", religion.DoctrineIds as IReadOnlyCollection<string>);
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
		var faith = religion.Faiths.First(f => f.Id == "test_faith");

		// Act: Serialize the faith
		var faithStr = PDXSerializer.Serialize(faith);

		// Assert: The religious head should be serialized in faith_details
		faithStr.Should().Contain("religious_head = k_papal_state");
	}
	
	[Fact]
	public void DuplicatesInReligionLocalizationAreRemoved() {
		var reader = new BufferedReader("""
        {
            localization = {
                ReligiousHeadName = first_title
                ReligiousHeadAdj = first_adj
                ReligiousHeadName = second_title
                ReligiousHeadAdj = second_adj
            }
        }
        """);
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());
		string serializedReligion = PDXSerializer.Serialize(religion);
		serializedReligion.Should().Contain("ReligiousHeadName=second_title");
		serializedReligion.Should().Contain("ReligiousHeadAdj=second_adj");
		serializedReligion.Should().NotContain("first_title");
		serializedReligion.Should().NotContain("first_adj");
	}

	[Fact]
	public void DuplicatesInFaithLocalizationAreRemoved() {
		var reader = new BufferedReader(@"{
			faiths = {
				test_faith = {
					localization = {
						ReligiousHeadName = first_title
						ReligiousHeadAdj = first_adj
						ReligiousHeadName = second_title
						ReligiousHeadAdj = second_adj
					}
				}
			}
		}");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());
		var faith = religion.Faiths.First(f => f.Id == "test_faith");
		string serializedFaith = PDXSerializer.Serialize(faith);
		serializedFaith.Should().Contain("ReligiousHeadName=second_title");
		serializedFaith.Should().Contain("ReligiousHeadAdj=second_adj");
		serializedFaith.Should().NotContain("first_title");
		serializedFaith.Should().NotContain("first_adj");
	}

	[Fact]
	public void FaithAttributesAreParsedAndStored() {		// Arrange: Create a religion with a faith that has custom attributes (not specially handled keywords)
		var reader = new BufferedReader(@"{
			faiths = {
				test_faith = {
					icon = custom_faith_icon
					custom_modifier = some_value
					special_mechanic = yes
					localization = {}
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
			"custom_modifier = some_value",
			"special_mechanic = yes",
			"localization = {"
		);
	}

	[Fact]
	public void ReligionDetailsAreParsedAndSerialized() {
		var reader = new BufferedReader("""
			{
				religion_details = {
					family = rf_pagan
					piety_icon_group = "pagan"
					theocracy_government_type = theocracy_government
				}
				doctrine = doctrine_monotheist
			}
			""");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		Assert.Equal("rf_pagan", religion.FamilyId);

		var religionStr = PDXSerializer.Serialize(religion);
		religionStr.Should().ContainAll(
			"religion_details = {",
			"family = rf_pagan",
			"piety_icon_group = \"pagan\"",
			"theocracy_government_type = theocracy_government",
			"doctrine=doctrine_monotheist"
		);
	}

	[Fact]
	public void LegacyFamilyIsMovedIntoReligionDetails() {
		var reader = new BufferedReader("""
			{
				family = rf_pagan
				pagan_roots = yes
			}
			""");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		Assert.Equal("rf_pagan", religion.FamilyId);

		var religionStr = PDXSerializer.Serialize(religion);
		religionStr.Should().Contain("family = rf_pagan");
		// family should not be serialized twice
		Regex.Matches(religionStr, "family = rf_pagan").Count.Should().Be(1);
		religionStr.Should().Contain("pagan_roots = yes");
	}

	[Fact]
	public void SerializedReligionCanBeParsedBack() {
		var reader = new BufferedReader("""
			{
				religion_details = {
					family = rf_pagan
				}
				doctrine = doctrine_monotheist
				pagan_roots = yes
			}
			""");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		var serialized = religion.Serialize(indent: "", withBraces: true);
		var parsedReligion = new Religion("test_religion", new BufferedReader(serialized), religions, new ColorFactory());

		Assert.Equal("rf_pagan", parsedReligion.FamilyId);
		Assert.Equal(["doctrine_monotheist"], parsedReligion.DoctrineIds);
	}

	[Fact]
	public void Serialize_WithoutBraces_OmitsOuterBraces() {
		var reader = new BufferedReader("{ doctrine = doctrine_no_head }");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions, new ColorFactory());

		var withoutBraces = religion.Serialize(indent: "", withBraces: false);
		Assert.StartsWith("religion_details = {", withoutBraces.TrimStart());

		var withBraces = religion.Serialize(indent: "", withBraces: true);
		Assert.StartsWith("{", withBraces.TrimStart());
	}
}
