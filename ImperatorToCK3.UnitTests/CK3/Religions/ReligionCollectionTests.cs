using FluentAssertions;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
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
}