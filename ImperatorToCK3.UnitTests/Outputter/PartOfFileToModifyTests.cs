using ImperatorToCK3.Outputter;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class PartOfFileToModifyTests {
	[Fact]
	public void Constructor_SetsWarnIfNotFoundToTrueByDefault() {
		var part = new PartOfFileToModify("before", "after");

		Assert.True(part.WarnIfNotFound);
		Assert.Equal("before", part.TextBefore);
		Assert.Equal("after", part.TextAfter);
	}

	[Fact]
	public void Constructor_SetsExplicitWarnIfNotFound() {
		var part = new PartOfFileToModify("before", "after", warnIfNotFound: false);

		Assert.False(part.WarnIfNotFound);
	}

	[Fact]
	public void Equals_WithSameValues_ReturnsTrue() {
		var a = new PartOfFileToModify("before", "after", warnIfNotFound: false);
		var b = new PartOfFileToModify("before", "after", warnIfNotFound: false);

		Assert.True(a.Equals(b));
	}

	[Fact]
	public void Equals_WithDifferentTextBefore_ReturnsFalse() {
		var a = new PartOfFileToModify("before1", "after");
		var b = new PartOfFileToModify("before2", "after");

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void Equals_WithDifferentTextAfter_ReturnsFalse() {
		var a = new PartOfFileToModify("before", "after1");
		var b = new PartOfFileToModify("before", "after2");

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void Equals_WithDifferentWarnIfNotFound_ReturnsFalse() {
		var a = new PartOfFileToModify("before", "after", warnIfNotFound: true);
		var b = new PartOfFileToModify("before", "after", warnIfNotFound: false);

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void EqualsObject_WithPartOfFileToModify_ReturnsTrue() {
		object a = new PartOfFileToModify("before", "after");
		object b = new PartOfFileToModify("before", "after");

		Assert.True(a.Equals(b));
	}

	[Fact]
	public void EqualsObject_WithNull_ReturnsFalse() {
		var part = new PartOfFileToModify("before", "after");

		Assert.False(part.Equals(null));
	}

	[Fact]
	public void EqualsObject_WithOtherType_ReturnsFalse() {
		var part = new PartOfFileToModify("before", "after");

		Assert.False(part.Equals("not a part"));
	}

	[Fact]
	public void GetHashCode_ForEqualInstances_AreEqual() {
		var a = new PartOfFileToModify("before", "after", warnIfNotFound: false);
		var b = new PartOfFileToModify("before", "after", warnIfNotFound: false);

		Assert.Equal(a.GetHashCode(), b.GetHashCode());
	}

	[Fact]
	public void EqualityOperator_WithEqualInstances_ReturnsTrue() {
		var a = new PartOfFileToModify("before", "after");
		var b = new PartOfFileToModify("before", "after");

		Assert.True(a == b);
	}

	[Fact]
	public void EqualityOperator_WithDifferentInstances_ReturnsFalse() {
		var a = new PartOfFileToModify("before", "after");
		var b = new PartOfFileToModify("different", "after");

		Assert.False(a == b);
	}

	[Fact]
	public void InequalityOperator_WithDifferentInstances_ReturnsTrue() {
		var a = new PartOfFileToModify("before", "after");
		var b = new PartOfFileToModify("different", "after");

		Assert.True(a != b);
	}

	[Fact]
	public void InequalityOperator_WithEqualInstances_ReturnsFalse() {
		var a = new PartOfFileToModify("before", "after");
		var b = new PartOfFileToModify("before", "after");

		Assert.False(a != b);
	}

	[Fact]
	public void Deconstruct_ReturnsAllFields() {
		var part = new PartOfFileToModify("before", "after", warnIfNotFound: false);

		var (textBefore, textAfter, warnIfNotFound) = part;

		Assert.Equal("before", textBefore);
		Assert.Equal("after", textAfter);
		Assert.False(warnIfNotFound);
	}
}
