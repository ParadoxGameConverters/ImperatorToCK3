using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Pops;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System;
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

	[Fact]
	public void ReligionsAreLoaded() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.LoadReligions(ck3ModFS, new ColorFactory());

		var religionIds = religions.Select(r => r.Id);
		religionIds.Should().Contain(new[] { "religion_a", "religion_b", "religion_c" });
	}

	[Fact]
	public void ReplaceableHolySitesCanBeLoaded() {
		var religions = new ReligionCollection(new Title.LandedTitles());
		religions.LoadReligions(ck3ModFS, new ColorFactory());
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
		titles.LoadTitles(titlesReader);

		var religions = new ReligionCollection(titles);
		religions.LoadHolySites(ck3ModFS);
		religions.LoadReligions(ck3ModFS, new ColorFactory());
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
}