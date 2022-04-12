using commonItems;
using ImperatorToCK3.CK3.Characters;
using System.IO;

namespace ImperatorToCK3.Outputter;
public static class CharacterOutputter {
	public static void OutputCharacter(TextWriter output, Character character, Date conversionDate) {
		// output ID, name, sex, culture, religion
		output.WriteLine($"{character.Id} = {{");
		if (!string.IsNullOrEmpty(character.Name)) {
			output.WriteLine($"\tname = \"{character.Name}\"\n");
		}
		if (character.Female) {
			output.WriteLine("\tfemale = yes");
		}
		if (!string.IsNullOrEmpty(character.Culture)) {
			output.WriteLine($"\tculture = {character.Culture}");
		}
		if (!string.IsNullOrEmpty(character.Religion)) {
			output.WriteLine($"\treligion = {character.Religion}");
		}

		// output dynasty
		if (character.DynastyId is not null) {
			output.WriteLine($"\tdynasty = {character.DynastyId}");
		}

		//output father and mother
		if (character.Father is not null) {
			output.WriteLine($"\tfather = {character.Father.Id}");
		}
		if (character.Mother is not null) {
			output.WriteLine($"\tmother = {character.Mother.Id}");
		}

		// output spouse
		// TODO: output add_spouse with earlier date if the pair has a born or unborn child
		foreach (var spouse in character.Spouses) {
			Date marriageDate;
			if (character.DeathDate is not null) {
				marriageDate = new Date(character.DeathDate);
				marriageDate.ChangeByDays(-1);
			} else {
				marriageDate = conversionDate;
			}
			output.WriteLine($"\t{marriageDate} = {{ add_spouse = {spouse.Id} }}");
		}

		// output nickname
		if (character.Nickname is not null) {
			var nicknameDate = conversionDate;
			if (character.DeathDate is not null) {
				nicknameDate = character.DeathDate;
			}
			output.WriteLine($"\t{nicknameDate} = {{ give_nickname = {character.Nickname} }}");
		}

		// output traits
		foreach (var trait in character.Traits) {
			output.WriteLine($"\ttrait = {trait}");
		}

		OutputBirthAndDeathDates(output, character);
		OutputPrisoners(output, character, conversionDate);
		OutputEmployer(output, character, conversionDate);

		output.WriteLine("}");
	}

	private static void OutputBirthAndDeathDates(TextWriter output, Character character) {
		output.WriteLine($"\t{character.BirthDate}={{birth=yes}}");

		if (character.DeathDate is null) {
			return;
		}

		output.WriteLine($"\t{character.DeathDate}={{");
		output.Write("\t\tdeath=");
		output.WriteLine(
			character.DeathReason is null ? "yes" : $"{{ death_reason={character.DeathReason} }}"
		);
		output.WriteLine("\t}");
	}

	private static void OutputPrisoners(TextWriter output, Character character, Date conversionDate) {
		if (character.PrisonerIds.Count == 0) {
			return;
		}

		output.WriteLine($"\t{conversionDate}={{");
		foreach (var (id, type) in character.PrisonerIds) {
			output.WriteLine($"\t\timprison={{target = character:{id} type={type}}}");
		}
		output.WriteLine("\t}");
	}
	private static void OutputEmployer(TextWriter output, Character character, Date conversionDate) {
		if (character.EmployerId is null) {
			return;
		}
		if (character.Dead) {
			return;
		}

		output.WriteLine($"\t{conversionDate}={{employer={character.EmployerId}}}");
	}
}
