using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Outputter;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class CharacterOutputterTests {
	[Fact]
	public void PregnanciesAreOutputted() {
		Date conversionDate = "600.20.1";
		var pregnantFemale = new Character("buttocks", "Incontinentia", birthDate: "600.1.1");

		var textWriter = new StringWriter();

		CharacterOutputter.OutputCharacter(textWriter, pregnantFemale, conversionDate);
	}
}