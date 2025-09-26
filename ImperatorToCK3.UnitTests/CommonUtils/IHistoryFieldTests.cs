using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class IHistoryFieldTests {
	[Fact]
	public void ReturnsLatestEntryOnOrBeforeGivenDate() {
		var field = CreateField();

		field.AddEntryToHistory(null, "setter", "initValue");
		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "first");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "second");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "second-late");

		var result = field.GetLastEntryWithDate(new Date(200, 1, 1));

		Assert.Equal(new Date(200, 1, 1), result.Key);
		Assert.Equal("second-late", result.Value);
	}

	[Fact]
	public void ReturnsEntryFromMostRecentEarlierDateWhenExactDateMissing() {
		var field = CreateField();

		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "first");
		field.AddEntryToHistory(new Date(150, 1, 1), "setter", "second");

		var result = field.GetLastEntryWithDate(new Date(140, 1, 1));

		Assert.Equal(new Date(100, 1, 1), result.Key);
		Assert.Equal("first", result.Value);
	}

	[Fact]
	public void FallsBackToLastInitialEntryWhenNoEarlierDatedEntriesExist() {
		var field = CreateField();

		field.AddEntryToHistory(null, "setter", "initial1");
		field.AddEntryToHistory(null, "setter", "initial2");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "future");

		var result = field.GetLastEntryWithDate(new Date(150, 1, 1));

		Assert.Null(result.Key);
		var entry = Assert.IsType<KeyValuePair<string, object>>(result.Value);
		Assert.Equal("setter", entry.Key);
		Assert.Equal("initial2", entry.Value);
	}

	[Fact]
	public void ReturnsNullPairWhenHistoryEmpty() {
		var field = CreateField();

		var result = field.GetLastEntryWithDate(new Date(50, 1, 1));

		Assert.Null(result.Key);
		Assert.Null(result.Value);
	}

	[Fact]
	public void ReturnsLastInitialEntryWhenDateIsNull() {
		var field = CreateField();

		field.AddEntryToHistory(null, "setter", "initial1");
		field.AddEntryToHistory(null, "setter", "initial2");
		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "dated");

		var result = field.GetLastEntryWithDate(null);

		Assert.Null(result.Key);
		var entry = Assert.IsType<KeyValuePair<string, object>>(result.Value);
		Assert.Equal("setter", entry.Key);
		Assert.Equal("initial2", entry.Value);
	}

	private static IHistoryField CreateField() => new SimpleHistoryField(
		fieldName: "field",
		setterKeywords: new OrderedSet<string> { "setter" },
		initialValue: null
	);
}
