using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class ModDefinitionTests {
	[Fact]
	public void IsMatch_ReturnsTrueForNameRegexMatch() {
		var definition = new ModDefinition("tfe", [new Regex("^The Fallen Eagle")], []);
		var mod = new Mod("The Fallen Eagle v2.0", "mod/tfe.mod");

		Assert.True(definition.IsMatch(mod));
	}

	[Fact]
	public void IsMatch_ReturnsFalseForNonMatchingName() {
		var definition = new ModDefinition("tfe", [new Regex("^The Fallen Eagle")], []);
		var mod = new Mod("Some Other Mod", "mod/other.mod");

		Assert.False(definition.IsMatch(mod));
	}

	[Fact]
	public void IsMatch_ReturnsTrueForIdMatch() {
		var definition = new ModDefinition("tfe", [], ["ugc_2243307127.mod"]);
		var mod = new Mod("", "mod/ugc_2243307127.mod");

		Assert.True(definition.IsMatch(mod));
	}

	[Fact]
	public void IsMatch_ReturnsFalseForNonMatchingId() {
		var definition = new ModDefinition("tfe", [], ["ugc_2243307127.mod"]);
		var mod = new Mod("", "mod/ugc_9999999999.mod");

		Assert.False(definition.IsMatch(mod));
	}

	[Fact]
	public void IsMatch_ReturnsTrueWhenEitherConditionMatches() {
		var definition = new ModDefinition("tfe", [new Regex("^The Fallen Eagle")], ["ugc_2243307127.mod"]);
		var modByName = new Mod("The Fallen Eagle v2.0", "mod/unknown.mod");
		var modById = new Mod("Unknown Name", "mod/ugc_2243307127.mod");

		Assert.True(definition.IsMatch(modByName));
		Assert.True(definition.IsMatch(modById));
	}
}

public class ModDefinitionsReaderTests {
	[Fact]
	public void LoadFromFile_ReturnsEmptyListWhenFileNotFound() {
		var definitions = ModDefinitionsReader.LoadFromFile("configurables/nonexistent_mods.txt");
		Assert.Empty(definitions);
	}

	[Fact]
	public void LoadFromFile_LoadsDefinitionsFromFile() {
		const string tempPath = "TestFiles/configurables/test_mods.txt";
		Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
		File.WriteAllText(tempPath,
			"""
			tfe = {
				name_regex = { "^The Fallen Eagle" }
				id = { "ugc_2243307127.mod" }
			}
			wtwsms = {
				name_regex = { "^When the World Stopped Making Sense" }
			}
			""");

		try {
			var definitions = ModDefinitionsReader.LoadFromFile(tempPath);

			Assert.Equal(2, definitions.Count);
			Assert.Equal("tfe", definitions[0].Flag);
			Assert.Equal("wtwsms", definitions[1].Flag);

			// Verify TFE matches by name.
			var tfeMod = new Mod("The Fallen Eagle v2.0", "mod/other.mod");
			Assert.True(definitions[0].IsMatch(tfeMod));

			// Verify TFE matches by ID.
			var tfeModById = new Mod("", "mod/ugc_2243307127.mod");
			Assert.True(definitions[0].IsMatch(tfeModById));

			// Verify WtWSMS matches.
			var wtwsmsMod = new Mod("When the World Stopped Making Sense v4.0", "mod/other.mod");
			Assert.True(definitions[1].IsMatch(wtwsmsMod));
		}
		finally {
			File.Delete(tempPath);
		}
	}
}
