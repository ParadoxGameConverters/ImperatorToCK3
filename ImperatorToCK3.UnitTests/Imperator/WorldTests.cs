using System;
using System.IO;
using System.Reflection;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.UnitTests.TestHelpers;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator;

public sealed class WorldTests : IDisposable {
	private readonly string tempRoot;
	private readonly Configuration config;
	private readonly TestImperatorWorld world;

	public WorldTests() {
		tempRoot = Path.Combine(Path.GetTempPath(), "IRToCK3Tests", nameof(WorldTests), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempRoot);

		config = new Configuration {
			ImperatorPath = "TestFiles/Imperator",
			ImperatorDocPath = Path.Combine(tempRoot, "ImperatorDocuments"),
			SaveGamePath = Path.Combine(tempRoot, "test-save.rome")
		};
		Directory.CreateDirectory(config.ImperatorDocPath);
		File.WriteAllText(config.SaveGamePath, string.Empty);

		world = new TestImperatorWorld(config);
	}

	public void Dispose() {
		try {
			Directory.Delete(tempRoot, recursive: true);
		} catch {
			// best effort cleanup
		}
	}

	[Fact]
	public void OutputContinueGameJson_canOverwriteReadOnlyFile() {
		var continueGamePath = Path.Combine(config.ImperatorDocPath, "continue_game.json");
		File.WriteAllText(continueGamePath, "old content");
		File.SetAttributes(continueGamePath, FileAttributes.ReadOnly);

		var result = (bool)InvokeWorldMethod("OutputContinueGameJson", config)!;

		Assert.True(result);
		Assert.Contains("\"title\": \"test-save\"", File.ReadAllText(continueGamePath), StringComparison.Ordinal);
		Assert.False(File.GetAttributes(continueGamePath).HasFlag(FileAttributes.ReadOnly));
	}

	[Fact]
	public void LaunchImperatorToExportCountryFlags_skipsLaunchWhenContinueGameJsonCannotBeWritten() {
		Directory.CreateDirectory(Path.Combine(config.ImperatorDocPath, "continue_game.json"));

		var exception = Record.Exception(() => InvokeWorldMethod("LaunchImperatorToExportCountryFlags", config));

		Assert.Null(exception);
		Assert.False(File.Exists(Path.Combine(config.ImperatorDocPath, "dlc_load.json")));
	}

	private object? InvokeWorldMethod(string methodName, params object[] args) {
		var method = typeof(World).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		return method!.Invoke(world, args);
	}
}
