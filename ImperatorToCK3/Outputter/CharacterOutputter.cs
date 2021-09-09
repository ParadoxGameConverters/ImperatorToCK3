using System.IO;
using ImperatorToCK3.CK3.Characters;
using commonItems;

namespace ImperatorToCK3.Outputter {
	public static class CharacterOutputter {
		public static void OutputCharacter(StreamWriter output, Character character, Date ck3BookmarkDate) {
			// output ID, name, sex, culture, religion
			output.WriteLine($"{character.ID} = {{");
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
			if (character.DynastyID is not null) {
				output.WriteLine($"\tdynasty = {character.DynastyID}");
			}

			//output father and mother
			if (character.Father is not null)
				output.WriteLine($"\tfather = {character.Father.ID}");
			if (character.Mother is not null)
				output.WriteLine($"\tmother = {character.Mother.ID}");

			// output spouse
			// TODO: output add_spouse with earlier date if the pair has a born or unborn child
			foreach (var spouseID in character.Spouses.Keys) {
				Date marriageDate;
				if (character.DeathDate is not null) {
					marriageDate = new Date(character.DeathDate.Year, character.DeathDate.Month, character.DeathDate.Day);
					marriageDate.ChangeByDays(-1);
					if (marriageDate.Day == 0) { // TODO: REMOVE THIS TEMP FIX FOR CHANGEBYDAYS ERROR
						marriageDate = new Date(marriageDate.Year, marriageDate.Month, 28);
						marriageDate.ChangeByMonths(-1);
					}
				} else {
					marriageDate = ck3BookmarkDate;
				}
				output.WriteLine($"\t{marriageDate} = {{ add_spouse = {spouseID} }}");
			}

			// output nickname
			if (character.Nickname is not null) {
				var nicknameDate = ck3BookmarkDate;
				if (character.DeathDate is not null) {
					nicknameDate = character.DeathDate;
				}
				output.WriteLine($"\t{nicknameDate} = {{ give_nickname = {character.Nickname} }}");
			}

			// output traits
			foreach (var trait in character.Traits) {
				output.WriteLine($"\ttrait = {trait}");
			}

			// output birthdate and deathdate
			output.WriteLine($"\t{character.BirthDate} = {{ birth = yes }}");
			if (character.DeathDate is not null) {
				output.WriteLine($"\t{character.DeathDate} = {{");
				output.Write("\t\tdeath = ");
				if (character.DeathReason is not null) {
					output.WriteLine($"{{ death_reason = {character.DeathReason} }}");
				} else {
					output.WriteLine("yes");
				}

				output.WriteLine("\t}");
			}

			output.WriteLine("}");
		}
	}
}
