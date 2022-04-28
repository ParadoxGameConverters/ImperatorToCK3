using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils {
	public class DatedHistoryBlockTests {
		[Fact]
		public void OnlyRegisteredThingsAreReturned() {
			var reader = new BufferedReader(
				@" = {
				culture = cuman
				culture = bashkiri
				religion = jewish
				title = ""c_sarkel""
				development = 500
				monthly_alien_sightings = 5
			}");

			var datedHistoryBlock = new DatedHistoryBlock(
				new List<SimpleFieldDef>
				{
					new() {FieldName = "culture", Setters = {"culture"}, InitialValue = ""},
					new() {FieldName = "title", Setters = {"title"}, InitialValue = ""},
					new() {
						FieldName = "monthly_alien_sightings", Setters = {"monthly_alien_sightings"}, InitialValue = "0"
					}
				},
				new List<ContainerFieldDef>(),
				reader
			);

			Assert.Equal(3, datedHistoryBlock.SimpleFieldContents.Count);

			Assert.Equal("cuman", datedHistoryBlock.SimpleFieldContents["culture"][0].Value);
			Assert.Equal("bashkiri", datedHistoryBlock.SimpleFieldContents["culture"][1].Value);
			Assert.Equal("c_sarkel", datedHistoryBlock.SimpleFieldContents["title"].Last().Value);
			Assert.Equal(5, datedHistoryBlock.SimpleFieldContents["monthly_alien_sightings"].Last().Value);

			Assert.False(datedHistoryBlock.SimpleFieldContents.ContainsKey("religion"));
			Assert.False(datedHistoryBlock.SimpleFieldContents.ContainsKey("development"));
		}

		[Fact]
		public void QuotedStringsAreNotReadAsKeys() {
			var reader = new BufferedReader(
				@"= {
					culture = cuman
					""religion"" = jewish
				}"
			);

			var datedHistoryBlock = new DatedHistoryBlock(
				new List<SimpleFieldDef> {
					new() { FieldName = "religion", Setters = {"religion"}, InitialValue = null }
				},
				new List<ContainerFieldDef>(),
				reader
			);

			Assert.Empty(datedHistoryBlock.SimpleFieldContents);
		}
	}
}
