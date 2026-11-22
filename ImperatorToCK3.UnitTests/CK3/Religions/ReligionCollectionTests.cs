using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using AwesomeAssertions;
using DotLiquid;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Pops;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System;
using System.IO;
using System.Linq;
using Xunit;
using ReligionCollection = ImperatorToCK3.CK3.Religions.ReligionCollection;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

public class ReligionCollectionTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private readonly ModFilesystem imperatorModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private const string CK3Root = "TestFiles/CK3/game";
	private readonly ModFilesystem ck3ModFS = new(CK3Root, Array.Empty<Mod>());
	private const string TestReligionsDirectory = "TestFiles/CK3/game/common/religion/religions";
	private const string TestReplaceableHolySitesFile = "TestFiles/configurables/replaceable_holy_sites.txt";
	private static readonly ColorFactory colorFactory = new();

	[Fact]
	public void ReligionsAreLoaded() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.LoadReligions(ck3ModFS, colorFactory);

		var religionIds = religions.Select(r => r.Id);
		religionIds.Should().Contain(new[] { "religion_a", "religion_b", "religion_c" });
	}

	[Fact]
	public void ReplaceableHolySitesCanBeLoaded() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.LoadReligions(ck3ModFS, colorFactory);
		religions.LoadReplaceableHolySites(TestReplaceableHolySitesFile);

		religions.ReplaceableHolySitesByFaith["religion_a_faith"]
			.Should()
			.BeEquivalentTo("site1", "site2", "site3", "site4", "site5");
		religions.ReplaceableHolySitesByFaith["religion_b_faith"]
			.Should()
			.BeEquivalentTo("site1");
		religions.ReplaceableHolySitesByFaith["religion_c_faith"]
			.Should()
			.BeEquivalentTo("site1", "site2", "site3");
	}

	[Fact]
	public void ProvincesCanBeGroupedByFaith() {
		var date = new Date("476.1.1");

		var impProv1 = new ImperatorToCK3.Imperator.Provinces.Province(1);
		var prov1 = new Province(1) { PrimaryImperatorProvince = impProv1 };
		prov1.SetFaithId("faith1", date: null);
		var impProv2 = new ImperatorToCK3.Imperator.Provinces.Province(2);
		var prov2 = new Province(2) { PrimaryImperatorProvince = impProv2 };
		prov2.SetFaithId("faith1", date);
		var impProv3 = new ImperatorToCK3.Imperator.Provinces.Province(3);
		var prov3 = new Province(3) { PrimaryImperatorProvince = impProv3 };
		prov3.SetFaithId("faith2", date);
		var prov4 = new Province(4); // has no Imperator province, won't be considered
		prov4.SetFaithId("faith2", date);
		var prov5 = new Province(5); // has no Imperator province, won't be considered
		prov5.SetFaithId("faith2", date);

		var provinces = new ProvinceCollection { prov1, prov2, prov3, prov4, prov5 };
		var provsByFaith = ReligionCollection.GetProvincesFromImperatorByFaith(provinces, date);

		provsByFaith.Should().HaveCount(2);
		provsByFaith["faith1"].Should().Equal(prov1, prov2);
		provsByFaith["faith2"].Should().Equal(prov3);
	}

	[Fact]
	public void ImperatorHolySitesAndMostPopulousProvinceAreSelectedForDynamicHolySites() {
		Province GenerateCK3AndImperatorProvinceWithPops(ulong provId, int popCount, bool holySite) {
			var imperatorProv = new ImperatorToCK3.Imperator.Provinces.Province(provId);
			for (int i = 0; i < popCount; ++i) {
				var popId = (ulong)HashCode.Combine(provId, i);
				imperatorProv.Pops.Add(popId, new Pop(popId));
			}
			if (holySite) {
				imperatorProv.HolySiteId = provId;
			}

			var ck3Prov = new Province(provId) { PrimaryImperatorProvince = imperatorProv };
			ck3Prov.SetFaithId("ck3Faith", date: null);
			return ck3Prov;
		}

		var imperatorScriptValues = new ScriptValueCollection();
		var imperatorReligions = new ImperatorToCK3.Imperator.Religions.ReligionCollection(imperatorScriptValues);
		imperatorReligions.LoadDeities(imperatorModFS);

		var deityManagerReader = new BufferedReader(
			@"deities_database = {
				1 = { deity=""deity1"" }
				2 = { deity=""deity2"" }
				3 = { deity=""deity3"" }
				4 = { deity=""deity4"" }
				5 = { deity=""deity5"" }
				6 = { deity=""deity6"" }
				7 = { deity=""deity7"" }
			}"
		);
		imperatorReligions.LoadHolySiteDatabase(deityManagerReader);

		var provinces = new ProvinceCollection {
			// provinces for dynamic holy sites
			GenerateCK3AndImperatorProvinceWithPops(1, popCount: 1, holySite: true),
			GenerateCK3AndImperatorProvinceWithPops(2, popCount: 7, holySite: true),
			GenerateCK3AndImperatorProvinceWithPops(3, popCount: 4, holySite: true),
			GenerateCK3AndImperatorProvinceWithPops(4, popCount: 2, holySite: true),
			GenerateCK3AndImperatorProvinceWithPops(5, popCount: 3, holySite: false),
			GenerateCK3AndImperatorProvinceWithPops(6, popCount: 6, holySite: false),
			GenerateCK3AndImperatorProvinceWithPops(7, popCount: 5, holySite: false),

			// provinces for existing (predefined) holy sites of faith ck3Faith
			GenerateCK3AndImperatorProvinceWithPops(8, popCount: 0, holySite: false),
			GenerateCK3AndImperatorProvinceWithPops(9, popCount: 0, holySite: false),
			GenerateCK3AndImperatorProvinceWithPops(10, popCount: 0, holySite: false),
			GenerateCK3AndImperatorProvinceWithPops(11, popCount: 0, holySite: false),
			GenerateCK3AndImperatorProvinceWithPops(12, popCount: 0, holySite: false),
		};

		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader(
			"c_county1={ b_barony1={province=1} } " +
			"c_county2={ b_barony2={province=2} } " +
			"c_county3={ b_barony3={province=3} } " +
			"c_county4={ b_barony4={province=4} } " +
			"c_county5={ b_barony5={province=5} } " +
			"c_county6={ b_barony6={province=6} } " +
			"c_county7={ b_barony7={province=7} } " +

			// baronies for existing (predefined) holy sites of faith ck3Faith
			"c_site_county1={ b_site_barony1={province=8} } " +
			"c_site_county2={ b_site_barony2={province=9} } " +
			"c_site_county3={ b_site_barony3={province=10} } " +
			"c_site_county4={ b_site_barony4={province=11} } " +
			"c_site_county5={ b_site_barony5={province=12} }");
		titles.LoadTitles(titlesReader, colorFactory);

		var religions = new ReligionCollection(titles);
		religions.LoadHolySites(ck3ModFS);
		religions.LoadReligions(ck3ModFS, colorFactory);
		religions.LoadReplaceableHolySites("TestFiles/configurables/replaceable_holy_sites.txt");

		var faith = religions.GetFaith("ck3Faith");
		Assert.NotNull(faith);

		religions.DetermineHolySites(
			provinces,
			imperatorReligions,
			new HolySiteEffectMapper("TestFiles/HolySiteEffectMapperTests/mappings.txt"),
			new Date("476.1.1")
		);

		faith.HolySiteIds.Should().Equal(
			"IRtoCK3_b_barony2_ck3Faith", // holy site, 7 pops
			"IRtoCK3_b_barony3_ck3Faith", // holy site, 4 pops
			"IRtoCK3_b_barony4_ck3Faith", // holy site, 2 pops
			"IRtoCK3_b_barony1_ck3Faith", // holy site, 1 pop
			"IRtoCK3_b_barony6_ck3Faith" // 6 pops - most populous province without an Imperator holy site
		);
	}

	[Fact]
	public void OptionalFaithIsNotLoadedIfInvalidatedByExistingFaiths() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.LoadReligions(ck3ModFS, colorFactory);
		religions.AddOrReplace(new ImperatorToCK3.CK3.Religions.Religion(
			"north_african_religion", new BufferedReader("{ religion_details = { family = rf_pagan } }"), religions));
		Assert.Contains(religions.Faiths, f => f.Id == "religion_a_faith");
		
		religions.LoadConverterFaiths("TestFiles/configurables/optional_faiths.liquid", colorFactory, liquidVariables: new Hash());
		// Optional berber_pagan is invalidated by religion_a_faith, so it should not be loaded.
		Assert.DoesNotContain(religions.Faiths, r => r.Id == "berber_pagan");
	}

	[Fact]
	public void OptionalFaithCanBeLoaded() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.AddOrReplace(new ImperatorToCK3.CK3.Religions.Religion(
			"north_african_religion", new BufferedReader("{ religion_details = { family = rf_pagan } }"), religions));
		Assert.DoesNotContain(religions.Faiths, r => r.Id == "religion_a_faith");
		
		religions.LoadConverterFaiths("TestFiles/configurables/optional_faiths.liquid", colorFactory, liquidVariables: new Hash());
		Assert.Contains(religions.Faiths, r => r.Id == "berber_pagan");
	}

	[Fact]
	public void ChristianAndIslamicSyncretismTenetsAreRemovedFromReligionsAndFaiths() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		var reader = new BufferedReader(
			"doctrine=tenet_christian_syncretism " +
			"doctrine=tenet_islamic_syncretism " +
			"doctrine=tenet_normal");
		var religion = new ImperatorToCK3.CK3.Religions.Religion("religion_x", reader, religions);
		religions.AddOrReplace(religion);

		var faithDataParser = new FaithDataParser(colorFactory);
		religion.Faiths.AddOrReplace(new Faith("faith_a", faithDataParser.Parse(new BufferedReader(
			"doctrines = { tenet_christian_syncretism tenet_islamic_syncretism tenet_faith_normal }")), religion));
		religion.Faiths.AddOrReplace(new Faith("faith_b", faithDataParser.Parse(new BufferedReader(
			"doctrines = { tenet_islamic_syncretism }")), religion));

		var riteReader = new BufferedReader("{ doctrines = { tenet_christian_syncretism tenet_rite_normal } }");
		religions.Rites.AddOrReplace(new Rite("rite_x", riteReader, religions, colorFactory));

		religions.RemoveChristianAndIslamicSyncretismFromAllFaiths();

		religion.DoctrineIds.Should().NotContain(new[] { "tenet_christian_syncretism", "tenet_islamic_syncretism" });
		religion.DoctrineIds.Should().Contain("tenet_normal");

		var faithA = religion.Faiths["faith_a"];
		faithA.DoctrineIds.Should().NotContain(new[] { "tenet_christian_syncretism", "tenet_islamic_syncretism" });
		faithA.DoctrineIds.Should().Contain("tenet_faith_normal");

		var faithB = religion.Faiths["faith_b"];
		faithB.DoctrineIds.Should().NotContain("tenet_islamic_syncretism");

		religions.Rites["rite_x"].DoctrineIds.Should().NotContain("tenet_christian_syncretism");
		religions.Rites["rite_x"].DoctrineIds.Should().Contain("tenet_rite_normal");
	}

	[Fact]
	public void ConverterHolySitesAreLoaded() {
		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader(
			"c_testcounty={ b_testbarony={province=1} } c_testcounty2={ b_testbarony2={province=2} }"),
			colorFactory);

		var religions = new ReligionCollection(titles);
		religions.LoadConverterHolySites("TestFiles/configurables/converter_holy_sites_test.txt");

		religions.HolySites.ContainsKey("hs_test1").Should().BeTrue();
		religions.HolySites["hs_test1"].IsFromConverter.Should().BeTrue();
		religions.HolySites["hs_test1"].CountyId.Should().Be("c_testcounty");
		religions.HolySites["hs_test1"].BaronyId.Should().Be("b_testbarony");

		religions.HolySites.ContainsKey("hs_test2").Should().BeTrue();
		religions.HolySites["hs_test2"].IsFromConverter.Should().BeTrue();
		religions.HolySites["hs_test2"].CountyId.Should().Be("c_testcounty2");
	}

	[Fact]
	public void DoctrinesAreLoaded() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.LoadDoctrines(ck3ModFS);

		religions.DoctrineGroups.ContainsKey("doctrine_group_a").Should().BeTrue();
		religions.DoctrineGroups["doctrine_group_a"].DoctrineIds.Should()
			.BeEquivalentTo(new[] { "doctrine_a1", "doctrine_a2" });
		religions.DoctrineGroups.ContainsKey("doctrine_group_b").Should().BeTrue();
	}

	[Fact]
	public void MissingDoctrinesAreReplacedWithFallbacks() {
		var tempRoot = Path.Combine(Path.GetTempPath(), "ReligionFallbackTests", Guid.NewGuid().ToString("N"));
		try {
			Directory.CreateDirectory(Path.Combine(tempRoot, "common", "religion", "doctrine_group_types"));
			File.WriteAllText(
				Path.Combine(tempRoot, "common", "religion", "doctrine_group_types", "groups.txt"),
				"doctrine_theism = { }\n" +
				"doctrine_coronation = { }\n" +
				"doctrine_funeral = { }\n" +
				"doctrine_pilgrimage = { }\n");
			Directory.CreateDirectory(Path.Combine(tempRoot, "common", "religion", "doctrine_types"));
			File.WriteAllText(
				Path.Combine(tempRoot, "common", "religion", "doctrine_types", "doctrines.txt"),
				"doctrine_other = { doctrine_group_type = doctrine_theism }\n" +
				"doctrine_polytheist = { doctrine_group_type = doctrine_theism }\n" +
				"doctrine_no_anointment = { doctrine_group_type = doctrine_coronation }\n" +
				"doctrine_funeral_stoic = { doctrine_group_type = doctrine_funeral }\n" +
				"doctrine_pilgrimage_encouraged = { doctrine_group_type = doctrine_pilgrimage }\n");
			var faithsFile = Path.Combine(tempRoot, "faiths.liquid");
			File.WriteAllText(faithsFile,
				"bare_faith = { faith_details = { religion = test_religion } }\n");
			var religionsFile = Path.Combine(tempRoot, "religions.liquid");
			File.WriteAllText(religionsFile,
				"test_religion = { religion_details = { family = rf_pagan } }\n");

			var religions = new ReligionCollection(new Title.LandedTitles());
			religions.LoadDoctrines(new ModFilesystem(tempRoot, Array.Empty<Mod>()));
			religions.LoadConverterReligions(religionsFile, colorFactory, liquidVariables: new Hash());
			religions.LoadConverterFaiths(faithsFile, colorFactory, liquidVariables: new Hash());

			var faith = religions.GetFaith("bare_faith");
			Assert.NotNull(faith);
			faith.DoctrineIds.Should().Contain([
				"doctrine_polytheist",
				"doctrine_no_anointment",
				"doctrine_funeral_stoic",
				"doctrine_pilgrimage_encouraged"
			]);
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void ReplaceableHolySitesWarnOnUnexpectedValuesAndMissingFaiths() {
		var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
		try {
			File.WriteAllText(tempFile,
				"religion_a_faith = { site1 }\n" +
				"missing_faith = all\n" +
				"religion_b_faith = 5\n");

			var religions = new ReligionCollection(new Title.LandedTitles());
			religions.LoadReligions(ck3ModFS, colorFactory);

			var output = new StringWriter();
			Console.SetOut(output);
			religions.LoadReplaceableHolySites(tempFile);
			var log = output.ToString();

			religions.ReplaceableHolySitesByFaith["religion_a_faith"].Should().BeEquivalentTo("site1");
			Assert.DoesNotContain("religion_b_faith", religions.ReplaceableHolySitesByFaith.Keys);
			log.Should().Contain("Unexpected value: 5");
			log.Should().Contain("Replaceable holy sites not loaded for missing faiths: missing_faith");
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void FaithsInNewFormatAreLoaded() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.LoadReligions(ck3ModFS, colorFactory);

		var faith = religions.GetFaith("religion_a_faith");
		Assert.NotNull(faith);
		Assert.Equal("religion_a", faith.Religion.Id);
		faith.HolySiteIds.Should().BeEquivalentTo("site1", "site2", "site3", "site4", "site5");
		faith.EminentHolySiteIds.Should().BeEquivalentTo("site3", "site4", "site5");
	}

	[Fact]
	public void DoctrineTypesAreAssignedToDoctrineGroups() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.LoadDoctrines(ck3ModFS);

		religions.DoctrineGroups["doctrine_group_a"].DoctrineIds
			.Should().BeEquivalentTo("doctrine_a1", "doctrine_a2");
		religions.DoctrineGroups["doctrine_group_b"].DoctrineIds
			.Should().BeEquivalentTo("doctrine_b1");
		religions.TenetIds.Should().Contain(["tenet_test", "tenet_other"]);
	}

	[Fact]
	public void ConverterFaithInNewFormatIsLoadedAndGetsMainlineRite() {
		var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".liquid");
		try {
			File.WriteAllText(tempFile, """
				converted_faith = {
					faith_details = {
						religion = religion_a
						color = rgb { 1 2 3 }
						icon = custom_icon
					}
					tenets = {
						tenet_test
					}
					doctrines = {
						doctrine_a1
					}
					holy_sites = {
						site1
					}
					eminent_holy_sites = {
						site2
					}
				}
				""");

			var religions = new ReligionCollection(new Title.LandedTitles());
			religions.LoadReligions(ck3ModFS, colorFactory);
			religions.LoadConverterFaiths(tempFile, colorFactory, liquidVariables: new Hash());

			var faith = religions.GetFaith("converted_faith");
			Assert.NotNull(faith);
			Assert.Equal("religion_a", faith.Religion.Id);
			Assert.Equal("converted_faith", faith.MainRiteId);
			faith.DoctrineIds.Should().BeEquivalentTo("tenet_test", "doctrine_a1");

			Assert.True(religions.Rites.ContainsKey("converted_faith"));
			var rite = religions.Rites["converted_faith"];
			Assert.Equal("converted_faith", rite.FaithId);
			rite.DoctrineIds.Should().BeEquivalentTo("tenet_test", "doctrine_a1");
			religions.ConverterFaithIds.Should().Contain("converted_faith");
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConverterRitesAreLoaded() {
		var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".liquid");
		try {
			File.WriteAllText(tempFile, """
				custom_rite = {
					faith = religion_a_faith
					color = rgb { 4 5 6 }
					tenets = { tenet_test }
				}
				""");

			var religions = new ReligionCollection(new Title.LandedTitles());
			religions.LoadConverterRites(tempFile, colorFactory, liquidVariables: new Hash());

			Assert.True(religions.Rites.ContainsKey("custom_rite"));
			religions.Rites["custom_rite"].FaithId.Should().Be("religion_a_faith");
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void RiteIdsInFaithHistoryAreResolvedToFaithIds() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		var riteReader = new BufferedReader("{ faith = test_faith }");
		religions.Rites.AddOrReplace(new Rite("test_rite", riteReader, religions, colorFactory));

		var province = new Province(1, new BufferedReader("""
			102.1.1 = { rite = test_rite }
			202.1.1 = { rite = unknown_rite }
			"""));
		religions.ResolveRiteIdsInFaithHistory(province.History);

		province.GetFaithId(new Date(150, 1, 1)).Should().Be("test_faith");
		province.GetRiteId(new Date(150, 1, 1)).Should().Be("test_rite");
		// The invalid later rite entry is removed, so the previous valid state persists.
		province.GetFaithId(new Date(250, 1, 1)).Should().Be("test_faith");
		province.GetRiteId(new Date(250, 1, 1)).Should().Be("test_rite");
	}

	private static (ReligionCollection religions, Faith faith) CreateFaithWithRite(string? mainRiteId) {
		var religions = new ReligionCollection(new Title.LandedTitles());
		var religion = new Religion("test_religion", new BufferedReader("{}"), religions);
		religions.AddOrReplace(religion);
		var faith = new Faith("test_faith", new FaithData {
			ReligionId = "test_religion",
			MainRiteId = mainRiteId
		}, religion);
		religion.Faiths.AddOrReplace(faith);
		return (religions, faith);
	}

	[Fact]
	public void HistoryFaithEntriesAreConvertedToDefaultRites() {
		var (religions, _) = CreateFaithWithRite(mainRiteId: "test_rite");
		religions.Rites.AddOrReplace(new Rite("test_rite", new BufferedReader("{ faith = test_faith }"), religions, colorFactory));

		var province = new Province(1, new BufferedReader("faith = test_faith"));
		religions.ConvertFaithHistoryToRites(province.History);

		province.GetFaithId(new Date(1000, 1, 1)).Should().BeNull();
		province.GetRiteId(new Date(1000, 1, 1)).Should().Be("test_rite");
	}

	[Fact]
	public void SharedMainRiteIsUsedForFaith() {
		// Vanilla shares roman_rite between christian_faith and catholic.
		var (religions, _) = CreateFaithWithRite(mainRiteId: "shared_rite");
		religions.Rites.AddOrReplace(new Rite("shared_rite", new BufferedReader("{ faith = origin_faith }"), religions, colorFactory));

		var province = new Province(1, new BufferedReader("faith = test_faith"));
		religions.ConvertFaithHistoryToRites(province.History);

		province.GetFaithId(new Date(1000, 1, 1)).Should().BeNull();
		province.GetRiteId(new Date(1000, 1, 1)).Should().Be("shared_rite");
	}

	[Fact]
	public void ExistingMatchingRiteIsPreservedWhenConvertingHistory() {
		var (religions, _) = CreateFaithWithRite(mainRiteId: "default_rite");
		religions.Rites.AddOrReplace(new Rite("default_rite", new BufferedReader("{ faith = test_faith }"), religions, colorFactory));
		religions.Rites.AddOrReplace(new Rite("input_rite", new BufferedReader("{ faith = test_faith }"), religions, colorFactory));

		var province = new Province(1, new BufferedReader("""
			faith = test_faith
			rite = input_rite
			"""));
		religions.ConvertFaithHistoryToRites(province.History);

		province.GetFaithId(new Date(1000, 1, 1)).Should().BeNull();
		province.GetRiteId(new Date(1000, 1, 1)).Should().Be("input_rite");
	}

	[Fact]
	public void StaleRiteIsReplacedWithFaithDefaultRite() {
		var (religions, _) = CreateFaithWithRite(mainRiteId: "default_rite");
		religions.Rites.AddOrReplace(new Rite("default_rite", new BufferedReader("{ faith = test_faith }"), religions, colorFactory));
		religions.Rites.AddOrReplace(new Rite("other_rite", new BufferedReader("{ faith = other_faith }"), religions, colorFactory));

		var province = new Province(1, new BufferedReader("""
			faith = test_faith
			rite = other_rite
			"""));
		religions.ConvertFaithHistoryToRites(province.History);

		province.GetRiteId(new Date(1000, 1, 1)).Should().Be("default_rite");
	}

	[Fact]
	public void DatedFaithEntriesAreConvertedToRites() {
		var (religions, _) = CreateFaithWithRite(mainRiteId: "test_rite");
		religions.Rites.AddOrReplace(new Rite("test_rite", new BufferedReader("{ faith = test_faith }"), religions, colorFactory));

		var province = new Province(1, new BufferedReader("""
			100.1.1 = { faith = test_faith }
			200.1.1 = { faith = test_faith }
			"""));
		religions.ConvertFaithHistoryToRites(province.History);

		province.GetRiteId(new Date(150, 1, 1)).Should().Be("test_rite");
		province.GetRiteId(new Date(250, 1, 1)).Should().Be("test_rite");
		province.GetFaithId(new Date(250, 1, 1)).Should().BeNull();
	}

	[Fact]
	public void ConverterFaithHolySiteLimitsAreAppliedDuringLoad() {
		var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".liquid");
		try {
			File.WriteAllText(tempFile, """
				converted_faith = {
					faith_details = {
						religion = religion_a
					}
					holy_sites = {
						site1
						site2
					}
					eminent_holy_sites = {
						site3
						site4
						site5
						site6
					}
				}
				""");

			var religions = new ReligionCollection(new Title.LandedTitles());
			religions.LoadReligions(ck3ModFS, colorFactory);
			religions.LoadConverterFaiths(tempFile, colorFactory, liquidVariables: new Hash());

			var faith = religions.GetFaith("converted_faith");
			Assert.NotNull(faith);
			faith.EminentHolySiteIds.Should().BeEquivalentTo("site3", "site4", "site5");
			faith.HolySiteIds.Should().BeEquivalentTo("site1", "site2", "site3", "site4", "site5", "site6");
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void GameFaithHolySiteLimitsAreWarnedButNotChangedDuringLoad() {
		var tempRoot = Path.Combine(
			Path.GetTempPath(),
			"ImperatorToCK3_UnitTests",
			"ReligionLimits",
			Guid.NewGuid().ToString("N")
		);
		try {
			var religionsDir = Path.Combine(tempRoot, "common", "religion", "religion_types");
			var faithsDir = Path.Combine(tempRoot, "common", "religion", "faith_types");
			Directory.CreateDirectory(religionsDir);
			Directory.CreateDirectory(faithsDir);

			File.WriteAllText(Path.Combine(religionsDir, "limited_religion.txt"), """
				limited_religion = {
					religion_details = {
						family = rf_pagan
					}
					eminent_holy_sites_max = 1
					holy_sites_max = 2
				}
				""");
			File.WriteAllText(Path.Combine(faithsDir, "limited_faith.txt"), """
				limited_faith = {
					faith_details = {
						religion = limited_religion
					}
					holy_sites = {
						site1
						site2
						site3
					}
					eminent_holy_sites = {
						site4
						site5
					}
				}
				""");

			var output = new StringWriter();
			var originalOut = Console.Out;
			ReligionCollection religions = new(new Title.LandedTitles());
			Console.SetOut(output);
			try {
				religions.LoadReligions(new ModFilesystem(tempRoot, Array.Empty<Mod>()), colorFactory);
			} finally {
				Console.SetOut(originalOut);
			}

			var faith = religions.GetFaith("limited_faith");
			Assert.NotNull(faith);
			faith.EminentHolySiteIds.Should().BeEquivalentTo("site4", "site5");
			faith.HolySiteIds.Should().BeEquivalentTo("site1", "site2", "site3", "site4", "site5");
			output.ToString().Should().Contain("allows at most 1");
			output.ToString().Should().Contain("allows at most 2");
		} finally {
			if (Directory.Exists(tempRoot)) {
				Directory.Delete(tempRoot, recursive: true);
			}
		}
	}
}