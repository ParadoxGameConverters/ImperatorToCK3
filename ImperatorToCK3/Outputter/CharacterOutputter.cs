using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.Imperator.Characters;
using System.Collections.Immutable;
using System.IO;
using Character = ImperatorToCK3.CK3.Characters.Character;

namespace ImperatorToCK3.Outputter;
public static class CharacterOutputter {
	public static void OutputCharacter(TextWriter output, Character character, CharacterCollection imperatorCharacters, Date conversionDate) {
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

		// output nickname
		if (character.Nickname is not null) {
			var nicknameDate = conversionDate;
			if (character.DeathDate is not null) {
				nicknameDate = character.DeathDate;
			}
			output.WriteLine($"\t{nicknameDate} = {{ give_nickname = {character.Nickname} }}");
		}

		// output history
		output.Write(PDXSerializer.Serialize(character.History, "\t"));
		
		OutputUnborns(output, character, imperatorCharacters, conversionDate);

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

	/// <summary>
	/// Outputs unborn children if pregnancy has lasted at most 3 months in Imperator
	/// </summary>
	private static void OutputUnborns(
		TextWriter output,
		Character character,
		CharacterCollection imperatorCharacters,
		Date conversionDate
	) {
		foreach (var unborn in character.ImperatorCharacter?.Unborns ?? ImmutableList<Unborn>.Empty) {
			var conceptionDate = unborn.EstimatedConceptionDate;
			if (conceptionDate is null) {
				continue;
			}
			
			var pregnancyLenght = conversionDate.DiffInYears(conceptionDate);
			if (pregnancyLenght > 0.25) {
				continue;
			}

			if (unborn.FatherId is null) {
				continue;
			}

			if (!imperatorCharacters.TryGetValue((ulong)unborn.FatherId, out var imperatorFather)) {
				continue;
			}

			var ck3Father = imperatorFather.CK3Character;
			if (ck3Father is null) {
				continue;
			}


			string fatherReference = $"character:{ck3Father.Id}";
			output.WriteLine($"\t{conceptionDate}={{ effect={{ make_pregnant={{father={fatherReference} }} }} }}");
		}
	}
}
