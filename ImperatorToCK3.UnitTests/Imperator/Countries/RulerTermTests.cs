using commonItems;
using ImperatorToCK3.Imperator.Countries;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Countries {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class RulerTermTests {
		[Fact]
		public void IgnoredTokensAreStored() {
			var reader1 = new BufferedReader(
				"character = 69 " +
				"start_date = 500.2.3 " +
				"government = dictatorship " +
				"corruption = unused"
			);
			_ = RulerTerm.Parse(reader1);

			var reader2 = new BufferedReader(
				"character = 69 " +
				"start_date = 500.2.3 " +
				"government = dictatorship " +
				"list = { unused }"
			);
			_ = RulerTerm.Parse(reader2);
			Assert.True(RulerTerm.IgnoredTokens.SetEquals(new HashSet<string>{ "corruption", "list" }));
		}
	}
}
