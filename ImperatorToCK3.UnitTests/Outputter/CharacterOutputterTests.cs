using commonItems;
using FluentAssertions;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Outputter;
using System.IO;
using System.Text;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class CharacterOutputterTests {
	[Fact]
	public void PregnancyIsOutputted() {
		Date conversionDate = "600.8.1";
		Date childBirthDate = "600.10.7";
		Date conceptionDate = childBirthDate.ChangeByDays(-280);

		var characters = new CharacterCollection();
		var pregnantFemale = new Character("1", "Incontinentia", birthDate: "580.1.1", characters) {Female = true};
		pregnantFemale.Pregnancies.Add(new Pregnancy(fatherId:"2", motherId: "1", childBirthDate, isBastard:false));

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, pregnantFemale, conversionDate);

		var outputString = sb.ToString();
		outputString.Should().Contain("female = yes");
		outputString.Should().Contain($"{conceptionDate}={{ effect={{ make_pregnant_no_checks={{ father=character:2 }} }} }}");
	}
}