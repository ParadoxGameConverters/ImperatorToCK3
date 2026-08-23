using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests;

public class ConverterGlobalsTests {
	[Fact]
	public void SupportedLanguagesContainsPrimaryLanguageAndSecondaryLanguages() {
		var supportedLanguages = ConverterGlobals.SupportedLanguages.ToArray();
		Assert.Contains(ConverterGlobals.PrimaryLanguage, supportedLanguages);
		foreach (var secondary in ConverterGlobals.SecondaryLanguages) {
			Assert.Contains(secondary, supportedLanguages);
		}
	}
}