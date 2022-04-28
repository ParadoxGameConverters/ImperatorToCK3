using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils {
	public class ContainerFieldTests {
		[Fact]
		public void ValueCanBeAddedToHistory() {
			var buildingsField = new HistoryField("buildings", initialValue: new List<object> { "temple", "aqueduct" });
			buildingsField.AddValueToHistory(new List<object> { "temple", "brothel", "forum" }, "buildings", new Date(100, 1, 1));
			Assert.Equal(new List<object> { "temple", "aqueduct" }, buildingsField.GetValue(new Date(99, 1, 1)));
			Assert.Equal(new List<object> { "temple", "brothel", "forum" }, buildingsField.GetValue(new Date(100, 1, 1)));
		}
		[Fact]
		public void InitialValueCanBeChanged() {
			var buildingsField = new HistoryField("buildings", initialValue: new List<object> { "temple", "aqueduct" });
			Assert.Equal(new List<object> { "temple", "aqueduct" }, buildingsField.GetValue(new Date(1, 1, 1)));
			buildingsField.InitialValue.Value = new List<object> { "temple", "brothel", "forum" };
			Assert.Equal(new List<object> { "temple", "brothel", "forum" }, buildingsField.GetValue(new Date(1, 1, 1)));
		}
	}
}
