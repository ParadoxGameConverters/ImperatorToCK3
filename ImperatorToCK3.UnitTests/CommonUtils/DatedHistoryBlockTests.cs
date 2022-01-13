﻿using commonItems;
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
					new() {FieldName = "culture", Setter = "culture", InitialValue = ""},
					new() {FieldName = "title", Setter = "title", InitialValue = ""},
					new() {
						FieldName = "monthly_alien_sightings", Setter = "monthly_alien_sightings", InitialValue = "0"
					}
				},
				new List<ContainerFieldDef>(),
				reader
			);
			var contents = datedHistoryBlock.Contents;

			Assert.Equal(3, contents.SimpleFieldContents.Count);

			Assert.Equal("cuman", contents.SimpleFieldContents["culture"][0]);
			Assert.Equal("bashkiri", contents.SimpleFieldContents["culture"][1]);
			Assert.Equal("c_sarkel", contents.SimpleFieldContents["title"].Last());
			Assert.Equal(5, contents.SimpleFieldContents["monthly_alien_sightings"].Last());

			Assert.False(contents.SimpleFieldContents.ContainsKey("religion"));
			Assert.False(contents.SimpleFieldContents.ContainsKey("development"));
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
					new() { FieldName = "religion", Setter = "religion", InitialValue = null }
				},
				new List<ContainerFieldDef>(),
				reader
			).Contents;

			Assert.Empty(contents.SimpleFieldContents);
		}
	}
}
