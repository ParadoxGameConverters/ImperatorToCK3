using System;
using System.IO;
using System.Reflection;
using ImperatorToCK3.CommonUtils;
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
		SetFileReadOnly(continueGamePath);

		var result = (bool)InvokeWorldMethod("OutputContinueGameJson", config)!;

		Assert.True(result);
		Assert.Contains("\"title\":\t\"test-save\"", File.ReadAllText(continueGamePath), StringComparison.Ordinal);
		// New file must be writable on all platforms.
		Assert.True(IsFileWritable(continueGamePath));
	}

	[Fact]
	public void LaunchImperatorToExportCountryFlags_skipsLaunchWhenContinueGameJsonCannotBeWritten() {
		Directory.CreateDirectory(Path.Combine(config.ImperatorDocPath, "continue_game.json"));

		var exception = Record.Exception(() => InvokeWorldMethod("LaunchImperatorToExportCountryFlags", config));

		Assert.Null(exception);
		Assert.False(File.Exists(Path.Combine(config.ImperatorDocPath, "dlc_load.json")));
	}

	[Fact]
	public void OutputContinueGameJson_createsBackupWhenFileExists() {
		var continueGamePath = Path.Combine(config.ImperatorDocPath, "continue_game.json");
		var originalContent = "original continue_game content";
		File.WriteAllText(continueGamePath, originalContent);

		var result = (bool)InvokeWorldMethod("OutputContinueGameJson", config)!;

		Assert.True(result);
		var backupPath = continueGamePath + ".backup";
		Assert.True(File.Exists(backupPath));
		Assert.Equal(originalContent, File.ReadAllText(backupPath));
		Assert.NotEqual(originalContent, File.ReadAllText(continueGamePath)); // New file should be different
	}

	[Fact]
	public void OutputDlcLoadJson_createsBackupWhenFileExists() {
		var dlcLoadPath = Path.Combine(config.ImperatorDocPath, "dlc_load.json");
		var originalContent = "original dlc_load content";
		File.WriteAllText(dlcLoadPath, originalContent);

		var result = (bool)InvokeWorldMethod("OutputDlcLoadJson", config)!;

		Assert.True(result);
		var backupPath = dlcLoadPath + ".backup";
		Assert.True(File.Exists(backupPath));
		Assert.Equal(originalContent, File.ReadAllText(backupPath));
		Assert.NotEqual(originalContent, File.ReadAllText(dlcLoadPath)); // New file should be different
	}

	[Fact]
	public void ExtractDynamicCoatsOfArms_restoresBackupFilesAfterCompletion() {
		var continueGamePath = Path.Combine(config.ImperatorDocPath, "continue_game.json");
		var dlcLoadPath = Path.Combine(config.ImperatorDocPath, "dlc_load.json");
		var originalContinueContent = "original continue_game";
		var originalDlcContent = "original dlc_load";
		
		// Create original files
		File.WriteAllText(continueGamePath, originalContinueContent);
		File.WriteAllText(dlcLoadPath, originalDlcContent);
		
		// Simulate what OutputContinueGameJson and OutputDlcLoadJson do: backup then write new
		File.Move(continueGamePath, continueGamePath + ".backup", overwrite: true);
		File.WriteAllText(continueGamePath, "modified continue_game");
		
		File.Move(dlcLoadPath, dlcLoadPath + ".backup", overwrite: true);
		File.WriteAllText(dlcLoadPath, "modified dlc_load");
		
		// Call ExtractDynamicCoatsOfArms which should restore files in finally block
		InvokeWorldMethod("ExtractDynamicCoatsOfArms", config);
		
		// Verify backups are restored
		Assert.True(File.Exists(continueGamePath));
		Assert.True(File.Exists(dlcLoadPath));
		Assert.Equal(originalContinueContent, File.ReadAllText(continueGamePath));
		Assert.Equal(originalDlcContent, File.ReadAllText(dlcLoadPath));
		
		// Verify backup files are cleaned up
		Assert.False(File.Exists(continueGamePath + ".backup"));
		Assert.False(File.Exists(dlcLoadPath + ".backup"));
	}

	private object? InvokeWorldMethod(string methodName, params object[] args) {
		var method = typeof(World).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		return method!.Invoke(world, args);
	}

	/// <summary>
	/// Marks a file read-only using the appropriate mechanism for the current OS.
	/// On Windows this sets <see cref="FileAttributes.ReadOnly"/>; on macOS/Linux
	/// it removes the user-write bit via <see cref="UnixFileMode"/>.
	/// </summary>
	private static void SetFileReadOnly(string filePath) {
		if (OperatingSystem.IsWindows()) {
			File.SetAttributes(filePath, FileAttributes.ReadOnly);
		} else {
			var mode = File.GetUnixFileMode(filePath);
			File.SetUnixFileMode(filePath, mode & ~UnixFileMode.UserWrite);
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> when the current user can write to the file.
	/// Uses <see cref="FileHelper.EnsureFileIsWritable"/> indirectly by checking
	/// whether the write bit is present rather than relying on
	/// <see cref="FileAttributes.ReadOnly"/> which has platform-dependent semantics.
	/// </summary>
	private static bool IsFileWritable(string filePath) {
		if (OperatingSystem.IsWindows()) {
			return !File.GetAttributes(filePath).HasFlag(FileAttributes.ReadOnly);
		}
		return File.GetUnixFileMode(filePath).HasFlag(UnixFileMode.UserWrite);
	}
}
