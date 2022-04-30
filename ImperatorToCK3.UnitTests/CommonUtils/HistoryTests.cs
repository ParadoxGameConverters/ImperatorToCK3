using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using ImperatorToCK3.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class HistoryTests {
	[Fact]
	public void InitialValueIsUsedAsFallback() {
		var reader = new BufferedReader(
		@"= {		#Sarkel
					750.1.2 = {
						religion = kabarism
					}
					1000.1.2 = {
						culture = cuman
					}
				}"
		);

		var provHistoryFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithSimpleField("culture", "culture", "roman")
			.WithSimpleField("religion", "religion", "orthodox")
			.WithSimpleField("holding", "holding", "none")
			.Build();

		var provHistory = provHistoryFactory.GetHistory(reader);

		Assert.Equal("roman", provHistory.GetFieldValue("culture", new Date(1, 1, 1)));
		Assert.Equal("orthodox", provHistory.GetFieldValue("religion", new Date(1, 1, 1)));
		Assert.Equal("none", provHistory.GetFieldValue("holding", new Date(1, 1, 1)));
	}

	[Fact]
	public void InitialValueIsOverriden() {
		var reader = new BufferedReader(
		@"= {		#Sarkel
					culture = khazar
					religion = tengri_pagan
					holding = tribal_holding
					750.1.2 = {
						religion = kabarism
					}
					1000.1.2 = {
						culture = cuman
					}
				}"
		);

		var provHistoryFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithSimpleField("culture", "culture", null)
			.WithSimpleField("religion", "religion", null)
			.WithSimpleField("holding", "holding", "none")
			.Build();

		var provHistory = provHistoryFactory.GetHistory(reader);

		Assert.Equal("khazar", provHistory.GetFieldValue("culture", new Date(1, 1, 1)));
		Assert.Equal("tengri_pagan", provHistory.GetFieldValue("religion", new Date(1, 1, 1)));
		Assert.Equal("tribal_holding", provHistory.GetFieldValue("holding", new Date(1, 1, 1)));
	}

	[Fact]
	public void DatedBlockCanChangeFieldValue() {
		var reader = new BufferedReader(
		@"= {		#Sarkel
					culture = khazar
					religion = tengri_pagan
					holding = tribal_holding
					750.1.2 = {
						religion = kabarism
					}
					1000.1.2 = {
						culture = cuman
					}
				}"
		);

		var provHistoryFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithSimpleField("culture", "culture", null)
			.WithSimpleField("religion", "religion", null)
			.WithSimpleField("holding", "holding", "none")
			.Build();

		var provHistory = provHistoryFactory.GetHistory(reader);

		Assert.Equal("tengri_pagan", provHistory.GetFieldValue("religion", new Date(750, 1, 1)));
		Assert.Equal("kabarism", provHistory.GetFieldValue("religion", new Date(750, 1, 2)));
		Assert.Equal("khazar", provHistory.GetFieldValue("culture", new Date(1000, 1, 1)));
		Assert.Equal("cuman", provHistory.GetFieldValue("culture", new Date(1000, 1, 3)));
	}

	[Fact]
	public void NullIsReturnedForNonExistingField() {
		var reader = new BufferedReader(
		@"= {		#Sarkel
					750.1.2 = {
						religion = kabarism
					}
					1000.1.2 = {
						culture = cuman
					}
				}"
		);

		var provHistoryFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithSimpleField("culture", "culture", null)
			.WithSimpleField("religion", "religion", null)
			.WithSimpleField("holding", "holding", "none")
			.Build();

		var provHistory = provHistoryFactory.GetHistory(reader);

		Assert.Null(provHistory.GetFieldValue("title", new Date(1000, 1, 1)));
	}

	[Fact]
	public void HistoryCanBeReadFromMultipleItems() {
		var reader1 = new BufferedReader(
		@"= {		#Sarkel
					750.1.2 = {
						religion = kabarism
					}
					1000.1.2 = {
						culture = cuman
					}
				}"
		);
		var reader2 = new BufferedReader(
			"= { # also Sarkel\n" +
			"\t800.1.1 = { holding = castle_holding }\n" +
			"}\n"
		);
		var reader3 = new BufferedReader(
			"= { #third time's a charm\n" +
			"\t1100.1.1 = { culture = roman }\n" +
			"}\n"
		);

		var provHistoryFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithSimpleField("culture", "culture", null)
			.WithSimpleField("religion", "religion", null)
			.WithSimpleField("holding", "holding", "none")
			.Build();

		var provHistory = provHistoryFactory.GetHistory(reader1);
		provHistoryFactory.UpdateHistory(provHistory, reader2);
		provHistoryFactory.UpdateHistory(provHistory, reader3);

		var date = new Date(1100, 1, 1);
		Assert.Equal("roman", provHistory.GetFieldValue("culture", date));
		Assert.Equal("kabarism", provHistory.GetFieldValue("religion", date));
		Assert.Equal("castle_holding", provHistory.GetFieldValue("holding", date));
	}

	[Fact]
	public void SimpleFieldValueCanBeAdded() {
		var history = new History();
		history.AddFieldValue(new Date(1, 1, 1),"holder",  "holder", "0"); // new field is created
		history.AddFieldValue(new Date(867, 1, 1), "holder", "holder", "69"); // existing field is updated
		Assert.Collection(history.Fields,
			field => Assert.Equal("holder", field.Id)
		);
		Assert.Collection(history.Fields["holder"].DateToEntriesDict,
			datedBlock => {
				Assert.Equal(new Date(1, 1, 1), datedBlock.Key);
				Assert.Equal("0", datedBlock.Value.Last().Value);
			},
			datedBlock => {
				Assert.Equal(new Date(867, 1, 1), datedBlock.Key);
				Assert.Equal("69", datedBlock.Value.Last().Value);
			}
		);
	}
	[Fact]
	public void ContainerFieldValueCanBeAdded() {
		var history = new History();
		history.AddFieldValue( // new field is created
			new Date(1, 1, 1), "buildings", "buildings", new List<string>());
		history.AddFieldValue(  // existing field is updated
			new Date(867, 1, 1), "buildings", "buildings", new List<string> { "aqueduct", "temple" });
		Assert.Collection(history.Fields,
			item1 => Assert.Equal("buildings", item1.Id)
		);
		Assert.Collection(history.Fields["buildings"].DateToEntriesDict,
			datedBlock => {
				Assert.Equal(new Date(1, 1, 1), datedBlock.Key);
				Assert.Equal(new List<string>(), datedBlock.Value.Last().Value);
			},
			datedBlock => {
				Assert.Equal(new Date(867, 1, 1), datedBlock.Key);
				Assert.Equal(new List<string> { "aqueduct", "temple" }, datedBlock.Value.Last().Value);
			}
		);
	}

	[Fact]
	public void HistoryCanBeSerialized() {
		var fields = new IdObjectCollection<string, IHistoryField> {
			new SimpleHistoryField("holder", setterKeywords: new OrderedSet<string> {"holder"}, null), // simple field with null initial value
			new SimpleHistoryField("culture", setterKeywords: new OrderedSet<string> {"culture"}, "greek"), // simple field with initial value
			new SimpleHistoryField("buildings", setterKeywords: new OrderedSet<string> {"buildings"}, new List<object> {"baths"}) // container field
		};

		var history = new History(fields);

		history.Fields["holder"].AddEntryToHistory(new Date(5, 1, 1), "holder", "nero");

		// Entries with same date should be added to a single date block.
		history.Fields["holder"].AddEntryToHistory(new Date(540, 1, 1), "holder", "justinian");
		history.Fields["culture"].AddEntryToHistory(new Date(540, 1, 1), "culture", "roman");

		// A field can have values of multiple types.
		// Here we're adding a value as a set, while the initial value is a list.
		history.Fields["buildings"].AddEntryToHistory(new Date(2, 1, 1), "buildings",new SortedSet<string> { "aqueduct", "baths" });

		// Date blocks are ordered by date.
		var expectedStr =
			"culture=\"greek\"" + Environment.NewLine +
			"buildings={ \"baths\" }" + Environment.NewLine +
			"2.1.1={ buildings={ \"aqueduct\" \"baths\" } }" + Environment.NewLine +
			"5.1.1={ holder=\"nero\" }" + Environment.NewLine +
			"540.1.1={ holder=\"justinian\" culture=\"roman\" }" + Environment.NewLine;
		Assert.Equal(expectedStr, PDXSerializer.Serialize(history));
	}

	[Fact]
	public void IntegersAreSerializedWithoutQuotes() {
		var fields = new IdObjectCollection<string, IHistoryField>();
		fields.Add(new SimpleHistoryField("development_level", new OrderedSet<string>{"change_development_level"}, 10));

		var history = new History(fields);
		history.Fields["development_level"].AddEntryToHistory(new Date(5, 1, 1), "change_development_level", 20);

		var expectedStr =
			"change_development_level=10" + Environment.NewLine +
			"5.1.1={ change_development_level=20 }" + Environment.NewLine;
		Assert.Equal(expectedStr, PDXSerializer.Serialize(history));
	}

	[Fact]
	public void EmptyListInitialValuesAreNotSerialized() {
		var fields = new IdObjectCollection<string, IHistoryField> {
			new SimpleHistoryField("buildings", new OrderedSet<string> { "buildings" }, new List<object>())
		};

		var history = new History(fields);
		history.Fields["buildings"].AddEntryToHistory( new Date(5, 1, 1), "buildings", new List<object> { "baths" });

		var expectedStr = "5.1.1={ buildings={ \"baths\" } }" + Environment.NewLine;
		Assert.Equal(expectedStr, PDXSerializer.Serialize(history));
	}

	[Fact]
	public void SimpleFieldCanHaveMultipleSetters() {
		var reader = new BufferedReader(
			@"= {
				100.1.1 = {
					holder = ""69""
				}
				200.1.1 = {
					holder_ignore_head_of_faith_requirement = ""420""
				}
			}"
		);

		var provHistoryFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithSimpleField(fieldName: "holder", setters: new OrderedSet<string> { "holder", "holder_ignore_head_of_faith_requirement" }, initialValue: 0)
			.Build();

		var provHistory = provHistoryFactory.GetHistory(reader);

		Assert.Equal("69", provHistory.GetFieldValue("holder", new Date(100, 1, 1)));
		Assert.Equal("420", provHistory.GetFieldValue("holder", new Date(200, 1, 1)));
	}

	[Fact]
	public void ValuesCanBeAddedAndRemovedFromAdditiveField() {
		var reader = new BufferedReader(
			@"= {
				add_trait = dumb
				100.1.1 = {
					add_trait = infertile
				}
				150.1.1 = {
					remove_trait = dumb
				}
			}"
		);

		var characterHistoryFactory = new HistoryFactory.HistoryFactoryBuilder()
			.WithDiffField(fieldName: "traits", inserter: "add_trait", remover: "remove_trait")
			.Build();

		var characterHistory = characterHistoryFactory.GetHistory(reader);

		var traits = characterHistory.GetFieldValueAsCollection("traits", new Date(50, 1, 1));
		Assert.NotNull(traits);
		Assert.Collection(traits,
			trait => Assert.Equal("dumb", trait));

		traits = characterHistory.GetFieldValueAsCollection("traits", new Date(100, 1, 1));
		Assert.NotNull(traits);
		Assert.Collection(traits,
			trait => Assert.Equal("dumb", trait),
			trait => Assert.Equal("infertile", trait));

		traits = characterHistory.GetFieldValueAsCollection("traits", new Date(150, 1, 1));
		Assert.NotNull(traits);
		Assert.Collection(traits,
			trait => Assert.Equal("infertile", trait));
	}
}
