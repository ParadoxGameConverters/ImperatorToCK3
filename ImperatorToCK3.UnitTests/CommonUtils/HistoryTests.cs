using commonItems;
using ImperatorToCK3.CommonUtils;
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

			var provHistoryFactory = new HistoryFactory(
				new() {
					new() { FieldName = "culture", Setter = "culture", InitialValue = "roman" },
					new() { FieldName = "religion", Setter = "religion", InitialValue = "orthodox" },
					new() { FieldName = "holding", Setter = "holding", InitialValue = "none" }
				},
				new()
			);

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

			var provHistoryFactory = new HistoryFactory(
				new() {
					new() { FieldName = "culture", Setter = "culture", InitialValue = null },
					new() { FieldName = "religion", Setter = "religion", InitialValue = null },
					new() { FieldName = "holding", Setter = "holding", InitialValue = "none" }
				},
				new()
			);

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

			var provHistoryFactory = new HistoryFactory(
					new() {
						new() { FieldName = "culture", Setter = "culture", InitialValue = null },
						new() { FieldName = "religion", Setter = "religion", InitialValue = null },
						new() { FieldName = "holding", Setter = "holding", InitialValue = "none" }
					},
					new()
				);

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

			var provHistoryFactory = new HistoryFactory(
					new() {
						new() { FieldName = "culture", Setter = "culture", InitialValue = null },
						new() { FieldName = "religion", Setter = "religion", InitialValue = null },
						new() { FieldName = "holding", Setter = "holding", InitialValue = "none" }
					},
					new()
				);

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

			var provHistoryFactory = new HistoryFactory(
					simpleFieldDefs: new() {
						new() { FieldName = "culture", Setter = "culture", InitialValue = null },
						new() { FieldName = "religion", Setter = "religion", InitialValue = null },
						new() { FieldName = "holding", Setter = "holding", InitialValue = "none" }
					},
					containerFieldDefs: new()
				);

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
	}
}
