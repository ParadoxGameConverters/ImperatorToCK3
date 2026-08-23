using AwesomeAssertions;
using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

[Collection("Sequential")]
public class LiteralHistoryFieldTests {
	[Fact]
	public void ConstructorAddsInitialEntryWithFirstSetterWhenInitialValueProvided() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, "initialValue");

		Assert.Equal("name", field.Id);
		var entry = Assert.Single(field.InitialEntries);
		Assert.Equal(new KeyValuePair<string, object>("name", "initialValue"), entry);
	}

	[Fact]
	public void ConstructorLeavesInitialEntriesEmptyWhenInitialValueNotProvided() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, initialValue: null);

		Assert.Empty(field.InitialEntries);
		Assert.Null(field.GetValue(null));
	}

	[Fact]
	public void ConstructorThrowsWhenInitialValueProvidedButNoSetters() {
		Assert.Throws<InvalidOperationException>(() =>
			new LiteralHistoryField("name", new OrderedSet<string>(), "initialValue")
		);
	}

	[Fact]
	public void GetValueReturnsLastInitialEntryWhenDateIsNull() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, "initial1");
		field.AddEntryToHistory(null, "name", "initial2");
		field.AddEntryToHistory(new Date(100, 1, 1), "name", "dated");

		Assert.Equal("initial2", field.GetValue(null)?.ToString());
	}

	[Fact]
	public void GetValueReturnsLastEntryOnOrBeforeGivenDate() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, "initial");
		field.AddEntryToHistory(new Date(100, 1, 1), "name", "first");
		field.AddEntryToHistory(new Date(200, 1, 1), "name", "second");
		field.AddEntryToHistory(new Date(200, 1, 1), "name", "second-late");

		Assert.Equal("first", field.GetValue(new Date(150, 1, 1))?.ToString());
		Assert.Equal("first", field.GetValue(new Date(199, 1, 1))?.ToString());
		Assert.Equal("second-late", field.GetValue(new Date(200, 1, 1))?.ToString());
	}

	[Fact]
	public void GetValueFallsBackToInitialEntryWhenNoDatedEntriesOnOrBeforeDate() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, "initial");
		field.AddEntryToHistory(new Date(100, 1, 1), "name", "future");

		Assert.Equal("initial", field.GetValue(new Date(50, 1, 1))?.ToString());
	}

	[Fact]
	public void GetValueReturnsNullWhenNoEntriesExist() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, initialValue: null);

		Assert.Null(field.GetValue(null));
		Assert.Null(field.GetValue(new Date(100, 1, 1)));
	}

	[Fact]
	public void AddEntryToHistoryAddsInitialEntryWhenDateIsNull() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, initialValue: null);

		field.AddEntryToHistory(null, "name", "value1");
		field.AddEntryToHistory(null, "name", "value2");

		Assert.Equal(2, field.InitialEntries.Count);
		Assert.Equal("value2", field.GetValue(null)?.ToString());
	}

	[Fact]
	public void AddEntryToHistoryAddsDatedEntriesToExistingDateBlock() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, initialValue: null);

		field.AddEntryToHistory(new Date(100, 1, 1), "name", "first");
		field.AddEntryToHistory(new Date(100, 1, 1), "name", "second");
		field.AddEntryToHistory(new Date(200, 1, 1), "name", "third");

		Assert.Equal(2, field.DateToEntriesDict[new Date(100, 1, 1)].Count);
		Assert.Single(field.DateToEntriesDict[new Date(200, 1, 1)]);
		Assert.Equal("second", field.GetValue(new Date(100, 1, 1))?.ToString());
		Assert.Equal("third", field.GetValue(new Date(200, 1, 1))?.ToString());
	}

	[Fact]
	public void AddEntryToHistoryWithUnknownSetterWarnsButStillAdds() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, initialValue: null);
		var output = new StringWriter();
		Console.SetOut(output);

		field.AddEntryToHistory(new Date(100, 1, 1), "badSetter", "value");

		var logStr = output.ToString();
		Assert.Contains("[WARN] Setter badSetter does not belong to history field's setters!", logStr);

		// The entry is added despite the warning.
		Assert.Equal("value", field.GetValue(new Date(100, 1, 1))?.ToString());
	}

	[Fact]
	public void EntriesCountIncludesInitialAndDatedEntries() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, "initial");
		field.AddEntryToHistory(new Date(100, 1, 1), "name", "first");
		field.AddEntryToHistory(new Date(100, 1, 1), "name", "second");
		field.AddEntryToHistory(new Date(200, 1, 1), "name", "third");

		Assert.Equal(4, field.EntriesCount);
	}

	[Fact]
	public void RegexReplaceAllEntriesReplacesInInitialEntriesWithReplacement() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, "root_top_liege");

		field.RegexReplaceAllEntries(new Regex("root_"), "R_");

		Assert.Equal("R_top_liege", field.GetValue(null)?.ToString());
	}

	[Fact]
	public void RegexReplaceAllEntriesReplacesInDatedEntriesWithEmptyString() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, initialValue: null);
		field.AddEntryToHistory(new Date(100, 1, 1), "name", "root_capital_province");

		field.RegexReplaceAllEntries(new Regex("root_"), "R_");

		// Note: dated entries are replaced with an empty string, unlike initial entries.
		Assert.Equal("capital_province", field.GetValue(new Date(100, 1, 1))?.ToString());
	}

	[Fact]
	public void RegexReplaceAllEntriesLeavesNonStringValuesUntouched() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, initialValue: null);
		field.AddEntryToHistory(new Date(200, 1, 1), "name", 123);

		field.RegexReplaceAllEntries(new Regex("root_"), "R_");

		Assert.Equal(123, field.GetValue(new Date(200, 1, 1)));
	}

	[Fact]
	public void SetterKeywordsCanBeParsedInDatedBlock() {
		var field = new LiteralHistoryField("holding", new OrderedSet<string> { "holding" }, initialValue: null);
		var parser = new Parser(implicitVariableHandling: true);
		field.RegisterKeywords(parser, new Date(100, 1, 1));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(new BufferedReader("= { holding = tribal_holding }"));

		Assert.Equal("tribal_holding", field.GetValue(new Date(100, 1, 1))?.ToString());
	}

	[Fact]
	public void ConditionalOperatorIsSupportedWhenParsing() {
		var field = new LiteralHistoryField("holding", new OrderedSet<string> { "holding" }, initialValue: null);
		var parser = new Parser(implicitVariableHandling: true);
		field.RegisterKeywords(parser, new Date(100, 1, 1));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(new BufferedReader("= { holding ?= tribal_holding }"));

		Assert.Equal("tribal_holding", field.GetValue(new Date(100, 1, 1))?.ToString());
	}

	[Fact]
	public void InitialEntriesForSerializationReturnsInitialEntries() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, "initial");
		field.AddEntryToHistory(null, "name", "second");

		var serializedEntries = field.InitialEntriesForSerialization.ToList();

		Assert.Equal(2, serializedEntries.Count);
		Assert.Equal("name", serializedEntries[0].Key);
		Assert.Equal("initial", serializedEntries[0].Value);
		Assert.Equal("second", serializedEntries[1].Value);
	}

	[Fact]
	public void ClonedFieldIsIndependentDeepCopy() {
		var field = new LiteralHistoryField("name", new OrderedSet<string> { "name" }, "initial");
		field.AddEntryToHistory(new Date(100, 1, 1), "name", "first");
		var clone = (LiteralHistoryField)field.Clone();

		Assert.Equal(field.Id, clone.Id);
		Assert.Equal("first", clone.GetValue(new Date(100, 1, 1))?.ToString());

		// Adding entries to the clone doesn't affect the original.
		clone.AddEntryToHistory(new Date(200, 1, 1), "name", "second");
		Assert.Equal("second", clone.GetValue(new Date(200, 1, 1))?.ToString());
		Assert.Equal("first", field.GetValue(new Date(200, 1, 1))?.ToString());

		// Mutating the original's dated entry list doesn't affect the clone.
		field.DateToEntriesDict[new Date(100, 1, 1)].Add(new("name", "first-late"));
		Assert.Equal("first-late", field.GetValue(new Date(100, 1, 1))?.ToString());
		Assert.Equal("first", clone.GetValue(new Date(100, 1, 1))?.ToString());

		// Mutating the original's initial entries doesn't affect the clone.
		field.InitialEntries.Add(new("name", "initial-late"));
		Assert.Equal("initial-late", field.GetValue(null)?.ToString());
		Assert.Equal("initial", clone.GetValue(null)?.ToString());
	}
}