using ImperatorToCK3.Helpers;
using Xunit;

namespace ImperatorToCK3.UnitTests.Helpers;

public class EnumHelperTests {
	private enum TestEnum {
		First = 1,
		Second = 2,
		Third = 3
	}

	[Fact]
	public void Min_ReturnsSmallerEnumValue() {
		Assert.Equal(TestEnum.First, EnumHelper.Min(TestEnum.First, TestEnum.Second));
		Assert.Equal(TestEnum.Second, EnumHelper.Min(TestEnum.Second, TestEnum.Third));
	}

	[Fact]
	public void Min_ReturnsLeftValueWhenEqual() {
		Assert.Equal(TestEnum.Second, EnumHelper.Min(TestEnum.Second, TestEnum.Second));
	}

	[Fact]
	public void Min_WorksForNumericTypes() {
		Assert.Equal(10, EnumHelper.Min(10, 20));
		Assert.Equal(-5, EnumHelper.Min(-5, 0));
	}
}
