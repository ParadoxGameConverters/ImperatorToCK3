using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using AwesomeAssertions;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class FaithTests {
	private readonly Religion testReligion;
	
	public FaithTests() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		testReligion = new Religion("test_religion", new BufferedReader("{}"), religions);
	}

	private Religion CreateReligion(string religionBody) {
		return new Religion("limited_religion", new BufferedReader(religionBody), testReligion.ReligionCollection);
	}

	private static (Faith faith, string log) CreateFaithCapturingLog(Func<Faith> factory) {
		var output = new StringWriter();
		var originalOut = Console.Out;
		Console.SetOut(output);
		try {
			var faith = factory();
			return (faith, output.ToString());
		} finally {
			Console.SetOut(originalOut);
		}
	}
	
	[Fact]
	public void HolySiteIdsAreLoadedAndSerialized() {
		var faithData = new FaithData {
			HolySiteIds = ["rome", "constantinople"],
			EminentHolySiteIds = ["antioch"],
			HasEminentHolySitesBlock = true
		};
		var faith = new Faith("chalcedonian", faithData, testReligion);

		Assert.Collection(faith.HolySiteIds,
			site => Assert.Equal("rome", site),
			site => Assert.Equal("constantinople", site),
			site => Assert.Equal("antioch", site));
		Assert.Equal(["antioch"], faith.EminentHolySiteIds);

		var faithStr = PDXSerializer.Serialize(faith).ReplaceLineEndings("\n");
		faithStr.Should().Contain("\tholy_sites = {\n\t\trome\n\t\tconstantinople\n\t}");
		faithStr.Should().Contain("\teminent_holy_sites = {\n\t\tantioch\n\t}");
	}

	[Fact]
	public void LegacyFaithsPromoteHolySitesToEminent() {
		var faithData = new FaithData {HolySiteIds = ["rome", "constantinople", "antioch", "jerusalem"]};
		var faith = new Faith("chalcedonian", faithData, testReligion);

		faith.EminentHolySiteIds.Should().Equal("rome", "constantinople", "antioch");
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "antioch", "jerusalem");
	}

	[Fact]
	public void LegacyFaithsPromoteHolySitesUpToReligionEminentMax() {
		var religion = CreateReligion("{ eminent_holy_sites_max = 2 }");
		var faithData = new FaithData {HolySiteIds = ["rome", "constantinople", "antioch", "jerusalem"]};
		var faith = new Faith("chalcedonian", faithData, religion);

		faith.EminentHolySiteIds.Should().BeEquivalentTo("rome", "constantinople");
		faith.HolySiteIds.Should().Equal("rome", "constantinople", "antioch", "jerusalem");
	}

	[Fact]
	public void LegacyFaithPromotionPrioritizesMainHolySite() {
		var religion = CreateReligion("{ main_holy_site = jerusalem }");
		var faithData = new FaithData {HolySiteIds = ["rome", "constantinople", "antioch", "jerusalem"]};
		var faith = new Faith("chalcedonian", faithData, religion);

		faith.EminentHolySiteIds.Should().BeEquivalentTo("rome", "constantinople", "jerusalem");
	}

	[Fact]
	public void ConverterFaithExcessEminentHolySitesAreDemoted() {
		var faithData = new FaithData {
			EminentHolySiteIds = ["site1", "site2", "site3", "site4", "site5"],
			HasEminentHolySitesBlock = true
		};
		var (faith, log) = CreateFaithCapturingLog(
			() => new Faith("too_many_eminent", faithData, testReligion, isFromConverter: true));

		faith.HolySiteIds.Should().BeEquivalentTo("site1", "site2", "site3", "site4", "site5");
		faith.EminentHolySiteIds.Should().Equal("site1", "site2", "site3");

		var faithStr = PDXSerializer.Serialize(faith).ReplaceLineEndings("\n");
		faithStr.Should().Contain("\tholy_sites = {\n\t\tsite4\n\t\tsite5\n\t}");
		faithStr.Should().Contain("\teminent_holy_sites = {\n\t\tsite1\n\t\tsite2\n\t\tsite3\n\t}");
		log.Should().Contain("Demoted: site4, site5");
	}

	[Fact]
	public void ConverterFaithExcessHolySitesAreTrimmed() {
		var religion = CreateReligion("{ holy_sites_max = 5 }");
		var faithData = new FaithData {
			HolySiteIds = ["site1", "site2", "site3", "site4", "site5", "site6"],
			EminentHolySiteIds = ["site1", "site2"],
			HasEminentHolySitesBlock = true
		};
		var (faith, log) = CreateFaithCapturingLog(
			() => new Faith("too_many_sites", faithData, religion, isFromConverter: true));

		faith.HolySiteIds.Should().BeEquivalentTo("site1", "site2", "site3", "site4", "site5");
		faith.EminentHolySiteIds.Should().Equal("site1", "site2");
		log.Should().Contain("Removed: site6");
	}

	[Fact]
	public void ConverterFaithTrimKeepsMainHolySite() {
		var religion = CreateReligion("""
			{
				main_holy_site = site6
				holy_sites_max = 4
			}
			""");
		var faithData = new FaithData {
			HolySiteIds = ["site1", "site2", "site3", "site4", "site5", "site6"]
		};
		var (faith, log) = CreateFaithCapturingLog(
			() => new Faith("main_site_protected", faithData, religion, isFromConverter: true));

		faith.HolySiteIds.Should().BeEquivalentTo("site1", "site2", "site3", "site6");
		faith.EminentHolySiteIds.Should().BeEquivalentTo("site1", "site2", "site6");
		log.Should().Contain("Removed: site5, site4");
	}

	[Fact]
	public void ConverterFaithPromotesRegularSitesToEminentMinimum() {
		var religion = CreateReligion("""
			{
				eminent_holy_sites_max = 3
				eminent_holy_sites_min = 2
			}
			""");
		var faithData = new FaithData {
			HolySiteIds = ["site1", "site2", "site3"],
			HasEminentHolySitesBlock = true
		};
		var faith = new Faith("below_eminent_min", faithData, religion, isFromConverter: true);

		faith.EminentHolySiteIds.Should().Equal("site1", "site2");
		faith.HolySiteIds.Should().Equal("site1", "site2", "site3");
	}

	[Fact]
	public void ConverterFaithPromotionPrioritizesMainHolySite() {
		var religion = CreateReligion("""
			{
				eminent_holy_sites_min = 1
				main_holy_site = site3
			}
			""");
		var faithData = new FaithData {
			HolySiteIds = ["site1", "site2", "site3"],
			HasEminentHolySitesBlock = true
		};
		var faith = new Faith("main_site_min", faithData, religion, isFromConverter: true);

		faith.EminentHolySiteIds.Should().Equal("site3");
	}

	[Fact]
	public void GameFaithOverHolySiteLimitsIsNotModified() {
		var religion = CreateReligion("""
			{
				eminent_holy_sites_max = 2
				holy_sites_max = 4
			}
			""");
		var faithData = new FaithData {
			HolySiteIds = ["site1", "site2", "site3", "site4"],
			EminentHolySiteIds = ["site5", "site6", "site7"],
			HasEminentHolySitesBlock = true
		};
		var (faith, log) = CreateFaithCapturingLog(
			() => new Faith("game_faith", faithData, religion));

		faith.HolySiteIds.Should().BeEquivalentTo("site1", "site2", "site3", "site4", "site5", "site6", "site7");
		faith.EminentHolySiteIds.Should().BeEquivalentTo("site5", "site6", "site7");
		log.Should().Contain("allows at most 2");
		log.Should().Contain("allows at most 4");
	}

	[Fact]
	public void ConverterFaithBelowHolySitesMinimumLogsWarning() {
		var faithData = new FaithData {HasEminentHolySitesBlock = true};
		var (faith, log) = CreateFaithCapturingLog(
			() => new Faith("no_sites", faithData, testReligion, isFromConverter: true));

		faith.HolySiteIds.Should().BeEmpty();
		faith.EminentHolySiteIds.Should().BeEmpty();
		log.Should().Contain("has only 0 holy sites");
	}

	[Fact]
	public void FaithColorIsSerialized() {
		var faithData = new FaithData {Color = new Color(0.15, 1, 0.7)};
		var faith = new Faith("celtic_pagan", faithData, testReligion);

		Assert.Equal(new Color(0.15, 1, 0.7), faith.Color);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().Contain(
			"color = rgb { 178 160 0 }"
		);
	}

	[Fact]
	public void FaithAttributesAreSerialized() {
		var faithData = new FaithData {
			DetailsAttributes = new OrderedDictionary<string, StringOfItem> {
				["icon"] = new StringOfItem("celtic_pagan")
			},
			Attributes = new List<KeyValuePair<string, StringOfItem>> {
				new("tenet_selection_pair", new StringOfItem("""
					{
						requires_dlc_flag = the_northern_lords
						tenet = tenet_gruesome_festivals
						fallback_tenet = tenet_human_sacrifice
					}
					"""))
			}
		};
		var faith = new Faith("celtic_pagan", faithData, testReligion);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().ContainAll(
			"icon = celtic_pagan",
			"tenet_selection_pair"
		);
	}

	[Fact]
	public void ReligiousHeadTitleIdIsCorrectlySerialized() {
		var faithData = new FaithData {ReligiousHeadTitleId = "d_papacy"};
		var faith = new Faith("atheism", faithData, testReligion);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().Contain("religious_head = d_papacy");
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
			DetailsAttributes = new OrderedDictionary<string, StringOfItem> {
				["icon"] = new StringOfItem("celtic_pagan")
			}
		};
		faithData.DoctrineIds.Add("unreformed_faith_doctrine");
		var faith = new Faith("celtic_pagan", faithData, testReligion);

		var faithStr = PDXSerializer.Serialize(faith);
		faithStr.Should().Contain("reformed_icon = celtic_pagan");
	}

	[Fact]
	public void TooManyDoctrinesInGroupAreTrimmedKeepingLastOne() {
		// Arrange doctrine group with 3 possible doctrines.
		var group = new DoctrineGroup("catA", new BufferedReader(""));
		group.AddDoctrineId("doc_a1");
		group.AddDoctrineId("doc_a2");
		group.AddDoctrineId("doc_a3");
		testReligion.ReligionCollection.DoctrineGroups.AddOrReplace(group);

		var output = new StringWriter();
		Console.SetOut(output);

		// Faith initially has 3 doctrines from the same group (only one can be active).
		var faithData = new FaithData();
		faithData.DoctrineIds.Add("doc_a1");
		faithData.DoctrineIds.Add("doc_a2");
		faithData.DoctrineIds.Add("doc_a3");
		var faith = new Faith("too_many_doctrines", faithData, testReligion);

		// Should keep only the last one among these and drop the others.
		Assert.Contains("doc_a3", faith.DoctrineIds as IReadOnlyCollection<string>);
		Assert.DoesNotContain("doc_a1", faith.DoctrineIds as IReadOnlyCollection<string>);
		Assert.DoesNotContain("doc_a2", faith.DoctrineIds as IReadOnlyCollection<string>);

		// Warning is logged.
		output.ToString().Should().Contain("Faith too_many_doctrines has too many doctrines in group catA");
	}

	[Fact]
	public void GetDoctrineIdsForDoctrineGroup_PrefersFaithOverReligion() {
		// Arrange group with two doctrines.
		var group = new DoctrineGroup("catB", new BufferedReader(""));
		group.AddDoctrineId("doc_b1");
		group.AddDoctrineId("doc_b2");
		testReligion.ReligionCollection.DoctrineGroups.AddOrReplace(group);

		// Religion has doc_b2, faith has doc_b1.
		testReligion.DoctrineIds.Clear();
		testReligion.DoctrineIds.Add("doc_b2");

		var faithData = new FaithData();
		faithData.DoctrineIds.Add("doc_b1");
		var faith = new Faith("prefers_faith", faithData, testReligion);

		var picks = faith.GetDoctrineIdsForDoctrineGroupId("catB");
		picks.Should().Equal("doc_b1");
	}

	[Fact]
	public void GetDoctrineIdsForDoctrineGroup_FallsBackToReligion() {
		// Arrange group with two doctrines.
		var group = new DoctrineGroup("catB_fallback", new BufferedReader(""));
		group.AddDoctrineId("doc_b1");
		group.AddDoctrineId("doc_b2");
		testReligion.ReligionCollection.DoctrineGroups.AddOrReplace(group);

		// Religion has doc_b2, faith has none from this category.
		testReligion.DoctrineIds.Clear();
		testReligion.DoctrineIds.Add("doc_b2");

		var faithData = new FaithData();
		faithData.DoctrineIds.Add("unrelated_doctrine");
		var faith = new Faith("fallback_faith", faithData, testReligion);

		var picks = faith.GetDoctrineIdsForDoctrineGroupId("catB_fallback");
		picks.Should().Equal("doc_b2");
	}

	[Fact]
	public void MainRiteDoctrinesTakePrecedenceOverFaithDoctrines() {
		// Arrange group with two doctrines.
		var group = new DoctrineGroup("catR", new BufferedReader(""));
		group.AddDoctrineId("doc_r1");
		group.AddDoctrineId("doc_r2");
		testReligion.ReligionCollection.DoctrineGroups.AddOrReplace(group);

		// Faith has doc_r1, but its main rite has doc_r2.
		var faithData = new FaithData { MainRiteId = "test_rite" };
		faithData.DoctrineIds.Add("doc_r1");
		var faith = new Faith("rite_precedence", faithData, testReligion);

		var riteReader = new BufferedReader("{ doctrines = { doc_r2 } }");
		testReligion.ReligionCollection.Rites.AddOrReplace(
			new Rite("test_rite", riteReader, testReligion.ReligionCollection, new ColorFactory()));

		var picks = faith.GetDoctrineIdsForDoctrineGroupId("catR");
		picks.Should().Equal("doc_r2");
	}

	[Fact]
	public void GetDoctrineIdsForDoctrineGroup_UnknownGroupReturnsEmpty() {
		var faithData = new FaithData();
		faithData.DoctrineIds.Add("anything");
		var faith = new Faith("unknown_group", faithData, testReligion);

		var picks = faith.GetDoctrineIdsForDoctrineGroupId("no_such_group");
		Assert.Empty(picks);
	}

	[Fact]
	public void HasDoctrine_TrueWhenFaithHasDoctrine() {
		var group = new DoctrineGroup("catC", new BufferedReader(""));
		group.AddDoctrineId("doc_c1");
		group.AddDoctrineId("doc_c2");
		testReligion.ReligionCollection.DoctrineGroups.AddOrReplace(group);

		var faithData = new FaithData();
		faithData.DoctrineIds.Add("doc_c1");
		var faith = new Faith("has_doctrine", faithData, testReligion);

		Assert.True(faith.HasDoctrine("doc_c1"));
		Assert.False(faith.HasDoctrine("doc_c2"));
	}

	[Fact]
	public void HasDoctrine_FallsBackToReligionWhenFaithHasNoneInCategory() {
		var group = new DoctrineGroup("catC_fallback", new BufferedReader(""));
		group.AddDoctrineId("doc_c1");
		group.AddDoctrineId("doc_c2");
		testReligion.ReligionCollection.DoctrineGroups.AddOrReplace(group);

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

	[Fact]
	public void NewFormatFaithDataIsParsed() {
		var parser = new FaithDataParser(new ColorFactory());
		var faithData = parser.Parse(new BufferedReader("""
			{
				faith_details = {
					religion = test_religion
					color = rgb { 10 20 30 }
					religious_head = d_head
					head_of_rite = k_head
					icon = my_icon
					reformed_icon = my_reformed_icon
				}
				main_rite = my_rite
				holy_sites = { site1 site2 }
				eminent_holy_sites = { site3 }
				tenets = { tenet_a tenet_b }
				doctrines = { doctrine_a }
				tenet_selection_pair = {
					requires_dlc_flag = some_dlc
					tenet = tenet_c
					fallback_tenet = tenet_d
				}
				localization = {
					Key = Value
				}
			}
			"""));

		Assert.Equal("test_religion", faithData.ReligionId);
		Assert.Equal("rgb { 10 20 30 }", faithData.Color!.Value.OutputRgb());
		Assert.Equal("d_head", faithData.ReligiousHeadTitleId);
		Assert.Equal("k_head", faithData.HeadOfRiteTitleId);
		Assert.Equal("my_rite", faithData.MainRiteId);
		Assert.True(faithData.HasEminentHolySitesBlock);
		Assert.Equal(["site1", "site2"], faithData.HolySiteIds);
		Assert.Equal(["site3"], faithData.EminentHolySiteIds);
		Assert.Equal(["tenet_a", "tenet_b", "doctrine_a"], faithData.DoctrineIds);
		Assert.Contains("tenet_selection_pair", faithData.Attributes.Select(pair => pair.Key));
		Assert.Contains("Key", faithData.Localization.Keys);
		Assert.Equal("my_icon", faithData.DetailsAttributes["icon"].ToString());
		Assert.Equal("my_reformed_icon", faithData.DetailsAttributes["reformed_icon"].ToString());
	}

	[Fact]
	public void SerializedFaithCanBeParsedBack() {
		var faithData = new FaithData {
			ReligionId = "test_religion",
			Color = new Color(0.1, 0.2, 0.3),
			ReligiousHeadTitleId = "d_head",
			MainRiteId = "my_rite",
			HolySiteIds = ["site1"],
			EminentHolySiteIds = ["site2"],
			HasEminentHolySitesBlock = true
		};
		faithData.DoctrineIds.Add("tenet_test");
		faithData.DoctrineIds.Add("doctrine_test");
		var faith = new Faith("test_faith", faithData, testReligion);

		var serialized = faith.Serialize(indent: "", withBraces: true);
		var parsed = new FaithDataParser(new ColorFactory()).Parse(new BufferedReader(serialized));

		Assert.Equal("test_religion", parsed.ReligionId);
		Assert.Equal("my_rite", parsed.MainRiteId);
		Assert.Equal("d_head", parsed.ReligiousHeadTitleId);
		Assert.Equal(["site1"], parsed.HolySiteIds);
		Assert.Equal(["site2"], parsed.EminentHolySiteIds);
		Assert.Equal(["tenet_test", "doctrine_test"], parsed.DoctrineIds);
		Assert.Equal(faith.Color!.Value.OutputRgb(), parsed.Color!.Value.OutputRgb());
	}

	[Fact]
	public void Serialize_WithoutBraces_OmitsOuterBraces() {
		var faith = new Faith("bare_faith", new FaithData(), testReligion);

		var withoutBraces = faith.Serialize(indent: "", withBraces: false);
		Assert.StartsWith("faith_details = {", withoutBraces.TrimStart());

		var withBraces = faith.Serialize(indent: "", withBraces: true);
		Assert.StartsWith("{", withBraces.TrimStart());
	}
}