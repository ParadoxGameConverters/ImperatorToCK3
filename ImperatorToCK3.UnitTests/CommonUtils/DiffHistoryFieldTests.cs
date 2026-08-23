using AwesomeAssertions;
using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

[Collection("Sequential")]
public class DiffHistoryFieldTests {
	private static DiffHistoryField CreateField() {
		return new DiffHistoryField("clan", new OrderedSet<string> { "insert" }, new OrderedSet<string> { "remove" });
	}

	private static OrderedSet<object> GetValueSet(DiffHistoryField field, Date? date) {
		return (OrderedSet<object>)field.GetValue(date)!;
	}

	[Fact]
	public void FieldsDefaultToEmpty() {
		var field = CreateField();

		Assert.Equal("clan", field.Id);
		Assert.Empty(field.InitialEntries);
		Assert.Empty(field.DateToEntriesDict);
		Assert.Empty(GetValueSet(field, null));
	}

	[Fact]
	public void AddEntryToHistoryAddsInitialEntriesWhenDateIsNull() {
		var field = CreateField();

		field.AddEntryToHistory(null, "insert", "item1");
		field.AddEntryToHistory(null, "remove", "item2");

		Assert.Equal(2, field.InitialEntries.Count);
		Assert.Equal(new KeyValuePair<string, object>("insert", "item1"), field.InitialEntries[0]);
		Assert.Equal(new KeyValuePair<string, object>("remove", "item2"), field.InitialEntries[1]);
	}

	[Fact]
	public void AddEntryToHistoryAddsDatedEntriesToExistingDateBlock() {
		var field = CreateField();

		field.AddEntryToHistory(new Date(100, 1, 1), "insert", "item1");
		field.AddEntryToHistory(new Date(100, 1, 1), "insert", "item2");
		field.AddEntryToHistory(new Date(200, 1, 1), "insert", "item3");

		Assert.Equal(2, field.DateToEntriesDict[new Date(100, 1, 1)].Count);
		Assert.Single(field.DateToEntriesDict[new Date(200, 1, 1)]);
	}

	[Fact]
	public void AddEntryToHistoryWithUnknownKeywordWarnsAndDoesNotAdd() {
		var field = CreateField();
		var output = new StringWriter();
		Console.SetOut(output);

		field.AddEntryToHistory(new Date(100, 1, 1), "badKeyword", "item1");

		var logStr = output.ToString();
		Assert.Contains("[WARN] Keyword badKeyword is not an insert or remove keyword for field clan!", logStr);
		Assert.Empty(field.InitialEntries);
		Assert.Empty(field.DateToEntriesDict);
	}

	[Fact]
	public void GetValueBuildsSetFromInitialEntriesWhenDateIsNull() {
		var field = CreateField();

		field.AddEntryToHistory(null, "insert", "item1");
		field.AddEntryToHistory(null, "insert", "item2");
		field.AddEntryToHistory(null, "remove", "item1");
		field.AddEntryToHistory(null, "insert", "item3");
		field.AddEntryToHistory(new Date(100, 1, 1), "insert", "dated-only");

		// Dated entries are not applied when date is null.
		GetValueSet(field, null).Should().BeEquivalentTo(["item2", "item3"]);
	}

	[Fact]
	public void GetValueAppliesDatedEntriesOnOrBeforeGivenDate() {
		var field = CreateField();

		field.AddEntryToHistory(null, "insert", "item1");
		field.AddEntryToHistory(new Date(100, 1, 1), "remove", "item1");
		field.AddEntryToHistory(new Date(100, 1, 1), "insert", "item2");
		field.AddEntryToHistory(new Date(200, 1, 1), "insert", "item3");

		GetValueSet(field, new Date(99, 1, 1)).Should().BeEquivalentTo(["item1"]);
		GetValueSet(field, new Date(100, 1, 1)).Should().BeEquivalentTo(["item2"]);
		GetValueSet(field, new Date(199, 1, 1)).Should().BeEquivalentTo(["item2"]);
		GetValueSet(field, new Date(200, 1, 1)).Should().BeEquivalentTo(["item2", "item3"]);
	}

	[Fact]
	public void GetValueIgnoresDatedEntriesAfterGivenDate() {
		var field = CreateField();

		field.AddEntryToHistory(null, "insert", "item1");
		field.AddEntryToHistory(new Date(500, 1, 1), "insert", "future");

		GetValueSet(field, new Date(400, 1, 1)).Should().BeEquivalentTo(["item1"]);
	}

	[Fact]
	public void GetValueLogsWarningForUnknownKeywordInEntries() {
		var field = CreateField();
		field.InitialEntries.Add(new KeyValuePair<string, object>("badKeyword", "item1"));
		var output = new StringWriter();
		Console.SetOut(output);

		_ = field.GetValue(null);

		var logStr = output.ToString();
		Assert.Contains("[WARN] Keyword badKeyword is not an insert or remove keyword for field clan!", logStr);
	}

	[Fact]
	public void InsertAndRemoveKeywordsCanBeParsedInDatedBlock() {
		var field = CreateField();
		var parser = new Parser(implicitVariableHandling: true);
		field.RegisterKeywords(parser, new Date(100, 1, 1));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(new BufferedReader("= { insert = item1 insert = item2 remove = item1 }"));

		GetValueSet(field, new Date(100, 1, 1)).Should().BeEquivalentTo(["item2"]);
	}

	[Fact]
	public void ParsedIntegerValuesAreConvertedToInts() {
		var field = CreateField();
		var parser = new Parser(implicitVariableHandling: true);
		field.RegisterKeywords(parser, new Date(100, 1, 1));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(new BufferedReader("= { insert = 123 insert = 456 }"));

		GetValueSet(field, new Date(100, 1, 1)).Should().BeEquivalentTo([123, 456]);
	}

	[Fact]
	public void ConditionalOperatorIsSupportedWhenParsing() {
		var field = CreateField();
		var parser = new Parser(implicitVariableHandling: true);
		field.RegisterKeywords(parser, new Date(100, 1, 1));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(new BufferedReader("= { insert ?= item1 remove ?= item1 }"));

		// item1 was inserted then removed, so the set is empty.
		Assert.Empty(GetValueSet(field, new Date(100, 1, 1)));
	}

	[Fact]
	public void ClonedFieldIsIndependentDeepCopy() {
		var field = CreateField();
		field.AddEntryToHistory(null, "insert", "item1");
		field.AddEntryToHistory(new Date(100, 1, 1), "insert", "item2");
		var clone = (DiffHistoryField)field.Clone();

		Assert.Equal(field.Id, clone.Id);
		GetValueSet(clone, null).Should().BeEquivalentTo(["item1"]);
		GetValueSet(clone, new Date(100, 1, 1)).Should().BeEquivalentTo(["item1", "item2"]);

		// Adding entries to the clone doesn't affect the original.
		clone.AddEntryToHistory(null, "insert", "item3");
		GetValueSet(clone, null).Should().BeEquivalentTo(["item1", "item3"]);
		GetValueSet(field, null).Should().BeEquivalentTo(["item1"]);
		GetValueSet(clone, new Date(100, 1, 1)).Should().BeEquivalentTo(["item1", "item3", "item2"]);

		// Mutating the original's dated entry list doesn't affect the clone.
		field.DateToEntriesDict[new Date(100, 1, 1)].Add(new KeyValuePair<string, object>("remove", "item2"));
		GetValueSet(field, new Date(100, 1, 1)).Should().BeEquivalentTo(["item1"]);
		GetValueSet(clone, new Date(100, 1, 1)).Should().BeEquivalentTo(["item1", "item3", "item2"]);
	}
}