using FluentAssertions;
using ImperatorToCK3.Helpers;
using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.Helpers;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class RakalyCallerTests {
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
}
