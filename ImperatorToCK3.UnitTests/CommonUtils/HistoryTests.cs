using commonItems;
using ImperatorToCK3.CommonUtils;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils {
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
					new() { fieldName = "culture", setter = "culture", initialValue = null },
					new() { fieldName = "religion", setter = "religion", initialValue = null },
					new() { fieldName = "holding", setter = "holding", initialValue = "none" }
				},
				new() { }
			);

			var provHistory = provHistoryFactory.GetHistory(reader);

			Assert.Null(provHistory.GetSimpleFieldValue("culture", new Date(1, 1, 1)));
			Assert.Null(provHistory.GetSimpleFieldValue("religion", new Date(1, 1, 1)));
			Assert.Equal("none", provHistory.GetSimpleFieldValue("holding", new Date(1, 1, 1)));
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
					new() { fieldName = "culture", setter = "culture", initialValue = null },
					new() { fieldName = "religion", setter = "religion", initialValue = null },
					new() { fieldName = "holding", setter = "holding", initialValue = "none" }
				},
				new() { }
			);

			var provHistory = provHistoryFactory.GetHistory(reader);

			Assert.Equal("khazar", provHistory.GetSimpleFieldValue("culture", new Date(1, 1, 1)));
			Assert.Equal("tengri_pagan", provHistory.GetSimpleFieldValue("religion", new Date(1, 1, 1)));
			Assert.Equal("tribal_holding", provHistory.GetSimpleFieldValue("holding", new Date(1, 1, 1)));
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
						new() { fieldName = "culture", setter = "culture", initialValue = null },
						new() { fieldName = "religion", setter = "religion", initialValue = null },
						new() { fieldName = "holding", setter = "holding", initialValue = "none" }
					},
					new() { }
				);

			var provHistory = provHistoryFactory.GetHistory(reader);

			Assert.Equal("tengri_pagan", provHistory.GetSimpleFieldValue("religion", new Date(750, 1, 1)));
			Assert.Equal("kabarism", provHistory.GetSimpleFieldValue("religion", new Date(750, 1, 2)));
			Assert.Equal("khazar", provHistory.GetSimpleFieldValue("culture", new Date(1000, 1, 1)));
			Assert.Equal("cuman", provHistory.GetSimpleFieldValue("culture", new Date(1000, 1, 3)));
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
						new() { fieldName = "culture", setter = "culture", initialValue = null },
						new() { fieldName = "religion", setter = "religion", initialValue = null },
						new() { fieldName = "holding", setter = "holding", initialValue = "none" }
					},
					new() { }
				);

			var provHistory = provHistoryFactory.GetHistory(reader);

			Assert.Null(provHistory.GetSimpleFieldValue("title", new Date(1000, 1, 1)));
		}
	}
}
