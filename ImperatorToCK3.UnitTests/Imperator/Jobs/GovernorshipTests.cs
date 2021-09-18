using commonItems;
using ImperatorToCK3.Imperator.Jobs;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Jobs {
	public class GovernorshipTests {
		[Fact]
		public void FieldsDefaultToCorrectValues() {
			var reader = new BufferedReader(string.Empty);
			var governorship = new Governorship(reader);
			Assert.Equal((ulong)0, governorship.CountryID);
			Assert.Equal((ulong)0, governorship.CharacterID);
			Assert.Equal(new Date(1, 1, 1), governorship.StartDate);
			Assert.True(string.IsNullOrEmpty(governorship.RegionName));
			Assert.False(governorship.LiegeAdjective);
		}
		[Fact]
		public void FieldsCanBeSet() {
			var reader = new BufferedReader(
				"who=589\n" +
				"character=25212\n" +
				"start_date=450.10.1\n" +
				"governorship = \"galatia_region\""
			);
			var governorship = new Governorship(reader);
			Assert.Equal((ulong)589, governorship.CountryID);
			Assert.Equal((ulong)25212, governorship.CharacterID);
			Assert.Equal(new Date(450, 10, 1), governorship.StartDate);
			Assert.Equal("galatia_region", governorship.RegionName);
		}
	}
}
