using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;
using ImperatorToCK3.CommonUtils;
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
				new List<SimpleFieldDef>{
					new() { fieldName = "culture", setter = "culture", initialValue = ""},
					new() { fieldName = "title", setter = "title", initialValue = ""},
					new() { fieldName = "monthly_alien_sightings", setter = "monthly_alien_sightings", initialValue = "0" }
				},
				new List<ContainerFieldStruct>(),
				reader
			);
			var contents = datedHistoryBlock.Contents;

			Assert.Equal(3, contents.simpleFieldContents.Count);

			Assert.Equal("cuman", contents.simpleFieldContents["culture"][0]);
			Assert.Equal("bashkiri", contents.simpleFieldContents["culture"][1]);
			Assert.Equal("c_sarkel", contents.simpleFieldContents["title"].Last());
			Assert.Equal("5", contents.simpleFieldContents["monthly_alien_sightings"].Last());

			Assert.False(contents.simpleFieldContents.ContainsKey("religion"));
			Assert.False(contents.simpleFieldContents.ContainsKey("development"));
		}


		[Fact]
		public void QuotedStringsAreNotReadAsKeys() {
			var reader = new BufferedReader(
				@"= {
					culture = cuman
					""religion"" = jewish
				}"
			);

			var contents = new DatedHistoryBlock(
				new List<SimpleFieldDef> {
					new() { fieldName = "religion", setter = "religion", initialValue = null }
				},
				new List<ContainerFieldStruct>(),
				reader
			).Contents;

			Assert.Empty(contents.simpleFieldContents);
		}
	}
}
