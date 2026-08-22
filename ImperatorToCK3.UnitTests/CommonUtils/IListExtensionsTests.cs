using System.Collections.Generic;
using System.Collections.ObjectModel;
using ImperatorToCK3.CommonUtils;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class IListExtensionsTests {
	[Fact]
	public void RemoveAll_OnList_RemovesMatchingItemsAndReturnsCount() {
		var list = new List<int> {1, 2, 3, 4, 5};

		var removed = list.RemoveAll(x => x % 2 == 0);

		Assert.Equal(2, removed);
		Assert.Equal(new[] {1, 3, 5}, list);
	}

	[Fact]
	public void RemoveAll_OnIListImplementation_RemovesMatchingItemsAndReturnsCount() {
		IList<string> list = new Collection<string> { "a", "b", "c", "d", "e" };

		var removed = list.RemoveAll(x => x is "b" or "d");

		Assert.Equal(2, removed);
		Assert.Equal(new[] { "a", "c", "e" }, list);
	}

	[Fact]
	public void RemoveAll_WithNoMatch_ReturnsZeroAndLeavesListUnchanged() {
		var list = new List<int> { 1, 2, 3 };

		var removed = list.RemoveAll(x => x > 10);

		Assert.Equal(0, removed);
		Assert.Equal(new[] { 1, 2, 3 }, list);
	}
}
