using commonItems;
using ImperatorToCK3.CK3.Religions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

public class DoctrineTypeTests {
	[Fact]
	public void Constructor_ParsesDoctrineGroupType() {
		var reader = new BufferedReader("doctrine_group_type = doctrine_marriage_type");
		var doctrineType = new DoctrineType("doctrine_monogamy", reader);

		Assert.Equal("doctrine_monogamy", doctrineType.Id);
		Assert.Equal("doctrine_marriage_type", doctrineType.GroupId);
	}

	[Fact]
	public void Constructor_LeavesGroupIdNullWhenNotDefined() {
		var doctrineType = new DoctrineType("doctrine_test", new BufferedReader("index = 1"));

		Assert.Null(doctrineType.GroupId);
	}
}
