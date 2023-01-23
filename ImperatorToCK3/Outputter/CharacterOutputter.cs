using commonItems;
using commonItems.Serialization;
using System.Globalization;
using System.IO;
using Character = ImperatorToCK3.CK3.Characters.Character;

namespace ImperatorToCK3.Outputter;
public static class CharacterOutputter {
	public static void OutputCharacter(TextWriter output, Character character, Date conversionDate) {
		// output ID, name, sex, culture, religion
		output.WriteLine($"{character.Id}={{");
		if (!string.IsNullOrEmpty(character.Name)) {
			output.WriteLine($"\tname=\"{character.Name}\"");
		}
		if (character.Female) {
			output.WriteLine("\tfemale=yes");
		}
		if (!string.IsNullOrEmpty(character.CultureId)) {
			output.WriteLine($"\tculture={character.CultureId}");
		}
		if (!string.IsNullOrEmpty(character.FaithId)) {
			output.WriteLine($"\treligion={character.FaithId}");
		}

		// output dynasty
		if (character.DynastyId is not null) {
			output.WriteLine($"\tdynasty={character.DynastyId}");
		}

		//output father and mother
		if (character.Father is not null) {
			output.WriteLine($"\tfather={character.Father.Id}");
		}
		if (character.Mother is not null) {
			output.WriteLine($"\tmother={character.Mother.Id}");
		}

		// output nickname
		if (character.Nickname is not null) {
			var nicknameDate = conversionDate;
			if (character.DeathDate is not null) {
				nicknameDate = character.DeathDate;
			}
			output.WriteLine($"\t{nicknameDate}={{ give_nickname={character.Nickname} }}");
		}

		// output gold
		if (character.Gold is not null && character.Gold != 0) {
			var gold = (float)character.Gold.Value;
			string effectStr = gold > 0 ?
				$"add_gold={gold.ToString("0.00", CultureInfo.InvariantCulture)}" :
				$"remove_long_term_gold={(-gold).ToString("0.00", CultureInfo.InvariantCulture)}";
			output.WriteLine($"\t{conversionDate}={{effect={{{effectStr}}}}}");
		}

		// output history
		output.Write(PDXSerializer.Serialize(character.History, "\t"));

		OutputBirthAndDeathDates(output, character);
		OutputPregnancies(output, character);

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
	private static void OutputPregnancies(
		TextWriter output,
		Character character
	) {
		foreach (var pregnancy in character.Pregnancies) {
			Date conceptionDate = pregnancy.EstimatedConceptionDate;
			string fatherReference = $"character:{pregnancy.FatherId}";
			output.Write($"\t{conceptionDate}={{ effect={{ ");
			output.Write($"make_pregnant_no_checks={{ father={fatherReference} {(pregnancy.IsBastard ? "known_bastard=yes " : "")}}} ");
			output.WriteLine("} }");
		}
	}
}
