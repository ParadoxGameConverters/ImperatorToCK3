using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CommonUtils;
using System;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils {
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
			history.AddFieldValue("holder", "0", new Date(1, 1, 1), "holder"); // new field is created
			history.AddFieldValue("holder", "69", new Date(867, 1, 1), "holder"); // existing field is updated
			Assert.Collection(history.Fields,
				item1 => Assert.Equal("holder", item1.Key)
			);
			Assert.Collection(history.Fields["holder"].ValueHistory,
				item1 => {
					Assert.Equal(new Date(1, 1, 1), item1.Key);
					Assert.Equal("0", item1.Value);
				},
				item2 => {
					Assert.Equal(new Date(867, 1, 1), item2.Key);
					Assert.Equal("69", item2.Value);
				}
			);
		}
		[Fact]
		public void ContainerFieldValueCanBeAdded() {
			var history = new History();
			history.AddFieldValue( // new field is created
				"buildings",
				new List<string>(),
				new Date(1, 1, 1),
				"buildings"
			);
			history.AddFieldValue(  // existing field is updated
				"buildings",
				new List<string> { "aqueduct", "temple" },
				new Date(867, 1, 1),
				"buildings"
			);
			Assert.Collection(history.Fields,
				item1 => Assert.Equal("buildings", item1.Key)
			);
			Assert.Collection(history.Fields["buildings"].ValueHistory,
				item1 => {
					Assert.Equal(new Date(1, 1, 1), item1.Key);
					Assert.Equal(new List<string>(), item1.Value);
				},
				item2 => {
					Assert.Equal(new Date(867, 1, 1), item2.Key);
					Assert.Equal(new List<string> { "aqueduct", "temple" }, item2.Value);
				}
			);
		}

		[Fact]
		public void HistoryCanBeSerialized() {
			var fields = new Dictionary<string, HistoryField> {
				{"holder", new HistoryField("holder", null)}, // simple field with null initial value
				{"culture", new HistoryField("culture", "roman")}, // simple field with initial value
				{"buildings", new HistoryField("buildings", new List<object> {"baths"})} // container field
			};
			var history = new History(fields);

			history.Fields["holder"].AddValueToHistory("nero", new Date(5, 1, 1));

			// Entries with same date should be added to a single date block.
			history.Fields["holder"].AddValueToHistory("justinian", new Date(540, 1, 1));
			history.Fields["culture"].AddValueToHistory("better_roman", new Date(540, 1, 1));

			// A field can have values of multiple types.
			// Here we're adding a value as a set, while the initial value is a list.
			history.Fields["buildings"].AddValueToHistory(new SortedSet<string> { "aqueduct", "baths" }, new Date(2, 1, 1));

			// Date blocks are ordered by date.
			var expectedStr =
				"culture=\"roman\"" + Environment.NewLine +
				"buildings={ \"baths\" }" + Environment.NewLine +
				"2.1.1={" + Environment.NewLine +
				"\tbuildings={ \"aqueduct\" \"baths\" }" + Environment.NewLine +
				"}" + Environment.NewLine +
				"5.1.1={" + Environment.NewLine +
				"\tholder=\"nero\"" + Environment.NewLine +
				"}" + Environment.NewLine +
				"540.1.1={" + Environment.NewLine +
				"\tholder=\"justinian\"" + Environment.NewLine +
				"\tculture=\"better_roman\"" + Environment.NewLine +
				"}" + Environment.NewLine;
			Assert.Equal(expectedStr, PDXSerializer.Serialize(history));
		}

		[Fact]
		public void IntegersAreSerializedWithoutQuotes() {
			var fields = new Dictionary<string, HistoryField> {
				{"development_level", new HistoryField("change_development_level", 10)}
			};
			var history = new History(fields);
			history.Fields["development_level"].AddValueToHistory(20, new Date(5, 1, 1));

			var expectedStr =
				"change_development_level=10" + Environment.NewLine +
				"5.1.1={" + Environment.NewLine +
				"\tchange_development_level=20" + Environment.NewLine +
				"}" + Environment.NewLine;
			Assert.Equal(expectedStr, PDXSerializer.Serialize(history));
		}

		[Fact]
		public void EmptyListInitialValuesAreNotSerialized() {
			var fields = new Dictionary<string, HistoryField> {
				{"buildings", new HistoryField("buildings", new List<object>())} // container field initially empty
			};
			var history = new History(fields);
			history.Fields["buildings"].AddValueToHistory(new List<object> { "baths" }, new Date(5, 1, 1));

			var expectedStr =
				"5.1.1={" + Environment.NewLine +
				"\tbuildings={ \"baths\" }" + Environment.NewLine +
				"}" + Environment.NewLine;
			Assert.Equal(expectedStr, PDXSerializer.Serialize(history));
		}
	}
}
