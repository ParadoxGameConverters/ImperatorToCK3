using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class PillarCollectionTests {
	[Fact]
	public void WarningIsLoggedWhenPillarDataIsMissingType() {
		Directory.CreateDirectory("pillars_test");
		Directory.CreateDirectory("pillars_test/common");
		Directory.CreateDirectory("pillars_test/common/culture");
		Directory.CreateDirectory("pillars_test/common/culture/pillars");
		var pillarsFile = File.CreateText("pillars_test/common/culture/pillars/test_pillars.txt");
		pillarsFile.WriteLine("pillar_without_type = {}");
		pillarsFile.Close();

		OrderedDictionary<string, bool> ck3ModFlags = [];
		var modFS = new ModFilesystem("pillars_test", Array.Empty<Mod>());
		var collection = new PillarCollection(new commonItems.Colors.ColorFactory(), ck3ModFlags);
		
		var consoleOut = new StringWriter();
		Console.SetOut(consoleOut);
		collection.LoadPillars(modFS, ck3ModFlags);
		Assert.Contains("[WARN] Pillar pillar_without_type has no type defined! Skipping.", consoleOut.ToString());
	}

	[Fact]
	public void MissingModFlags_DoNotThrowKeyNotFound() {
		// Empty flags should not throw when validating heritage/language pillars.
		OrderedDictionary<string, bool> emptyFlags = [];
		var collection = new PillarCollection(new commonItems.Colors.ColorFactory(), emptyFlags);
		var modFS = new ModFilesystem("TestFiles/CK3/game", new List<Mod>());

		// Create a temp pillar directory with a heritage pillar lacking params.
		string tempRoot = Path.Combine(Path.GetTempPath(), "PillarMissingFlagTest", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(tempRoot, "common", "culture", "pillars"));
		File.WriteAllText(Path.Combine(tempRoot, "common", "culture", "pillars", "heritage_test.txt"), "heritage_test = { type = heritage color = { 1 2 3 } }");
		var tempModFS = new ModFilesystem(tempRoot, new List<Mod>());
		try {
			Exception? ex = Record.Exception(() => collection.LoadPillars(tempModFS, emptyFlags));
			Assert.Null(ex);
		} finally {
			try { Directory.Delete(tempRoot, recursive: true); } catch { }
		}

		// Also test with flags missing wtwsms/roa/tfe but vanilla_ck3 present
		OrderedDictionary<string, bool> vanillaOnly = new() { ["vanilla_ck3"] = true };
		var collection2 = new PillarCollection(new commonItems.Colors.ColorFactory(), vanillaOnly);
		Exception? ex2 = Record.Exception(() => collection2.LoadPillars(tempModFS, vanillaOnly));
		// Should not throw KeyNotFoundException for wtwsms/roa/tfe
		Assert.True(ex2 is null || ex2 is not System.Collections.Generic.KeyNotFoundException);
	}
}