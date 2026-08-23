using commonItems.Mods;
using ImperatorToCK3.CK3.Legends;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Legends;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class LegendSeedCollectionTests {
	[Fact]
	public void LoadSeeds_ParsesLegendSeedFiles() {
		var testRoot = Path.Combine(Path.GetTempPath(), "LegendSeedCollectionTests", Guid.NewGuid().ToString());
		try {
			var seedsDir = Path.Combine(testRoot, "common", "legends", "legend_seeds");
			Directory.CreateDirectory(seedsDir);
			File.WriteAllText(Path.Combine(seedsDir, "seeds.txt"), "seed_a = { foo = bar }\nseed_b = { baz = qux }");

			var modFs = new ModFilesystem(testRoot, Array.Empty<Mod>());
			var collection = new LegendSeedCollection();
			collection.LoadSeeds(modFs);

			var serialized = collection.Serialize(string.Empty, withBraces: true);
			var seedIds = serialized.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
					.Select(line => line.Split('=', 2)[0].Trim());

			Assert.Contains("seed_a", seedIds);
			Assert.Contains("seed_b", seedIds);
			Assert.Contains("seed_a={ foo = bar }", serialized);
			Assert.Contains("seed_b={ baz = qux }", serialized);
		} finally {
			try {
				Directory.Delete(testRoot, recursive: true);
			} catch {
				// Failure to delete the temp directory can be ignored.
			}
		}
	}

	[Fact]
	public void RemoveAnachronisticSeeds_RemovesListedSeeds() {
		var testRoot = Path.Combine(Path.GetTempPath(), "LegendSeedCollectionTests", Guid.NewGuid().ToString());
		try {
			var seedsDir = Path.Combine(testRoot, "common", "legends", "legend_seeds");
			Directory.CreateDirectory(seedsDir);
			File.WriteAllText(Path.Combine(seedsDir, "seeds.txt"), "seed_a = { foo = bar }\nseed_b = { baz = qux }");

			var removalFile = Path.Combine(testRoot, "remove_seeds.txt");
			File.WriteAllText(removalFile, "seed_a\n");

			var modFs = new ModFilesystem(testRoot, Array.Empty<Mod>());
			var collection = new LegendSeedCollection();
			collection.LoadSeeds(modFs);
			collection.RemoveAnachronisticSeeds(removalFile);

			var serialized = collection.Serialize(string.Empty, withBraces: true);
			Assert.DoesNotContain("seed_a=", serialized);
			Assert.Contains("seed_b=", serialized);
		} finally {
			try {
				Directory.Delete(testRoot, recursive: true);
			} catch {
				// Failure to delete the temp directory can be ignored.
			}
		}
	}
}
