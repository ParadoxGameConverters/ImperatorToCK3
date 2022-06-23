using commonItems;
using FluentAssertions;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Pops;
using System;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions; 

public class ReligionCollectionTests {
	private const string TestReligionsDirectory = "TestFiles/CK3/game/common/religion/religions";
	private const string TestReplaceableHolySitesFile = "TestFiles/configurables/replaceable_holy_sites.txt";
	
	[Fact]
	public void ReligionsAreGroupedByFile() {
		var religions = new ReligionCollection();
		religions.LoadReligions(TestReligionsDirectory);
		
		religions.ReligionsPerFile["religion_a.txt"].Select(r=>r.Id)
			.Should().BeEquivalentTo("religion_a");
		religions.ReligionsPerFile["multiple_religions.txt"].Select(r=>r.Id)
			.Should().BeEquivalentTo("religion_b", "religion_c");
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
		var prov1 = new Province(1) {Religion = "faith1", ImperatorProvince = impProv1};
		var impProv2 = new ImperatorToCK3.Imperator.Provinces.Province(2);
		var prov2 = new Province(2) {Religion = "faith1", ImperatorProvince = impProv2};
		var impProv3 = new ImperatorToCK3.Imperator.Provinces.Province(3);
		var prov3 = new Province(3) {Religion = "faith2", ImperatorProvince = impProv3};
		var prov4 = new Province(4) {Religion = "faith2"}; // has no Imperator province, won't be considered
		var prov5 = new Province(5) {Religion = "faith3"}; // has no Imperator province, won't be considered

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
		ImperatorToCK3.Imperator.Provinces.Province GenerateImperatorProvinceWithPops(ulong provId, int popCount) {
			var imperatorProv = new ImperatorToCK3.Imperator.Provinces.Province(provId);
			for (int i = 0; i < popCount; ++i) {
				var popId = (ulong)HashCode.Combine(provId, i);
				imperatorProv.Pops.Add(popId, new Pop(popId));
			}
			imperatorProv.HolySiteDeityId = provId;
			return imperatorProv;
		}
		
		var irProv1 = GenerateImperatorProvinceWithPops(1, popCount: 1);
		var irProv2 = GenerateImperatorProvinceWithPops(1, popCount: 7);
		var irProv3 = GenerateImperatorProvinceWithPops(1, popCount: 4);
		var irProv4 = GenerateImperatorProvinceWithPops(1, popCount: 2);
		var irProv5 = GenerateImperatorProvinceWithPops(1, popCount: 3);
		var irProv6 = GenerateImperatorProvinceWithPops(1, popCount: 6);
		var irProv7 = GenerateImperatorProvinceWithPops(1, popCount: 5);

		var ck3Prov1 = new Province(1) {Religion = "faith1", ImperatorProvince = irProv1};
		var ck3Prov2 = new Province(2) {Religion = "faith1", ImperatorProvince = irProv2};
		var ck3Prov3 = new Province(3) {Religion = "faith1", ImperatorProvince = irProv3};
		var ck3Prov4 = new Province(4) {Religion = "faith1", ImperatorProvince = irProv4};
		var ck3Prov5 = new Province(5) {Religion = "faith1", ImperatorProvince = irProv5};
		var ck3Prov6 = new Province(6) {Religion = "faith1", ImperatorProvince = irProv6};
		var ck3Prov7 = new Province(7) {Religion = "faith1", ImperatorProvince = irProv7};

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
		religions.DetermineHolySites(provinces, titles);
		
		throw new NotImplementedException();
	}
}