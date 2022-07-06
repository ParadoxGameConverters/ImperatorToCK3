using commonItems;
using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Pops;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions; 

public class ReligionCollectionTests {
	private const string CK3Root = "TestFiles/CK3/game";
	private readonly ModFilesystem ck3ModFs = new(CK3Root, new Mod[] { });
	private const string TestReligionsDirectory = "TestFiles/CK3/game/common/religion/religions";
	private const string TestReplaceableHolySitesFile = "TestFiles/configurables/replaceable_holy_sites.txt";
	
	[Fact]
	public void ReligionsAreLoaded() {
		var religions = new ReligionCollection();
		religions.LoadReligions(ck3ModFs);

		var religionIds = religions.Select(r => r.Id);
		religionIds.Should().Contain("religion_a", "religion_b", "religion_c");
	}

	[Fact]
	public void ReplaceableHolySitesCanBeLoaded() {
		var religions = new ReligionCollection();
		religions.LoadReligions(TestReligionsDirectory);
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
		var impProv1 = new ImperatorToCK3.Imperator.Provinces.Province(1);
		var prov1 = new Province(1) {FaithId = "faith1", ImperatorProvince = impProv1};
		var impProv2 = new ImperatorToCK3.Imperator.Provinces.Province(2);
		var prov2 = new Province(2) {FaithId = "faith1", ImperatorProvince = impProv2};
		var impProv3 = new ImperatorToCK3.Imperator.Provinces.Province(3);
		var prov3 = new Province(3) {FaithId = "faith2", ImperatorProvince = impProv3};
		var prov4 = new Province(4) {FaithId = "faith2"}; // has no Imperator province, won't be considered
		var prov5 = new Province(5) {FaithId = "faith3"}; // has no Imperator province, won't be considered

		var provinces = new ProvinceCollection {prov1, prov2, prov3, prov4, prov5};
		var provsByFaith = ReligionCollection.GetProvincesByFaith(provinces);

		provsByFaith.Should().HaveCount(2);
		provsByFaith["faith1"].Should().Equal(prov1, prov2);
		provsByFaith["faith2"].Should().Equal(prov3);
	}


	public class TestImperatorProvince : ImperatorToCK3.Imperator.Provinces.Province {
		public TestImperatorProvince(ulong id): base(id) { }
	}
	[Fact]
	public void ImperatorHolySitesAndMostPopulousProvinceAreSelectedForDynamicHolySites() {
		ImperatorToCK3.Imperator.Provinces.Province GenerateImperatorProvinceWithPops(ulong provId, int popCount, bool holySite) {
			var imperatorProv = new ImperatorToCK3.Imperator.Provinces.Province(provId);
			for (int i = 0; i < popCount; ++i) {
				var popId = (ulong)HashCode.Combine(provId, i);
				imperatorProv.Pops.Add(popId, new Pop(popId));
			}
			if (holySite) {
				imperatorProv.HolySiteDeityId = provId;
			}
			return imperatorProv;
		}
		
		var irProv1 = GenerateImperatorProvinceWithPops(1, popCount: 1, holySite: true);
		var irProv2 = GenerateImperatorProvinceWithPops(1, popCount: 7, holySite: true);
		var irProv3 = GenerateImperatorProvinceWithPops(1, popCount: 4, holySite: true);
		var irProv4 = GenerateImperatorProvinceWithPops(1, popCount: 2, holySite: true);
		var irProv5 = GenerateImperatorProvinceWithPops(1, popCount: 3, holySite: false);
		var irProv6 = GenerateImperatorProvinceWithPops(1, popCount: 6, holySite: false);
		var irProv7 = GenerateImperatorProvinceWithPops(1, popCount: 5, holySite: false);

		var ck3Prov1 = new Province(1) {FaithId = "ck3Faith", ImperatorProvince = irProv1};
		var ck3Prov2 = new Province(2) {FaithId = "ck3Faith", ImperatorProvince = irProv2};
		var ck3Prov3 = new Province(3) {FaithId = "ck3Faith", ImperatorProvince = irProv3};
		var ck3Prov4 = new Province(4) {FaithId = "ck3Faith", ImperatorProvince = irProv4};
		var ck3Prov5 = new Province(5) {FaithId = "ck3Faith", ImperatorProvince = irProv5};
		var ck3Prov6 = new Province(6) {FaithId = "ck3Faith", ImperatorProvince = irProv6};
		var ck3Prov7 = new Province(7) {FaithId = "ck3Faith", ImperatorProvince = irProv7};

		var provinces = new ProvinceCollection {
			ck3Prov1,
			ck3Prov2,
			ck3Prov3,
			ck3Prov4,
			ck3Prov5,
			ck3Prov6,
			ck3Prov7
		};
		
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader(
			"b_barony1={province=1} " +
			"b_barony2={province=2} " +
			"b_barony3={province=3} " +
			"b_barony4={province=4} " +
			"b_barony5={province=5} " +
			"b_barony6={province=6} " +
			"b_barony7={province=7} ");
		titles.LoadTitles(titlesReader);

		var religions = new ReligionCollection();
		religions.LoadReligions(ck3ModFs);
		religions.DetermineHolySites(provinces, titles);

		var faith = religions.GetFaith("ck3Faith");
		Assert.NotNull(faith);
		faith.HolySiteIds.Should().Equal(
			"b_barony2", // holy site, 7 pops
			"b_barony3", // holy site, 4 pops
			"b_barony4", // holy site, 2 pops
			"b_barony1", // holy site, 1 pop
			"b_barony6" // 6 pops
		);
	}
}