using ImperatorToCK3.Helpers;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.Helpers {
	public class RakalyCallerTests {
		[Fact]
		public void RakalyCallerReportsWrongExitCode() {
			const string missingSavePath = "missing.rome";
			var e = Assert.Throws<FormatException>(() => RakalyCaller.MeltSave(missingSavePath));
			Assert.Contains($"Rakaly melter failed to melt {missingSavePath} with exit code 2", e.ToString());
		}
	}
}
