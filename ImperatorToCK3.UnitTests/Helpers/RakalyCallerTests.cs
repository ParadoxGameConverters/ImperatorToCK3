using FluentAssertions;
using ImperatorToCK3.Helpers;
using Newtonsoft.Json.Linq;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.Helpers;

public class RakalyCallerTests {
	[Fact]
	public void RakalyCallerReportsWrongExitCode() {
		const string missingSavePath = "missing.rome";
		var e = Assert.Throws<FormatException>(() => RakalyCaller.MeltSave(missingSavePath));
		Assert.Contains($"Rakaly melter failed to melt {missingSavePath} with exit code 2", e.ToString());
	}

	[Fact]
	public void RakalyCanConvertFileToJsonString() {
		const string filePath = "TestFiles/RakalyCallerTests/00_defines.txt";

		var jsonString = RakalyCaller.GetJson(filePath);
		var jsonObject = JObject.Parse(jsonString);
		
		Assert.Collection(jsonObject.Properties(),
			property => {
				Assert.Equal("NGame", property.Name);
			},
			property => {
				Assert.Equal("NUnit", property.Name);
			});
		
		Assert.Equal("450.10.1", jsonObject["NGame"]?["START_DATE"]);
		jsonObject["NGame"]?["GAME_SPEED_TICKS"].Should().Equal(1, 0.75, 0.5, 0.25, 0.0);
		Assert.Equal(50, jsonObject["NGame"]?["SCORE_START_BASE"]);
		Assert.Equal(0.45, jsonObject["NGame"]?["SCORE_START_POP_WEIGHT"]);
		Assert.Equal(500, jsonObject["NUnit"]?["COHORT_SIZE"]);
	}
	
	[Fact]
	public void RakalyCallerReportsWrongExitCodeWhenConvertingFileToJson() {
		const string missingFilePath = "\"missing.rome\"";
		var e = Assert.Throws<FormatException>(() => RakalyCaller.GetJson(missingFilePath));
		Assert.Contains($"Rakaly failed to convert {missingFilePath} to JSON with exit code 2", e.ToString());
	}
}