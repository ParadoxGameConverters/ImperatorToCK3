using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.Outputter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class PillarOutputterTests {
	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "PillarOutputter", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void TryDeleteDir(string dir) {
		try {
			if (Directory.Exists(dir)) {
				Directory.Delete(dir, recursive: true);
			}
		} catch {
			// Best-effort cleanup only.
		}
	}

	private static PillarCollection CreatePillarCollectionWithTestPillars() {
		var pillarCollection = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
		pillarCollection.AddOrReplace(new Pillar("heritage_test", new PillarData { Type = "heritage" }));
		pillarCollection.AddOrReplace(new Pillar("language_test", new PillarData { Type = "language" }));
		return pillarCollection;
	}

	[Fact]
	public async Task OutputPillarsWritesPillarsFileAndRemovesOtherPillarFiles() {
		var tempDir = CreateTempDir();
		try {
			var pillarsDir = Path.Combine(tempDir, "common", "culture", "pillars");
			Directory.CreateDirectory(pillarsDir);
			var otherPillarFilePath = Path.Combine(pillarsDir, "some_other_pillars.txt");
			await File.WriteAllTextAsync(otherPillarFilePath, "irrelevant", TestContext.Current.CancellationToken);

			await PillarOutputter.OutputPillars(tempDir, CreatePillarCollectionWithTestPillars());

			var outputFilePath = Path.Combine(pillarsDir, "IRtoCK3_all_pillars.txt");
			Assert.True(File.Exists(outputFilePath));
			var output = await File.ReadAllTextAsync(outputFilePath, TestContext.Current.CancellationToken);
			Assert.Contains("heritage_test=", output, StringComparison.Ordinal);
			Assert.Contains("type=heritage", output, StringComparison.Ordinal);
			Assert.Contains("language_test=", output, StringComparison.Ordinal);
			Assert.Contains("type=language", output, StringComparison.Ordinal);

			Assert.False(File.Exists(otherPillarFilePath), "Pre-existing extra pillar file should have been deleted.");
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputPillarsWithEmptyCollectionStillWritesEmptyFile() {
		var tempDir = CreateTempDir();
		try {
			var pillarsDir = Path.Combine(tempDir, "common", "culture", "pillars");
			Directory.CreateDirectory(pillarsDir);

			var emptyPillars = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
			await PillarOutputter.OutputPillars(tempDir, emptyPillars);

			var outputFilePath = Path.Combine(pillarsDir, "IRtoCK3_all_pillars.txt");
			Assert.True(File.Exists(outputFilePath));
			var output = await File.ReadAllTextAsync(outputFilePath, TestContext.Current.CancellationToken);
			Assert.True(string.IsNullOrWhiteSpace(output));

			Assert.Single(Directory.GetFiles(pillarsDir, "*.txt"));
		} finally {
			TryDeleteDir(tempDir);
		}
	}
}
