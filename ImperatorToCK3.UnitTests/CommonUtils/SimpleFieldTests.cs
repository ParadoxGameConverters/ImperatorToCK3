using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class SimpleFieldTests {
	[Fact]
	public void ValueCanBeAddedToHistory() {
		var cultureField = new SimpleHistoryField(fieldName: "culture", setterKeywords: new OrderedSet<string> { "culture" }, initialValue: "greek");
		cultureField.AddEntryToHistory(new Date(100, 1, 1), "culture", "roman");
		Assert.Equal("greek", cultureField.GetValue(new Date(99, 1, 1)));
		Assert.Equal("roman", cultureField.GetValue(new Date(100, 1, 1)));
	}
	[Fact]
	public void InitialValueCanBeChanged() {
		var cultureField = new SimpleHistoryField(fieldName: "culture", setterKeywords: new OrderedSet<string> { "culture" }, initialValue: "greek");
		Assert.Equal("greek", cultureField.GetValue(new Date(1, 1, 1)));
		cultureField.InitialEntries.Add(new KeyValuePair<string, object>("culture", "roman"));
		Assert.Equal("roman", cultureField.GetValue(new Date(1, 1, 1)));
	}
}
