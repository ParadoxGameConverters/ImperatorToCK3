using AwesomeAssertions;
using ImperatorToCK3.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Helpers;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]

public class RakalyCallerTests : IDisposable {
	private readonly string tempRoot;

	public RakalyCallerTests() {
		tempRoot = Path.Combine(Path.GetTempPath(), "IRToCK3Tests", nameof(RakalyCallerTests), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempRoot);
	}

	public void Dispose() {
		try {
			Directory.Delete(tempRoot, recursive: true);
		} catch {
			// best effort cleanup
		}
	}

	[Fact]
	public void RakalyCallerReportsWrongExitCode() {
		var stdOut = new StringWriter();
		Console.SetOut(stdOut);
		
		const string missingSavePath = "missing.rome";
		var e = Assert.Throws<FormatException>(() => RakalyCaller.MeltSave(missingSavePath));
		Assert.Contains("Rakaly melter failed to melt the save", e.ToString());
		
		var stdErrString = stdOut.ToString();
		Assert.Contains("Save path: missing.rome", stdErrString);
		Assert.Contains("Rakaly exit code: 2", stdErrString);
	}

	[Fact]
	public void RakalyCanConvertFileToJsonString() {
		const string filePath = "TestFiles/RakalyCallerTests/00_defines.txt";

		var jsonString = RakalyCaller.GetJson(filePath);
		var jsonRoot = JsonDocument.Parse(jsonString).RootElement;

		Assert.Collection(jsonRoot.EnumerateObject(),
			property => {
				Assert.Equal("NGame", property.Name);
			},
			property => {
				Assert.Equal("NUnit", property.Name);
			});

		Assert.Equal("450.10.1", jsonRoot.GetProperty("NGame").GetProperty("START_DATE").GetString());
		jsonRoot
			.GetProperty("NGame")
			.GetProperty("GAME_SPEED_TICKS")
			.EnumerateArray()
			.Select(element => element.GetDouble())
			.Should()
			.Equal(1, 0.75, 0.5, 0.25, 0.0);
		Assert.Equal(50, jsonRoot.GetProperty("NGame").GetProperty("SCORE_START_BASE").GetDouble());
		Assert.Equal(0.45, jsonRoot.GetProperty("NGame").GetProperty("SCORE_START_POP_WEIGHT").GetDouble());
		Assert.Equal(500, jsonRoot.GetProperty("NUnit").GetProperty("COHORT_SIZE").GetInt32());
	}

	[Fact]
	public void RakalyCallerReportsWrongExitCodeWhenConvertingFileToJson() {
		const string missingFilePath = "\"missing.rome\"";
		var e = Assert.Throws<FormatException>(() => RakalyCaller.GetJson(missingFilePath));
		Assert.Contains($"Rakaly failed to convert {missingFilePath} to JSON with exit code 2", e.ToString());
	}

	[Fact]
	public void NormalizeMeltedSaveForNonIronman_modifiesOnlyRelevantMetadataInFirst200Lines() {
		var meltedSavePath = Path.Combine(tempRoot, "melted_save.rome");
		var content = string.Join('\n',
			new[] {
				"header=yes",
				"ironman=yes",
				"iron=yes",
				"\tironman=yes",
				"\tironman_cloud=yes",
				"\tironman_save_name=\"My Save\"",
				"keep=this"
			}.Concat(Enumerable.Range(8, 193).Select(i => $"line{i}=value")).Concat(new[] {
				"ironman=yes",
				"\tironman_cloud=yes",
				"\tironman_save_name=\"Late Save\""
			})) + "\n";
		File.WriteAllText(meltedSavePath, content);

		InvokeRakalyCallerStaticMethod("NormalizeMeltedSaveForNonIronman", meltedSavePath);

		var normalizedContent = File.ReadAllText(meltedSavePath);
		var lines = normalizedContent.Split('\n', StringSplitOptions.None);

		Assert.DoesNotContain("iron=yes", lines[..200]);
		Assert.Equal(1, lines.Count(line => line == "ironman=yes"));
		Assert.DoesNotContain("\tironman=yes", lines[..200]);
		Assert.Contains("\tironman_cloud=no", lines[..200]);
		Assert.Contains("\tironman_save_name=\"\"", lines[..200]);
		Assert.Contains("keep=this", lines[..200]);
		Assert.Contains("\tironman_cloud=yes", lines);
		Assert.Contains("\tironman_save_name=\"Late Save\"", lines);
	}

	[Fact]
	public async Task OpenReadableFileWithRetries_waitsForExclusiveLockToClear() {
		var filePath = Path.Combine(tempRoot, "locked.rome");
		File.WriteAllText(filePath, "content");

		var lockingStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		var releaseTask = Task.Run(async () => {
			await Task.Delay(150, TestContext.Current.CancellationToken);
			lockingStream.Dispose();
		}, TestContext.Current.CancellationToken);

		using var reopenedStream = (FileStream)InvokeRakalyCallerStaticMethod("OpenReadableFileWithRetries", filePath, 20, 25)!;
		await releaseTask.WaitAsync(TestContext.Current.CancellationToken);

		Assert.NotNull(reopenedStream);
		Assert.True(reopenedStream.CanRead);
	}

	private static object? InvokeRakalyCallerStaticMethod(string methodName, params object[] args) {
		var method = typeof(RakalyCaller).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
		Assert.NotNull(method);
		return method!.Invoke(null, args);
	}
}
