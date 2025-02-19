﻿using commonItems;
using commonItems.Serialization;
using System.Globalization;
using System.Text;
using Character = ImperatorToCK3.CK3.Characters.Character;

namespace ImperatorToCK3.Outputter;
internal static class CharacterOutputter {
	public static void WriteCharacter(StringBuilder sb, Character character, Date conversionDate, Date ck3BookmarkDate) {
		// Output ID.
		sb.AppendLine($"{character.Id}={{");
		
		if (character.DeathDate is not null && character.DeathDate <= ck3BookmarkDate) {
			// Don't output attributes of dead characters (not needed).
			var fieldsToRemove = new[] {"employer", "diplomacy", "martial", "stewardship", "intrigue", "learning"};
			foreach (var field in fieldsToRemove) {
				character.History.Fields.Remove(field);
			}
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
			Date goldDate = ck3BookmarkDate;
			var deathDate = character.DeathDate;
			if (deathDate is not null && deathDate < ck3BookmarkDate) {
				goldDate = deathDate;
			}
			character.History.AddFieldValue(goldDate, "effects", "effect", effectStr);
		}

		// Output history.
		sb.Append(PDXSerializer.Serialize(character.History, "\t"));

		WritePregnancies(sb, character);
		WritePrisoners(sb, character, conversionDate);

		sb.AppendLine("}");
	}

	private static void WritePrisoners(StringBuilder stringBuilder, Character character, Date conversionDate) {
		if (character.PrisonerIds.Count == 0) {
			return;
		}

		stringBuilder.AppendLine($"\t{conversionDate}={{");
		foreach (var (id, type) in character.PrisonerIds) {
			stringBuilder.AppendLine($"\t\timprison={{target = character:{id} type={type}}}");
		}
		stringBuilder.AppendLine("\t}");
	}

	/// <summary>
	/// Outputs unborn children if pregnancy has lasted at most 3 months in Imperator
	/// </summary>
	private static void WritePregnancies(StringBuilder stringBuilder, Character character) {
		foreach (var pregnancy in character.Pregnancies) {
			Date conceptionDate = pregnancy.EstimatedConceptionDate;
			string fatherReference = $"character:{pregnancy.FatherId}";
			stringBuilder.Append($"\t{conceptionDate}={{ effect={{ ");
			stringBuilder.Append($"make_pregnant_no_checks={{ father={fatherReference} {(pregnancy.IsBastard ? "known_bastard=yes " : "")}}} ");
			stringBuilder.AppendLine("} }");
		}
	}
}
