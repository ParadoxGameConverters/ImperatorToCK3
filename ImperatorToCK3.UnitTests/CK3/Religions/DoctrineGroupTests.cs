using ImperatorToCK3.CK3.Religions;
using commonItems;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

public class DoctrineGroupTests {
	[Fact]
	public void Constructor_SetsId() {
		var reader = new BufferedReader("");
		var category = new DoctrineGroup("test_id", reader);
		Assert.Equal("test_id", category.Id);
	}

	[Fact]
	public void Constructor_ParsesCategoryId() {
		var reader = new BufferedReader("category = test_category");
		var category = new DoctrineGroup("cat", reader);
		Assert.Equal("test_category", category.CategoryId);
	}

	[Fact]
	public void Constructor_ParsesNumberOfPicks() {
		var reader = new BufferedReader("number_of_picks = 3");
		var category = new DoctrineGroup("cat", reader);
		Assert.Equal(3, category.NumberOfPicks);
	}

	[Fact]
	public void Constructor_DefaultsNumberOfPicksTo1() {
		var reader = new BufferedReader("");
		var category = new DoctrineGroup("cat", reader);
		Assert.Equal(1, category.NumberOfPicks);
	}

	[Fact]
	public void Constructor_ParsesDoctrineIds() {
		var reader = new BufferedReader("doctrine_types = { doctrine1 doctrine2 doctrine3 }");
		var category = new DoctrineGroup("cat", reader);
		Assert.Equal(["doctrine1", "doctrine2", "doctrine3"], category.DoctrineIds);
	}

	[Fact]
	public void DoctrineIds_IsReadOnly() {
		var reader = new BufferedReader("doctrine1");
		var category = new DoctrineGroup("cat", reader);
		Assert.IsType<IReadOnlyCollection<string>>(category.DoctrineIds, exactMatch: false);
	}
}
