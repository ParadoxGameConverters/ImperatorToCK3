using commonItems;
using FluentAssertions;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Outputter;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class CharacterOutputterTests {
	[Fact]
	public void PregnancyIsOutputted() {
		Date conversionDate = "600.8.1";
		Date childBirthDate = "600.10.7";
		Date conceptionDate = childBirthDate.ChangeByDays(-280);
		
		var pregnantFemale = new Character("1", "Incontinentia", birthDate: "580.1.1") {Female = true};
		pregnantFemale.Pregnancies.Add(new Pregnancy(fatherId:"2", motherId: "1", childBirthDate, isBastard:false));

		var output = new StringWriter();
		CharacterOutputter.OutputCharacter(output, pregnantFemale, conversionDate);

		var outputString = output.ToString();
		outputString.Should().Contain("female=yes");
		outputString.Should().Contain($"{conceptionDate}={{ effect={{ make_pregnant_no_checks={{ father=character:2 }} }} }}");
	}
}