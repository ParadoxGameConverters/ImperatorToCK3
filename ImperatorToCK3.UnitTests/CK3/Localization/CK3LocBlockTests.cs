using commonItems.Localization;
using ImperatorToCK3.CK3.Localization;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Localization;

public class CK3LocBlockTests {
	private static CK3LocBlock MakeBlockWithEnglishAndFrench() {
		var block = new CK3LocBlock("test_key", "english");
		block["english"] = "Hello";
		block["french"] = "Bonjour";
		return block;
	}

	[Fact]
	public void Indexer_Get_ReturnsNullWhenMissingAndNoBaseLoc() {
		var block = new CK3LocBlock("test_key", "english");

		Assert.Null(block["french"]);
		Assert.Null(block["english"]);
	}

	[Fact]
	public void Indexer_Get_FallsBackToBaseLanguageLoc() {
		var block = new CK3LocBlock("test_key", "english");
		block["english"] = "Hello";

		Assert.Equal("Hello", block["french"]);
		Assert.Equal("Hello", block["english"]);
	}

	[Fact]
	public void Indexer_Set_NullRemovesEntry() {
		var block = MakeBlockWithEnglishAndFrench();

		block["french"] = null;

		Assert.False(block.HasLocForLanguage("french"));
		Assert.Equal("Hello", block["french"]); // falls back to base language
	}

	[Fact]
	public void Indexer_Set_StoresLocAsConverterGenerated() {
		var block = new CK3LocBlock("test_key", "english");
		block.AddModFSLoc("english", "Hello");

		block["english"] = "Hi";

		Assert.Equal("Hi", block["english"]);
		Assert.Equal(CK3LocType.ConverterGenerated, block.GetLocTypeForLanguage("english"));
	}

	[Fact]
	public void Ctor_WithLocBlock_CopiesLocsAsConverterGenerated() {
		var locBlock = new LocBlock("test_key", "english") {
			["english"] = "Hello",
			["french"] = "Bonjour"
		};

		var block = new CK3LocBlock("test_key", "english", locBlock);

		Assert.Equal("Hello", block["english"]);
		Assert.Equal("Bonjour", block["french"]);
		Assert.Equal(CK3LocType.ConverterGenerated, block.GetLocTypeForLanguage("english"));
		Assert.Equal(CK3LocType.ConverterGenerated, block.GetLocTypeForLanguage("french"));
	}

	[Fact]
	public void CopyFrom_LocBlock_CopiesLocs() {
		var block = new CK3LocBlock("test_key", "english");
		var locBlock = new LocBlock("test_key", "english") {
			["english"] = "Hello",
			["french"] = "Bonjour"
		};

		block.CopyFrom(locBlock);

		Assert.Equal("Hello", block["english"]);
		Assert.Equal("Bonjour", block["french"]);
	}

	[Fact]
	public void CopyFrom_CK3LocBlock_CopiesLocsAndResetsType() {
		var source = new CK3LocBlock("test_key", "english");
		source.AddModFSLoc("english", "Hello");
		var target = new CK3LocBlock("test_key", "english");

		target.CopyFrom(source);

		Assert.Equal("Hello", target["english"]);
		Assert.Equal(CK3LocType.ConverterGenerated, target.GetLocTypeForLanguage("english"));
	}

	[Fact]
	public void HasLocForLanguage_ReturnsFalseForMissingLanguage() {
		var block = MakeBlockWithEnglishAndFrench();

		Assert.True(block.HasLocForLanguage("english"));
		Assert.False(block.HasLocForLanguage("german"));
	}

	[Fact]
	public void ModifyForEveryLanguage_WithCK3LocBlock_AppliesFunction() {
		var block = MakeBlockWithEnglishAndFrench();
		var other = new CK3LocBlock("other_key", "english");
		other["english"] = "World";
		other["french"] = "Monde";

		block.ModifyForEveryLanguage(other, (loc, otherLoc, language) => $"{loc} {otherLoc}");

		Assert.Equal("Hello World", block["english"]);
		Assert.Equal("Bonjour Monde", block["french"]);
	}

	[Fact]
	public void ModifyForEveryLanguage_WithCK3LocBlock_NullResultRemovesEntry() {
		var block = MakeBlockWithEnglishAndFrench();
		var other = new CK3LocBlock("other_key", "english");

		block.ModifyForEveryLanguage(other, (loc, otherLoc, language) => language == "french" ? null : loc);

		Assert.Equal("Hello", block["english"]);
		Assert.False(block.HasLocForLanguage("french"));
	}

	[Fact]
	public void ModifyForEveryLanguage_WithCK3LocBlock_GeneratesMissingBaseLanguage() {
		var block = new CK3LocBlock("test_key", "english");
		block["french"] = "Bonjour";
		var other = new CK3LocBlock("other_key", "english");
		other["english"] = "World";

		block.ModifyForEveryLanguage(other, (loc, otherLoc, language) => $"{loc ?? "empty"} {otherLoc}");

		Assert.Equal("Bonjour World", block["french"]);
		Assert.Equal("empty World", block["english"]);
	}

	[Fact]
	public void ModifyForEveryLanguage_WithCK3LocBlock_DoesNotGenerateBaseLanguageWhenNull() {
		var block = new CK3LocBlock("test_key", "english");
		block["french"] = "Bonjour";
		var other = new CK3LocBlock("other_key", "english");

		block.ModifyForEveryLanguage(other, (loc, otherLoc, language) => language == "english" ? null : loc);

		Assert.Equal("Bonjour", block["french"]);
		Assert.False(block.HasLocForLanguage("english"));
	}

	[Fact]
	public void ModifyForEveryLanguage_WithLocBlock_AppliesFunction() {
		var block = MakeBlockWithEnglishAndFrench();
		var other = new LocBlock("other_key", "english") {
			["english"] = "World",
			["french"] = "Monde"
		};

		block.ModifyForEveryLanguage(other, (loc, otherLoc, language) => $"{loc} {otherLoc}");

		Assert.Equal("Hello World", block["english"]);
		Assert.Equal("Bonjour Monde", block["french"]);
	}

	[Fact]
	public void ModifyForEveryLanguage_SingleArg_AppliesFunction() {
		var block = MakeBlockWithEnglishAndFrench();

		block.ModifyForEveryLanguage((loc, language) => $"{loc}!");

		Assert.Equal("Hello!", block["english"]);
		Assert.Equal("Bonjour!", block["french"]);
	}

	[Fact]
	public void ModifyForEveryLanguage_SingleArg_NullResultRemovesEntry() {
		var block = MakeBlockWithEnglishAndFrench();

		block.ModifyForEveryLanguage((loc, language) => language == "french" ? null : loc);

		Assert.Equal("Hello", block["english"]);
		Assert.False(block.HasLocForLanguage("french"));
	}

	[Fact]
	public void ModifyForEveryLanguage_SingleArg_GeneratesMissingBaseLanguage() {
		var block = new CK3LocBlock("test_key", "english");
		block["french"] = "Bonjour";

		block.ModifyForEveryLanguage((loc, language) => $"{loc ?? "empty"}!");

		Assert.Equal("Bonjour!", block["french"]);
		Assert.Equal("empty!", block["english"]);
	}

	[Fact]
	public void ModifyForEveryLanguage_SingleArg_DoesNotGenerateBaseLanguageWhenNull() {
		var block = new CK3LocBlock("test_key", "english");
		block["french"] = "Bonjour";

		block.ModifyForEveryLanguage((loc, language) => language == "english" ? null : loc);

		Assert.Equal("Bonjour", block["french"]);
		Assert.False(block.HasLocForLanguage("english"));
	}

	[Fact]
	public void GetYmlLocLineForLanguage_EscapesQuotes() {
		var block = new CK3LocBlock("test_key", "english");
		block["english"] = "Say \"hi\"";

		Assert.Equal(" test_key: \"Say \\\"hi\\\"\"", block.GetYmlLocLineForLanguage("english"));
	}

	[Fact]
	public void GetYmlLocLineForLanguage_ReturnsEmptyValueForMissingLanguage() {
		var block = new CK3LocBlock("test_key", "english");

		Assert.Equal(" test_key: \"\"", block.GetYmlLocLineForLanguage("german"));
	}

	[Fact]
	public void GetLocTypeForLanguage_ReturnsNullForMissingLanguage() {
		var block = MakeBlockWithEnglishAndFrench();

		Assert.Null(block.GetLocTypeForLanguage("german"));
	}

	[Fact]
	public void AddModFSLoc_StoresLocWithModFSType() {
		var block = new CK3LocBlock("test_key", "english");

		block.AddModFSLoc("english", "Hello");

		Assert.Equal("Hello", block["english"]);
		Assert.Equal(CK3LocType.CK3ModFS, block.GetLocTypeForLanguage("english"));
	}

	[Fact]
	public void AddOptionalLoc_StoresLocWithOptionalType() {
		var block = new CK3LocBlock("test_key", "english");

		block.AddOptionalLoc("english", "Hello");

		Assert.Equal("Hello", block["english"]);
		Assert.Equal(CK3LocType.Optional, block.GetLocTypeForLanguage("english"));
	}

	[Fact]
	public void Probe_LocBlockNullHandling() {
		var locBlock = new LocBlock("k", "english");
		locBlock["english"] = "v";
		locBlock["french"] = null;
		int count = 0;
		string? nullLang = null;
		foreach (var (lang, loc) in locBlock) {
			count++;
			if (loc is null) {
				nullLang = lang;
			}
		}
		Assert.Equal(2, count);
		Assert.Equal("french", nullLang);
	}

	[Fact]
	public void Ctor_WithLocBlockContainingNull_SkipsNullLocs() {
		var locBlock = new LocBlock("test_key", "english") {
			["english"] = "Hello"
		};
		locBlock["french"] = null;

		var block = new CK3LocBlock("test_key", "english", locBlock);

		Assert.Equal("Hello", block["english"]);
		Assert.False(block.HasLocForLanguage("french"));
	}

	[Fact]
	public void CopyFrom_LocBlockWithNull_SkipsNullLocs() {
		var locBlock = new LocBlock("test_key", "english") {
			["english"] = "Hello"
		};
		locBlock["french"] = null;
		var block = new CK3LocBlock("test_key", "english");

		block.CopyFrom(locBlock);

		Assert.Equal("Hello", block["english"]);
		Assert.False(block.HasLocForLanguage("french"));
	}
}
