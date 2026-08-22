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

	[Fact]
	public void RemoveHistoryPastDateRemovesOnlyEntriesPastGivenDate() {
		var field = CreateField();

		field.AddEntryToHistory(null, "setter", "initValue");
		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "first");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "second");
		field.AddEntryToHistory(new Date(300, 1, 1), "setter", "third");

		field.RemoveHistoryPastDate(new Date(200, 1, 1));

		Assert.Equal(2, field.DateToEntriesDict.Count);
		Assert.True(field.DateToEntriesDict.ContainsKey(new Date(100, 1, 1)));
		Assert.True(field.DateToEntriesDict.ContainsKey(new Date(200, 1, 1)));
		Assert.False(field.DateToEntriesDict.ContainsKey(new Date(300, 1, 1)));

		// Initial entries are never touched by history trimming.
		Assert.Single(field.InitialEntries);

		// The value as of the removed date falls back to the last kept entry.
		Assert.Equal("second", field.GetValue(new Date(300, 1, 1)));
	}

	[Fact]
	public void RemoveHistoryPastDateKeepsEntryOnGivenDate() {
		var field = CreateField();

		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "first");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "second");

		field.RemoveHistoryPastDate(new Date(100, 1, 1));

		// Only entries strictly past the given date are removed.
		Assert.True(field.DateToEntriesDict.ContainsKey(new Date(100, 1, 1)));
		Assert.False(field.DateToEntriesDict.ContainsKey(new Date(200, 1, 1)));
		Assert.Equal("first", field.GetValue(new Date(200, 1, 1)));
	}

	[Fact]
	public void RemoveHistoryPastDateDoesNothingWhenAllEntriesAreOnOrBeforeGivenDate() {
		var field = CreateField();

		field.AddEntryToHistory(new Date(50, 1, 1), "setter", "first");
		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "second");

		field.RemoveHistoryPastDate(new Date(200, 1, 1));

		Assert.Equal(2, field.DateToEntriesDict.Count);
		Assert.Equal("second", field.GetValue(new Date(200, 1, 1)));
	}

	[Fact]
	public void RemoveHistoryPastDateRemovesAllDatedEntriesWhenGivenDateIsEarlierThanThem() {
		var field = CreateField();

		field.AddEntryToHistory(null, "setter", "initValue");
		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "first");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "second");

		field.RemoveHistoryPastDate(new Date(50, 1, 1));

		Assert.Empty(field.DateToEntriesDict);

		// The value falls back to the initial entry.
		Assert.Equal("initValue", field.GetValue(new Date(200, 1, 1)));
	}

	[Fact]
	public void RemoveHistoryPastDateDoesNothingWhenThereAreNoDatedEntries() {
		var field = CreateField();

		field.AddEntryToHistory(null, "setter", "initValue");

		field.RemoveHistoryPastDate(new Date(100, 1, 1));

		Assert.Empty(field.DateToEntriesDict);
		Assert.Single(field.InitialEntries);
	}

	private static IHistoryField CreateField() => new SimpleHistoryField(
		fieldName: "field",
		setterKeywords: new OrderedSet<string> { "setter" },
		initialValue: null
	);
}
