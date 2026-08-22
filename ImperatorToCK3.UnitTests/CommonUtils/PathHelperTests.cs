using ImperatorToCK3.CommonUtils;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class PathHelperTests {
	[Fact]
	public void RemoveTrailingSeparators_ReturnsNull_ForNull() {
		Assert.Null(PathHelper.RemoveTrailingSeparators(null!));
	}

	[Fact]
	public void RemoveTrailingSeparators_ReturnsEmptyString_ForEmptyString() {
		Assert.Equal(string.Empty, PathHelper.RemoveTrailingSeparators(string.Empty));
	}

	[Fact]
	public void RemoveTrailingSeparators_LeavesPathWithoutTrailingSeparatorUnchanged() {
		Assert.Equal("foo/bar", PathHelper.RemoveTrailingSeparators("foo/bar"));
	}

	[Fact]
	public void RemoveTrailingSeparators_RemovesSingleTrailingSeparator() {
		Assert.Equal("foo/bar", PathHelper.RemoveTrailingSeparators("foo/bar/"));
	}

	[Fact]
	public void RemoveTrailingSeparators_RemovesMultipleTrailingSeparators() {
		Assert.Equal("foo/bar", PathHelper.RemoveTrailingSeparators("foo/bar///"));
	}

	[Fact]
	public void RemoveTrailingSeparators_RemovesBothSeparatorCharacters() {
		Assert.Equal("foo/bar", PathHelper.RemoveTrailingSeparators("foo/bar/" + Path.DirectorySeparatorChar));
	}

	[Fact]
	public void RemoveTrailingSeparators_RemovesPlatformDefaultSeparator() {
		var path = $"foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}";
		Assert.Equal($"foo{Path.DirectorySeparatorChar}bar", PathHelper.RemoveTrailingSeparators(path));
	}
}