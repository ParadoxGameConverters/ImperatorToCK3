using FluentAssertions;
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
		
		religions.re
	}
}