using commonItems;
using AwesomeAssertions;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Outputter;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class CharacterOutputterTests {
	[Fact]
	public void PregnancyIsOutputted() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";
		Date childBirthDate = "600.10.7";
		Date conceptionDate = childBirthDate.ChangeByDays(-280);

		var characters = new CharacterCollection();
		var pregnantFemale = new Character("1", "Incontinentia", birthDate: "580.1.1", characters) {Female = true};
		pregnantFemale.Pregnancies.Add(new Pregnancy(fatherId:"2", motherId: "1", childBirthDate, isBastard:false));

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, pregnantFemale, conversionDate, bookmarkDate);

		var outputString = sb.ToString();
		outputString.Should().Contain("female = yes");
		outputString.Should().Contain($"{conceptionDate}={{ effect={{ make_pregnant_no_checks={{ father=character:2 }} }} }}");
	}

	[Fact]
	public void BastardPregnancyIncludesKnownBastardFlag() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";
		Date childBirthDate = "600.10.7";

		var characters = new CharacterCollection();
		var pregnantFemale = new Character("10", "BastardMother", birthDate: "580.1.1", characters) {Female = true};
		pregnantFemale.Pregnancies.Add(new Pregnancy(fatherId:"99", motherId:"10", childBirthDate, isBastard:true));

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, pregnantFemale, conversionDate, bookmarkDate);

		sb.ToString().Should().Contain("known_bastard=yes");
	}

	[Fact]
	public void DeadCharacterAttributesAreRemovedWhenDeathBeforeBookmark() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";
		Date deathDate = "590.1.1";

		var characters = new CharacterCollection();
		var dead = new Character("2", "DeadGuy", birthDate: "550.1.1", characters);
		dead.DeathDate = deathDate; // <= bookmark
		dead.History.AddFieldValue(null, "employer", "employer", "employer1");
		dead.History.AddFieldValue(null, "diplomacy", "diplomacy", 5);
		dead.History.AddFieldValue(null, "martial", "martial", 5);
		dead.History.AddFieldValue(null, "stewardship", "stewardship", 5);
		dead.History.AddFieldValue(null, "intrigue", "intrigue", 5);
		dead.History.AddFieldValue(null, "learning", "learning", 5);

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, dead, conversionDate, bookmarkDate);

		var output = sb.ToString();
		output.Should().NotContain("employer");
		output.Should().NotContain("diplomacy");
		output.Should().NotContain("martial");
		output.Should().NotContain("stewardship");
		output.Should().NotContain("intrigue");
		output.Should().NotContain("learning");
	}

	[Fact]
	public void AliveCharacterKeepsAttributes() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var alive = new Character("3", "AliveGuy", birthDate: "580.1.1", characters);
		alive.History.AddFieldValue(null, "employer", "employer", "employer1");
		alive.History.AddFieldValue(null, "diplomacy", "diplomacy", 8);

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, alive, conversionDate, bookmarkDate);

		var output = sb.ToString();
		output.Should().Contain("employer");
		output.Should().Contain("diplomacy");
	}

	[Fact]
	public void DeathAfterBookmarkDoesNotRemoveAttributes() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";
		Date deathDate = "700.1.1";

		var characters = new CharacterCollection();
		var almostDead = new Character("4", "AlmostDead", birthDate: "580.1.1", characters);
		almostDead.DeathDate = deathDate; // > bookmark
		Assert.True(almostDead.DeathDate > bookmarkDate);
		Assert.True(almostDead.DeathDate is not null && almostDead.DeathDate <= bookmarkDate == false);
		almostDead.History.AddFieldValue(null, "employer", "employer", "employer1");

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, almostDead, conversionDate, bookmarkDate);

		sb.ToString().Should().Contain("employer");
	}

	[Fact]
	public void DnaIsAddedWhenPresent() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var withDna = new Character("5", "DnaGuy", birthDate: "580.1.1", characters);
		withDna.DNA = new DNA("dna_test", [], [], []);

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, withDna, conversionDate, bookmarkDate);

		sb.ToString().Should().Contain("dna = dna_test");
	}

	[Fact]
	public void DnaIsNotAddedWhenAbsent() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var noDna = new Character("6", "NoDna", birthDate: "580.1.1", characters);

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, noDna, conversionDate, bookmarkDate);

		sb.ToString().Should().NotContain("dna =");
	}

	[Fact]
	public void PositiveGoldUsesAddGoldAtBookmarkDate() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var rich = new Character("7", "Rich", birthDate: "580.1.1", characters);
		rich.Gold = 123.456f;

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, rich, conversionDate, bookmarkDate);

		var output = sb.ToString();
		output.Should().Contain("add_gold=123.46");
		output.Should().Contain($"{bookmarkDate} = {{ effect = {{ add_gold=123.46 }} }}");
	}

	[Fact]
	public void NegativeGoldUsesRemoveGold() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var debtor = new Character("8", "Debtor", birthDate: "580.1.1", characters);
		debtor.Gold = -50f;

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, debtor, conversionDate, bookmarkDate);

		sb.ToString().Should().Contain("remove_long_term_gold=50.00");
	}

	[Fact]
	public void ZeroAndNullGoldProduceNoEffect() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var nullGold = new Character("9a", "NullGold", birthDate: "580.1.1", characters);
		nullGold.Gold = null;
		var sb1 = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb1, nullGold, conversionDate, bookmarkDate);
		sb1.ToString().Should().NotContain("add_gold");
		sb1.ToString().Should().NotContain("remove_long_term_gold");

		var zeroGold = new Character("9b", "ZeroGold", birthDate: "580.1.1", characters);
		zeroGold.Gold = 0f;
		var sb2 = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb2, zeroGold, conversionDate, bookmarkDate);
		sb2.ToString().Should().NotContain("add_gold");
		sb2.ToString().Should().NotContain("remove_long_term_gold");
	}

	[Fact]
	public void GoldUsesDeathDateWhenCharacterDiedBeforeBookmark() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "610.1.1";
		Date deathDate = "605.1.1";

		var characters = new CharacterCollection();
		var deadRich = new Character("10", "DeadRich", birthDate: "580.1.1", characters);
		deadRich.DeathDate = deathDate;
		deadRich.Gold = 10f;

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, deadRich, conversionDate, bookmarkDate);

		var output = sb.ToString();
		output.Should().Contain($"{deathDate} = {{ death = yes effect = {{ add_gold=10.00 }} }}");
		output.Should().NotContain($"{bookmarkDate} = {{ effect = {{ add_gold");
	}

	[Fact]
	public void GoldUsesBookmarkDateWhenDeathAfterBookmark() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";
		Date deathDate = "700.1.1";

		var characters = new CharacterCollection();
		var laterDead = new Character("11", "LaterDead", birthDate: "580.1.1", characters);
		laterDead.DeathDate = deathDate;
		laterDead.Gold = 10f;

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, laterDead, conversionDate, bookmarkDate);

		var output = sb.ToString();
		output.Should().Contain($"{bookmarkDate} = {{ effect = {{ add_gold=10.00 }} }}");
		output.Should().Contain($"{deathDate} = {{ death = yes }}");
	}

	[Fact]
	public void PrisonersAreOutputtedWhenPresent() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var jailor = new Character("12", "Jailor", birthDate: "580.1.1", characters);
		jailor.PrisonerIds["20"] = "dungeon";
		jailor.PrisonerIds["21"] = "house_arrest";

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, jailor, conversionDate, bookmarkDate);

		var output = sb.ToString();
		output.Should().Contain($"{conversionDate}={{");
		output.Should().Contain("imprison={target = character:20 type=dungeon}");
		output.Should().Contain("imprison={target = character:21 type=house_arrest}");
	}

	[Fact]
	public void NoPrisonersProducesNoImprisonBlock() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var noPrisoners = new Character("13", "NoJail", birthDate: "580.1.1", characters);

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, noPrisoners, conversionDate, bookmarkDate);

		sb.ToString().Should().NotContain("imprison");
	}

	[Fact]
	public void MultiplePregnanciesAreAllOutputted() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";
		Date birth1 = "600.10.7";
		Date birth2 = "601.2.1";

		var characters = new CharacterCollection();
		var mother = new Character("14", "Mother", birthDate: "580.1.1", characters) {Female = true};
		mother.Pregnancies.Add(new Pregnancy("2", "14", birth1, isBastard:false));
		mother.Pregnancies.Add(new Pregnancy("3", "14", birth2, isBastard:true));

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, mother, conversionDate, bookmarkDate);

		var output = sb.ToString();
		output.Should().Contain("father=character:2");
		output.Should().Contain("father=character:3");
		output.Should().Contain("known_bastard=yes");
	}

	[Fact]
	public void DeadCharacterWithDeathEqualToBookmarkIsConsideredDeadForRemoval() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var deadEqual = new Character("15", "DeadEqual", birthDate: "580.1.1", characters);
		deadEqual.DeathDate = bookmarkDate;
		deadEqual.History.AddFieldValue(null, "employer", "employer", "emp");

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, deadEqual, conversionDate, bookmarkDate);

		sb.ToString().Should().NotContain("employer");
	}

	[Fact]
	public void GoldWithDeathEqualToBookmarkUsesBookmarkDate() {
		Date conversionDate = "600.8.1";
		Date bookmarkDate = "600.1.1";

		var characters = new CharacterCollection();
		var deadEqualGold = new Character("16", "DeadEqualGold", birthDate: "580.1.1", characters);
		deadEqualGold.DeathDate = bookmarkDate;
		deadEqualGold.Gold = 20f;

		var sb = new StringBuilder();
		CharacterOutputter.WriteCharacter(sb, deadEqualGold, conversionDate, bookmarkDate);

		var output = sb.ToString();
		// death == bookmark => not <, so gold uses bookmark date, which is same as death date, so they are combined
		output.Should().Contain("add_gold=20.00");
		output.Should().Contain($"{bookmarkDate} = {{ death = yes effect = {{ add_gold=20.00 }} }}");
	}
}