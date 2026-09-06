using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using DotLiquid;
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

	[Fact]
	public void InvalidatedPillarIsMergedIntoExistingPillar() {
		var tempRoot = CreateTempPillarsDir(
			"h_other = { type = heritage }\n" +
			"h_old = { type = heritage }\n" +
			"h_new = { REPLACED_BY = { vanilla_ck3 = { h_nonexistent h_old } } type = heritage }\n"
		);
		try {
			OrderedDictionary<string, bool> emptyFlags = [];
			var collection = new PillarCollection(new ColorFactory(), emptyFlags);
			collection.LoadConverterPillars(tempRoot, emptyFlags, new Hash());

			// h_new was invalidated by h_old.
			Assert.Same(collection["h_old"], collection.GetHeritageForId("h_new"));
			Assert.Null(collection.GetHeritageForId("missing_heritage"));

			// Pillars added directly are found by scanning the collection.
			collection.AddOrReplace(new Pillar("h_scan", new PillarData { Type = "heritage" }));
			Assert.Equal("h_scan", collection.GetHeritageForId("h_scan")?.Id);
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void InvalidatingIdsFromInactiveModFlagAreIgnored() {
		var tempRoot = CreateTempPillarsDir(
			"h_old = { type = heritage }\n" +
			"h_new = { REPLACED_BY = { mymod = { h_old } } type = heritage }\n"
		);
		try {
			OrderedDictionary<string, bool> flags = new() { ["mymod"] = false };
			var collection = new PillarCollection(new ColorFactory(), flags);
			collection.LoadConverterPillars(tempRoot, flags, new Hash());

			// mymod is inactive, so h_new is loaded normally instead of being merged.
			Assert.Equal("h_new", collection.GetHeritageForId("h_new")?.Id);
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void InvalidatingIdsFromActiveModFlagAreApplied() {
		var tempRoot = CreateTempPillarsDir(
			"h_old = { type = heritage }\n" +
			"h_new = { REPLACED_BY = { mymod = { h_old } } type = heritage }\n"
		);
		try {
			OrderedDictionary<string, bool> flags = new() { ["mymod"] = true };
			var collection = new PillarCollection(new ColorFactory(), flags);
			collection.LoadConverterPillars(tempRoot, flags, new Hash());

			Assert.Same(collection["h_old"], collection.GetHeritageForId("h_new"));
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void GetLanguageForId_FindsScannedAndCachedPillars() {
		var collection = new PillarCollection(new ColorFactory(), []);
		collection.AddOrReplace(new Pillar("lang_x", new PillarData { Type = "language" }));

		// First call scans the collection, second call uses the cache.
		Assert.Equal("lang_x", collection.GetLanguageForId("lang_x")?.Id);
		Assert.Equal("lang_x", collection.GetLanguageForId("lang_x")?.Id);
		Assert.Null(collection.GetLanguageForId("missing_language"));
	}

	[Theory]
	[InlineData("wtwsms")]
	[InlineData("tfe")]
	[InlineData("roa")]
	public void HeritageWithoutRequiredParametersLogsWarnings(string activeFlag) {
		var tempRoot = CreateTempPillarsDir("h_test = { type = heritage }\n");
		try {
			OrderedDictionary<string, bool> flags = new() { [activeFlag] = true };
			var collection = new PillarCollection(new ColorFactory(), flags);

			var consoleOut = new StringWriter();
			Console.SetOut(consoleOut);
			collection.LoadConverterPillars(tempRoot, flags, new Hash());

			var log = consoleOut.ToString();
			Assert.Contains("Heritage h_test is missing required heritage_family parameter!", log);
			Assert.Contains("Heritage h_test is missing required heritage_group parameter!", log);
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Theory]
	[InlineData("wtwsms")]
	[InlineData("tfe")]
	[InlineData("roa")]
	public void LanguageWithoutRequiredParametersLogsWarnings(string activeFlag) {
		var tempRoot = CreateTempPillarsDir("l_test = { type = language }\n");
		try {
			OrderedDictionary<string, bool> flags = new() { [activeFlag] = true };
			var collection = new PillarCollection(new ColorFactory(), flags);

			var consoleOut = new StringWriter();
			Console.SetOut(consoleOut);
			collection.LoadConverterPillars(tempRoot, flags, new Hash());

			var log = consoleOut.ToString();
			if (activeFlag == "tfe") {
				Assert.Contains("Language l_test is missing required language_family parameter!", log);
				Assert.Contains("Language l_test is missing required language_group parameter!", log);
			} else {
				Assert.Contains("Language l_test is missing required language_family parameter!", log);
				Assert.Contains("Language l_test is missing required language_branch parameter!", log);
			}
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void HeritageWithOnlyGroupParameterSkipsGroupWarning() {
		var tempRoot = CreateTempPillarsDir(
			"h_test = { type = heritage parameters = { heritage_group_test = yes } }\n");
		try {
			OrderedDictionary<string, bool> flags = new() { ["tfe"] = true };
			var collection = new PillarCollection(new ColorFactory(), flags);

			var consoleOut = new StringWriter();
			Console.SetOut(consoleOut);
			collection.LoadConverterPillars(tempRoot, flags, new Hash());

			var log = consoleOut.ToString();
			Assert.Contains("Heritage h_test is missing required heritage_family parameter!", log);
			Assert.DoesNotContain("heritage_group", log);
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void HeritageWithOnlyFamilyParameterSkipsFamilyWarning() {		var tempRoot = CreateTempPillarsDir(
			"h_test = { type = heritage parameters = { heritage_family_test = yes } }\n");
		try {
			OrderedDictionary<string, bool> flags = new() { ["tfe"] = true };
			var collection = new PillarCollection(new ColorFactory(), flags);

			var consoleOut = new StringWriter();
			Console.SetOut(consoleOut);
			collection.LoadConverterPillars(tempRoot, flags, new Hash());

			var log = consoleOut.ToString();
			Assert.DoesNotContain("heritage_family", log);
			Assert.Contains("Heritage h_test is missing required heritage_group parameter!", log);
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void LanguageWithAllParametersLogsNoWarnings() {
		var tempRoot = CreateTempPillarsDir(
			"l_test = { type = language parameters = { language_family_test = yes language_branch_test = yes language_group_test = yes } }\n");
		try {
			OrderedDictionary<string, bool> flags = new() { ["wtwsms"] = true, ["tfe"] = true };
			var collection = new PillarCollection(new ColorFactory(), flags);

			var consoleOut = new StringWriter();
			Console.SetOut(consoleOut);
			collection.LoadConverterPillars(tempRoot, flags, new Hash());

			Assert.DoesNotContain("missing required", consoleOut.ToString());
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void PillarOfOtherTypeSkipsHeritageAndLanguageValidation() {
		var tempRoot = CreateTempPillarsDir("p_test = { type = ethnicity }\n");
		try {
			OrderedDictionary<string, bool> flags = new() { ["tfe"] = true };
			var collection = new PillarCollection(new ColorFactory(), flags);

			var consoleOut = new StringWriter();
			Console.SetOut(consoleOut);
			collection.LoadConverterPillars(tempRoot, flags, new Hash());

			Assert.DoesNotContain("missing required", consoleOut.ToString());
			Assert.Equal("ethnicity", collection["p_test"].Type);
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	private static string CreateTempPillarsDir(string fileContent) {		string tempRoot = Path.Combine(Path.GetTempPath(), "PillarCollectionTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempRoot);
		File.WriteAllText(Path.Combine(tempRoot, "pillars.txt"), fileContent);
		return tempRoot;
	}
}