using commonItems;
using commonItems.Serialization;
using System.Globalization;
using System.IO;
using Character = ImperatorToCK3.CK3.Characters.Character;

namespace ImperatorToCK3.Outputter;
public static class CharacterOutputter {
	public static void OutputCharacter(TextWriter output, Character character, Date conversionDate) {
		// Output ID.
		output.WriteLine($"{character.Id}={{");

		// Output gold.
		if (character.Gold is not null && character.Gold != 0) {
			var gold = (float)character.Gold.Value;
			string effectStr = gold > 0 ?
				$"add_gold={gold.ToString("0.00", CultureInfo.InvariantCulture)}" :
				$"remove_long_term_gold={(-gold).ToString("0.00", CultureInfo.InvariantCulture)}";
			output.WriteLine($"\t{conversionDate}={{effect={{{effectStr}}}}}");
		}
		
		// Don't output traits and attributes of dead characters (not needed).
		if (character.Dead) {
			var fieldsToRemove = new[] {"traits", "employer", "diplomacy", "martial", "stewardship", "intrigue", "learning"};
			foreach (var field in fieldsToRemove) {
				character.History.Fields.Remove(field);
			}
			output.WriteLine("\tdisallow_random_traits=yes");
		}

		// Output history.
		output.Write(PDXSerializer.Serialize(character.History, "\t"));

		OutputPregnancies(output, character);

		OutputPrisoners(output, character, conversionDate);

		output.WriteLine("}");
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
