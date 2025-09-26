using commonItems;
using ImperatorToCK3.CommonUtils;
using System;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class IHistoryFieldTests {
	[Fact]
	public void ReturnsLatestEntryOnOrBeforeGivenDate() {
		var field = new DummyHistoryField();

		field.AddEntryToHistory(null, "init", "initValue");
		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "first");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "second");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "second-late");

		var result = ((IHistoryField)field).GetLastEntryWithDate(new Date(200, 1, 1));

		Assert.Equal(new Date(200, 1, 1), result.Key);
		Assert.Equal("second-late", result.Value);
	}

	[Fact]
	public void ReturnsEntryFromMostRecentEarlierDateWhenExactDateMissing() {
		var field = new DummyHistoryField();

		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "first");
		field.AddEntryToHistory(new Date(150, 1, 1), "setter", "second");

		var result = ((IHistoryField)field).GetLastEntryWithDate(new Date(140, 1, 1));

		Assert.Equal(new Date(100, 1, 1), result.Key);
		Assert.Equal("first", result.Value);
	}

	[Fact]
	public void FallsBackToLastInitialEntryWhenNoEarlierDatedEntriesExist() {
		var field = new DummyHistoryField();

		field.AddEntryToHistory(null, "setter", "initial1");
		field.AddEntryToHistory(null, "setter", "initial2");
		field.AddEntryToHistory(new Date(200, 1, 1), "setter", "future");

		var result = ((IHistoryField)field).GetLastEntryWithDate(new Date(150, 1, 1));

		Assert.Null(result.Key);
		var entry = Assert.IsType<KeyValuePair<string, object>>(result.Value);
		Assert.Equal("setter", entry.Key);
		Assert.Equal("initial2", entry.Value);
	}

	[Fact]
	public void ReturnsNullPairWhenHistoryEmpty() {
		var field = new DummyHistoryField();

		var result = ((IHistoryField)field).GetLastEntryWithDate(new Date(50, 1, 1));

		Assert.Null(result.Key);
		Assert.Null(result.Value);
	}

	[Fact]
	public void ReturnsLastInitialEntryWhenDateIsNull() {
		var field = new DummyHistoryField();

		field.AddEntryToHistory(null, "setter", "initial1");
		field.AddEntryToHistory(null, "setter", "initial2");
		field.AddEntryToHistory(new Date(100, 1, 1), "setter", "dated");

		var result = ((IHistoryField)field).GetLastEntryWithDate(null);

		Assert.Null(result.Key);
		var entry = Assert.IsType<KeyValuePair<string, object>>(result.Value);
		Assert.Equal("setter", entry.Key);
		Assert.Equal("initial2", entry.Value);
	}

	private sealed class DummyHistoryField : IHistoryField {
		public string Id { get; } = "dummy";
		public List<KeyValuePair<string, object>> InitialEntries { get; } = [];
		public SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict { get; } = new();

		public object? GetValue(Date? date) => throw new NotSupportedException();

		public void AddEntryToHistory(Date? date, string keyword, object value) {
			var entry = new KeyValuePair<string, object>(keyword, value);
			if (date is Date actualDate) {
				if (!DateToEntriesDict.TryGetValue(actualDate, out var entries)) {
					entries = [];
					DateToEntriesDict.Add(actualDate, entries);
				}

				entries.Add(entry);
				return;
			}

			InitialEntries.Add(entry);
		}

		public void RegisterKeywords(Parser parser, Date date) => throw new NotSupportedException();

		public IHistoryField Clone() => throw new NotSupportedException();
	}
}
