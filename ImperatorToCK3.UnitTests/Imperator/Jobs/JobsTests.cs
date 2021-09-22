using System;
using System.IO;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Jobs {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class JobsTests {
		[Fact]
		public void GovernorshipsDefaultToEmpty() {
			var jobs = new ImperatorToCK3.Imperator.Jobs.Jobs();
			Assert.Empty(jobs.Governorships);
		}
		[Fact]
		public void GovernorshipsCanBeRead() {
			var reader = new BufferedReader(
				"province_job={who=1} province_job={who=2}"
			);
			var jobs = new ImperatorToCK3.Imperator.Jobs.Jobs(reader);
			Assert.Collection(jobs.Governorships,
				item1 => Assert.Equal((ulong)1, item1.CountryID),
				item2 => Assert.Equal((ulong)2, item2.CountryID)
			);
		}
		[Fact]
		public void IgnoredTokensAreLogged() {
			var output = new StringWriter();
			Console.SetOut(output);

			var reader = new BufferedReader(
				"useless_job = {}"
			);
			_ = new ImperatorToCK3.Imperator.Jobs.Jobs(reader);

			Assert.Contains("Ignored Jobs tokens: useless_job", output.ToString());
		}
	}
}
