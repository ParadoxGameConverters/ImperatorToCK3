using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.CommonUtils;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils {
	public class ContainerFieldTests {
		[Fact]
		public void ValueCanBeAddedToHistory() {
			var buildingsField = new HistoryField(initialValue: new List<string> { "temple", "aqueduct" });
			buildingsField.AddValueToHistory(new List<string> { "temple", "brothel", "forum" }, new Date(100, 1, 1));
			Assert.Equal(new List<string> { "temple", "aqueduct" }, buildingsField.GetValue(new Date(99, 1, 1)));
			Assert.Equal(new List<string> { "temple", "brothel", "forum" }, buildingsField.GetValue(new Date(100, 1, 1)));
		}
		[Fact]
		public void InitialValueCanBeChanged() {
			var buildingsField = new HistoryField(initialValue: new List<string> { "temple", "aqueduct" });
			Assert.Equal(new List<string> { "temple", "aqueduct" }, buildingsField.GetValue(new Date(1, 1, 1)));
			buildingsField.InitialValue = new List<string> { "temple", "brothel", "forum" };
			Assert.Equal(new List<string> { "temple", "brothel", "forum" }, buildingsField.GetValue(new Date(1, 1, 1)));
		}
	}
}
