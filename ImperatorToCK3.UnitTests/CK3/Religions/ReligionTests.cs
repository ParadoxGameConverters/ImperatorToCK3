using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using AwesomeAssertions;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using System;
using System.IO;
using System.Linq;
using Xunit;
using System.Collections.Generic;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

[Collection("Sequential")] // Tests in this class manipulate global log output, so they must run sequentially to avoid interference.
public class ReligionTests {
	[Fact]
	public void ReligionIsSerializedWithoutNestedFaiths() {
		var reader = new BufferedReader("{ pagan_roots = yes }");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions);

		// In CK3 1.20+ faiths are not nested inside religions.
		var religionStr = PDXSerializer.Serialize(religion);
		religionStr.Should().Contain("pagan_roots = yes");
		religionStr.Should().NotContain("faiths");
	}

	private static Faith ParseFaith(string faithId, string faithBody) {
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", new BufferedReader("{}"), religions);
		var faithData = new FaithDataParser(new ColorFactory()).Parse(new BufferedReader(faithBody));
		return new Faith(faithId, faithData, religion);
	}

	[Fact]
	public void ReligionAttributesAreReadAndSerialized() {
		var reader = new BufferedReader(@"{
			pagan_roots = yes
			doctrine = doctrine_no_head
			doctrine = doctrine_gender_male_dominated # should not replace the line above
		}");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("celtic_religion", reader, religions);

		var religionStr = PDXSerializer.Serialize(religion);
		religionStr.Should().ContainAll(
			"pagan_roots = yes",
			"doctrine=doctrine_no_head",
			"doctrine=doctrine_gender_male_dominated"
		);
	}

	[Fact]
	public void DoctrinesInSameGroupAreLimitedToOne() {
		// Arrange: Create a doctrine group with 4 possible doctrines.
		var group = new DoctrineGroup("head_group", new BufferedReader(""));
		group.AddDoctrineId("doctrine_head_1");
		group.AddDoctrineId("doctrine_head_2");
		group.AddDoctrineId("doctrine_head_3");
		group.AddDoctrineId("doctrine_head_4");
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.DoctrineGroups.AddOrReplace(group);

		// Act: Create a religion with 4 doctrines from the same group (only one can be active).
		var reader = new BufferedReader(@"{
			doctrine = doctrine_head_1
			doctrine = doctrine_head_2
			doctrine = doctrine_head_3
			doctrine = doctrine_head_4
		}");
		var religion = new Religion("test_religion", reader, religions);

		// Assert: Should keep only the last doctrine.
		Assert.Single(religion.DoctrineIds);
		Assert.Contains("doctrine_head_4", religion.DoctrineIds as IReadOnlyCollection<string>);
		Assert.DoesNotContain("doctrine_head_1", religion.DoctrineIds as IReadOnlyCollection<string>);
		Assert.DoesNotContain("doctrine_head_2", religion.DoctrineIds as IReadOnlyCollection<string>);
		Assert.DoesNotContain("doctrine_head_3", religion.DoctrineIds as IReadOnlyCollection<string>);
	}

	[Fact]
	public void InvalidColorInFaithLogsWarning() {
		// Arrange: Capture log output while parsing a faith with an invalid color format.
		var logWriter = new StringWriter();
		var originalOut = Console.Out;
		Console.SetOut(logWriter);
		try {
			var faithData = new FaithDataParser(new ColorFactory()).Parse(new BufferedReader(@"
				faith_details = {
					color = hex #345345345
				}"));
			// Assert: Faith data should still be created despite the invalid color.
			Assert.Null(faithData.Color);
			logWriter.ToString().Should().Contain("Found invalid color");
		} finally {
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void ReligiousHeadIsSetWhenNotNone() {
		var faith = ParseFaith("test_faith", @"
			faith_details = {
				religious_head = k_papal_state
			}");

		Assert.Equal("k_papal_state", faith.ReligiousHeadTitleId);
	}

	[Fact]
	public void ReligiousHeadIsNotSetWhenNone() {
		var faith = ParseFaith("test_faith", @"
			faith_details = {
				religious_head = none
			}");

		Assert.Null(faith.ReligiousHeadTitleId);
	}

	[Fact]
	public void ReligiousHeadIsSerializedWhenSet() {
		var faith = ParseFaith("test_faith", @"
			faith_details = {
				religious_head = k_papal_state
			}");

		// Assert: The religious head should be serialized in faith_details
		var faithStr = PDXSerializer.Serialize(faith);
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
		var religion = new Religion("test_religion", reader, religions);
		string serializedReligion = PDXSerializer.Serialize(religion);
		serializedReligion.Should().Contain("ReligiousHeadName=second_title");
		serializedReligion.Should().Contain("ReligiousHeadAdj=second_adj");
		serializedReligion.Should().NotContain("first_title");
		serializedReligion.Should().NotContain("first_adj");
	}

	[Fact]
	public void DuplicatesInFaithLocalizationAreRemoved() {
		var faith = ParseFaith("test_faith", """
			{
				localization = {
					ReligiousHeadName = first_title
					ReligiousHeadAdj = first_adj
					ReligiousHeadName = second_title
					ReligiousHeadAdj = second_adj
				}
			}
			""");

		string serializedFaith = PDXSerializer.Serialize(faith);
		serializedFaith.Should().Contain("ReligiousHeadName=second_title");
		serializedFaith.Should().Contain("ReligiousHeadAdj=second_adj");
		serializedFaith.Should().NotContain("first_title");
		serializedFaith.Should().NotContain("first_adj");
	}

	[Fact]
	public void FaithAttributesAreParsedAndStored() {
		// Arrange: A faith with detail keys and custom attributes (not specially handled keywords).
		var faith = ParseFaith("test_faith", """
			{
				faith_details = {
					icon = custom_faith_icon
				}
				custom_modifier = some_value
				special_mechanic = yes
				localization = {}
			}
			""");

		// Serialize to verify attributes are stored and output correctly.
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
		var religion = new Religion("test_religion", reader, religions);

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
	public void HolySiteLimitsDefaultToCK3Definitions() {
		var reader = new BufferedReader("{ religion_details = { family = rf_pagan } }");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions);

		Assert.Equal(3, religion.EminentHolySitesMax);
		Assert.Equal(1, religion.EminentHolySitesMin);
		Assert.Equal(9, religion.HolySitesMax);
		Assert.Equal(1, religion.HolySitesMin);
		Assert.Equal(3, religion.EffectiveEminentHolySitesMax);
		Assert.Equal(1, religion.EffectiveEminentHolySitesMin);
		Assert.Null(religion.MainHolySiteId);
	}

	[Fact]
	public void HolySiteLimitsAndMainHolySiteAreParsedAndSerialized() {
		var reader = new BufferedReader("""
			{
				eminent_holy_sites_max = 5
				eminent_holy_sites_min = 2
				holy_sites_max = 12
				holy_sites_min = 4
				main_holy_site = rome
			}
			""");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions);

		Assert.Equal(5, religion.EminentHolySitesMax);
		Assert.Equal(2, religion.EminentHolySitesMin);
		Assert.Equal(12, religion.HolySitesMax);
		Assert.Equal(4, religion.HolySitesMin);
		Assert.Equal("rome", religion.MainHolySiteId);

		var religionStr = PDXSerializer.Serialize(religion);
		religionStr.Should().ContainAll(
			"eminent_holy_sites_max = 5",
			"eminent_holy_sites_min = 2",
			"holy_sites_max = 12",
			"holy_sites_min = 4",
			"main_holy_site = rome"
		);
	}

	[Fact]
	public void MainHolySiteNoneIsParsedAsNull() {
		var reader = new BufferedReader("{ main_holy_site = none }");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions);

		Assert.Null(religion.MainHolySiteId);
	}

	[Fact]
	public void ContradictoryHolySiteLimitsAreCappedAndWarned() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader("""
			{
				eminent_holy_sites_max = 12
				eminent_holy_sites_min = 7
				holy_sites_max = 5
			}
			""");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions);

		Assert.Equal(12, religion.EminentHolySitesMax);
		Assert.Equal(5, religion.EffectiveEminentHolySitesMax);
		Assert.Equal(5, religion.EffectiveEminentHolySitesMin);
		output.ToString().Should().Contain("greater than holy_sites_max (5)");
		output.ToString().Should().Contain("effective eminent holy site max (5)");
	}

	[Fact]
	public void InvalidHolySiteLimitsFallBackToDefaultsAndWarn() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader("""
			{
				eminent_holy_sites_max = many
				holy_sites_min = -1
			}
			""");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions);

		Assert.Equal(3, religion.EminentHolySitesMax);
		Assert.Equal(1, religion.HolySitesMin);
		output.ToString().Should().Contain("invalid eminent_holy_sites_max: many. Using default 3.");
		output.ToString().Should().Contain("invalid holy_sites_min: -1. Using default 1.");
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
		var religion = new Religion("test_religion", reader, religions);

		var serialized = religion.Serialize(indent: "", withBraces: true);
		var parsedReligion = new Religion("test_religion", new BufferedReader(serialized), religions);

		Assert.Equal("rf_pagan", parsedReligion.FamilyId);
		Assert.Equal(["doctrine_monotheist"], parsedReligion.DoctrineIds);
	}

	[Fact]
	public void Serialize_WithoutBraces_OmitsOuterBraces() {
		var reader = new BufferedReader("{ doctrine = doctrine_no_head }");
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", reader, religions);

		var withoutBraces = religion.Serialize(indent: "", withBraces: false);
		Assert.StartsWith("religion_details = {", withoutBraces.TrimStart());

		var withBraces = religion.Serialize(indent: "", withBraces: true);
		Assert.StartsWith("{", withBraces.TrimStart());
	}
}
