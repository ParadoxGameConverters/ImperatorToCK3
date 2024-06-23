using commonItems;
using commonItems.Serialization;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Character = ImperatorToCK3.CK3.Characters.Character;

namespace ImperatorToCK3.Outputter;
public static class CharacterOutputter {
	public static async Task OutputCharacter(TextWriter output, Character character, Date conversionDate) {
		// Output ID.
		await output.WriteLineAsync($"{character.Id}={{");
		
		if (character.Dead) {
			// Don't output traits and attributes of dead characters (not needed).
			var fieldsToRemove = new[] {"traits", "employer", "diplomacy", "martial", "stewardship", "intrigue", "learning"};
			foreach (var field in fieldsToRemove) {
				character.History.Fields.Remove(field);
			}

			// Disallow random traits for dead characters.
			character.History.AddFieldValue(null, "disallow_random_traits", "disallow_random_traits", "yes");
		}

		// Add DNA to history.
		if (character.DNA is not null) {
			character.History.AddFieldValue(null, "dna", "dna", character.DNA.Id);
		}

		// Add gold to history.
		if (character.Gold is not null && character.Gold != 0) {
			var gold = (float)character.Gold.Value;
			string effectStr = gold > 0 ?
				$"{{ add_gold={gold.ToString("0.00", CultureInfo.InvariantCulture)} }}" :
				$"{{ remove_long_term_gold={(-gold).ToString("0.00", CultureInfo.InvariantCulture)} }}";
			character.History.AddFieldValue(conversionDate, "effects", "effect", effectStr);
		}

		// Output history.
		await output.WriteAsync(PDXSerializer.Serialize(character.History, "\t"));

		await OutputPregnancies(output, character);
		await OutputPrisoners(output, character, conversionDate);

		await output.WriteLineAsync("}");
	}

	private static async Task OutputPrisoners(TextWriter output, Character character, Date conversionDate) {
		if (character.PrisonerIds.Count == 0) {
			return;
		}

		await output.WriteLineAsync($"\t{conversionDate}={{");
		foreach (var (id, type) in character.PrisonerIds) {
			await output.WriteLineAsync($"\t\timprison={{target = character:{id} type={type}}}");
		}
		await output.WriteLineAsync("\t}");
	}

	/// <summary>
	/// Outputs unborn children if pregnancy has lasted at most 3 months in Imperator
	/// </summary>
	private static async Task OutputPregnancies(
		TextWriter output,
		Character character
	) {
		foreach (var pregnancy in character.Pregnancies) {
			Date conceptionDate = pregnancy.EstimatedConceptionDate;
			string fatherReference = $"character:{pregnancy.FatherId}";
			await output.WriteAsync($"\t{conceptionDate}={{ effect={{ ");
			await output.WriteAsync($"make_pregnant_no_checks={{ father={fatherReference} {(pregnancy.IsBastard ? "known_bastard=yes " : "")}}} ");
			await output.WriteLineAsync("} }");
		}
	}
}
