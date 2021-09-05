using commonItems;
using ImperatorToCK3.CommonUtils;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils {
	public class SimpleFieldTests {
		[Fact]
		public void ValueCanBeAddedToHistory() {
			var cultureField = new SimpleField(initialValue: "greek");
			cultureField.AddValueToHistory("roman", new Date(100, 1, 1));
			Assert.Equal("greek", cultureField.GetValue(new Date(99, 1, 1)));
			Assert.Equal("roman", cultureField.GetValue(new Date(100, 1, 1)));
		}
		[Fact]
		public void InitialValueCanBeChanged() {
			var cultureField = new SimpleField(initialValue: "greek");
			Assert.Equal("greek", cultureField.GetValue(new Date(1, 1, 1)));
			cultureField.InitialValue = "roman";
			Assert.Equal("roman", cultureField.GetValue(new Date(1, 1, 1)));
		}
	}
}
