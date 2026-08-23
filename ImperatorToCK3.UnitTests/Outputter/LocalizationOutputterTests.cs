using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class LocalizationOutputterTests {
	[Fact]
	public void FallbackLocIsGeneratedOnlyForSecondaryLanguagesMissingPrimaryKeyLoc() {
		var ck3LocDB = new TestCK3LocDB();
		ck3LocDB.AddLocForLanguage("key1", "english", "English loc 1");
		ck3LocDB.AddLocForLanguage("key1", "french", "French loc 1");
		ck3LocDB.AddLocForLanguage("key2", "english", "English loc 2");
		ck3LocDB.AddLocForLanguage("key3", "german", "German loc 3");

		var fallbackLocByLanguage = LocalizationOutputter.GetFallbackLocLinesByLanguage(ck3LocDB);

		Assert.DoesNotContain(" key1: \"English loc 1\"", fallbackLocByLanguage["french"]);
		Assert.Contains(" key1: \"English loc 1\"", fallbackLocByLanguage["german"]);
		Assert.Contains(" key2: \"English loc 2\"", fallbackLocByLanguage["french"]);
		Assert.Contains(" key2: \"English loc 2\"", fallbackLocByLanguage["german"]);
		Assert.DoesNotContain(" key3: \"\"", fallbackLocByLanguage["french"]);
		Assert.DoesNotContain(" key3: \"\"", fallbackLocByLanguage["german"]);
	}
}