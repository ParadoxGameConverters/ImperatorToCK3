using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Localization;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CK3LocDBTests {
	[Theory]
	// https://en.wikipedia.org/wiki/MurmurHash
	[InlineData("", 0)]
	[InlineData("test", 0xba6bd213)]
	[InlineData("Hello, world!", 0xc0363e43)]
	[InlineData("The quick brown fox jumps over the lazy dog", 0x2e4ff723)]
	public void MurmurHashIsCorrectlyCalculated(string key, uint expectedHash) {
		var actualHash = CK3LocDB.GetHashForKey(key);
		Assert.Equal(expectedHash, actualHash);
	}

	[Theory]
	[InlineData("Mallobald", "laamp_base_contract_schemes.2541.e.tt.employer_has_trait.paranoid")]
	[InlineData("dynn_Hkeng", "debug_min_popular_opinion_modifier")]
	[InlineData("b_hinggan_adj", "grand_wedding_completed_guest")]
	[InlineData("c_biak_adj", "b_celtzene")]
	[InlineData("b_molungr_adj", "c_somkhiti")]
	[InlineData("BrewPositiveAdjectiveSpectacular", "duchy_theo_cath_andalusian")]
	[InlineData("childhood.2200.desc", "b_dezful_adj")]
	[InlineData("khabzism_devoteeplural", "caballero_flavor")]
	[InlineData("building_nishapur_mines_02", "k_IRTOCK3_ATV_adj")]
	public void HashCollisionsAreDetected(string key1, string key2) {
		 var locDB = new TestCK3LocDB();
		 locDB.AddLocForLanguage(key1, language: "english", string.Empty);
		 Assert.True(locDB.KeyHasConflictingHash(key2));
	}
	
	[Theory]
	[InlineData("a", "b")]
	[InlineData("key1", "key2")]
	[InlineData("Mallobald", "laamp_base_contract_schemes.2541")]
	[InlineData("dynn_Hkeng", "dynn_Heng")]
	[InlineData("b_hinggan_adj", "b_hinggan_adj2")]
	[InlineData("c_biak_adj", "c_biak_adj2")]
	[InlineData("b_molungr_adj", "b_molungr_adj2")]
	[InlineData("BrewPositiveAdjectiveSpectacular", "BrewPositiveAdjectiveSpectacular2")]
	[InlineData("childhood.2200.desc", "childhood.2200.desc2")]
	[InlineData("khabzism_devoteeplural", "khabzism_devoteeplural2")]
	public void FalseHashCollisionsAreNotDetected(string key1, string key2) {
		 var locDB = new TestCK3LocDB();
		 locDB.AddLocForLanguage(key1, language: "english", string.Empty);
		 Assert.False(locDB.KeyHasConflictingHash(key2));
	}

	[Fact]
	public void GetOrCreateLocBlock_ReturnsExistingBlock() {
		var locDB = new TestCK3LocDB();
		locDB.AddLocForLanguage("test_key", "english", "Hello");

		var block = locDB.GetOrCreateLocBlock("test_key");

		Assert.Same(locDB.GetLocBlockForKey("test_key"), block);
		Assert.Equal("Hello", block["english"]);
	}

	[Fact]
	public void GetLocBlockForKey_ReturnsNullForMissingKey() {
		var locDB = new TestCK3LocDB();

		Assert.Null(locDB.GetLocBlockForKey("missing_key"));
	}

	[Fact]
	public void HasKeyLocForLanguage_ChecksKeyAndLanguage() {
		var locDB = new TestCK3LocDB();
		locDB.AddLocForLanguage("test_key", "english", "Hello");

		Assert.True(locDB.HasKeyLocForLanguage("test_key", "english"));
		Assert.False(locDB.HasKeyLocForLanguage("test_key", "french"));
		Assert.False(locDB.HasKeyLocForLanguage("missing_key", "english"));
	}

	[Fact]
	public void GetYmlLocLineForLanguage_ReturnsLineOrNull() {
		var locDB = new TestCK3LocDB();
		locDB.AddLocForLanguage("test_key", "english", "Hello");

		Assert.Equal(" test_key: \"Hello\"", locDB.GetYmlLocLineForLanguage("test_key", "english"));
		Assert.Null(locDB.GetYmlLocLineForLanguage("test_key", "french"));
		Assert.Null(locDB.GetYmlLocLineForLanguage("missing_key", "english"));
	}

	[Fact]
	public void HashCollisionWarningIsLoggedWhenCollidingKeysAreAdded() {
		var locDB = new TestCK3LocDB();
		locDB.AddLocForLanguage("Mallobald", "english", string.Empty);

		var output = new StringWriter();
		Console.SetOut(output);
		locDB.AddLocForLanguage(
			"laamp_base_contract_schemes.2541.e.tt.employer_has_trait.paranoid", "english", string.Empty);

		Assert.Contains("Hash collision detected for loc key", output.ToString());
		Assert.NotNull(locDB.GetLocBlockForKey("Mallobald"));
	}

	[Fact]
	public void LocIsLoadedFromModFilesystem() {		var modFS = new ModFilesystem("TestFiles/CK3LocDBTests/game", new List<Mod>());

		var locDB = new CK3LocDB(modFS, Array.Empty<string>());

		Assert.Equal("Hello", locDB.GetLocBlockForKey("test_key_1")!["english"]);
		Assert.Equal(CK3LocType.CK3ModFS, locDB.GetLocBlockForKey("test_key_1")?.GetLocTypeForLanguage("english"));
		Assert.Equal("World", locDB.GetLocBlockForKey("test_key_2")!["english"]);
	}

	[Fact]
	public void NullLocsAreSkippedWhenImportingFromLocDB() {
		var locDB = new TestCK3LocDB();
		var sourceLocDB = new LocDB("english");
		var sourceBlock = sourceLocDB.AddLocBlock("test_key");
		sourceBlock["english"] = "Hello";
		sourceBlock["french"] = null;

		var importMethod = typeof(CK3LocDB).GetMethod("ImportLocFromLocDB",
			BindingFlags.NonPublic | BindingFlags.Instance)!;
		importMethod.Invoke(locDB, [sourceLocDB]);

		Assert.Equal("Hello", locDB.GetLocBlockForKey("test_key")!["english"]);
		Assert.False(locDB.HasKeyLocForLanguage("test_key", "french"));
	}

	[Fact]
	public void OptionalLocIsLoadedFromConfigurables() {
		const string optionalLocDir = "configurables/localization";
		if (Directory.Exists(optionalLocDir)) {
			Directory.Delete(optionalLocDir, recursive: true);
		}
		try {
			Directory.CreateDirectory(Path.Combine(optionalLocDir, "base", "english"));
			File.WriteAllText(Path.Combine(optionalLocDir, "base", "english", "test_l_english.yml"),
				"l_english:\n test_key_1: \"BaseShouldNotWin\"\n base_key: \"BaseOnly\"\n");
			Directory.CreateDirectory(Path.Combine(optionalLocDir, "tfe", "english"));
			File.WriteAllText(Path.Combine(optionalLocDir, "tfe", "english", "test_l_english.yml"),
				"l_english:\n tfe_key: \"TFEOnly\"\n");

			// test_key_1 is already localized from the mod filesystem, so the optional loc must not overwrite it.
			var modFS = new ModFilesystem("TestFiles/CK3LocDBTests/game", new List<Mod>());
			var locDB = new CK3LocDB(modFS, ["tfe", "missing_flag"]);

			Assert.Equal("Hello", locDB.GetLocBlockForKey("test_key_1")!["english"]);
			Assert.Equal(CK3LocType.CK3ModFS, locDB.GetLocBlockForKey("test_key_1")?.GetLocTypeForLanguage("english"));
			Assert.Equal("BaseOnly", locDB.GetLocBlockForKey("base_key")!["english"]);
			Assert.Equal(CK3LocType.Optional, locDB.GetLocBlockForKey("base_key")?.GetLocTypeForLanguage("english"));
			Assert.Equal("TFEOnly", locDB.GetLocBlockForKey("tfe_key")!["english"]);
		} finally {
			if (Directory.Exists(optionalLocDir)) {
				Directory.Delete(optionalLocDir, recursive: true);
			}
		}
	}
}