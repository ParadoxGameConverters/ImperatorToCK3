using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class CharactersOutputter {
	public static async Task OutputEverything(string outputPath, CharacterCollection characters, Date conversionDate, ModFilesystem ck3ModFS) {
		await Task.WhenAll(
			OutputCharacters(outputPath, characters, conversionDate),
			BlankOutHistoricalPortraitModifiers(ck3ModFS, outputPath)
		);
		
		Logger.IncrementProgress();
	}
	
	public static async Task OutputCharacters(string outputPath, CharacterCollection characters, Date conversionDate) {
		Logger.Info("Writing Characters...");

		// Portrait modifiers need to be outputted before characters themselves,
		// because while outputting the portrait modifiers we're adding character flags to character history.
		var charactersWithDNA = characters
			.Where(c => c.DNA is not null)
			.ToImmutableList();
		await OutputPortraitModifiers(outputPath, charactersWithDNA, conversionDate);
		
		var charactersFromIR = characters.Where(c => c.FromImperator)
			.OrderBy(c => c.Id).ToImmutableList();
		var charactersFromCK3 = characters.Except(charactersFromIR)
			.OrderBy(c => c.Id).ToImmutableList();
		
		var sb = new StringBuilder();
		var pathForCharactersFromIR = $"{outputPath}/history/characters/IRToCK3_fromImperator.txt";
		await using var charactersFromIROutput = FileOpeningHelper.OpenWriteWithRetries(pathForCharactersFromIR);
		foreach (var character in charactersFromIR) {
			CharacterOutputter.WriteCharacter(sb, character, conversionDate);
			await charactersFromIROutput.WriteAsync(sb.ToString());
			sb.Clear();
		}

		var pathForCharactersFromCK3 = $"{outputPath}/history/characters/IRToCK3_fromCK3.txt";
		await using var charactersFromCK3Output = FileOpeningHelper.OpenWriteWithRetries(pathForCharactersFromCK3, System.Text.Encoding.UTF8);
		foreach (var character in charactersFromCK3) {
			CharacterOutputter.WriteCharacter(sb, character, conversionDate);
			await charactersFromCK3Output.WriteAsync(sb.ToString());
			sb.Clear();
		}
		
		await OutputCharactersDNA(outputPath, charactersWithDNA);
	}

	public static async Task BlankOutHistoricalPortraitModifiers(ModFilesystem ck3ModFS, string outputPath) {
		Logger.Info("Blanking out historical portrait modifiers...");

		const string modifiersFilePath = "gfx/portraits/portrait_modifiers/02_all_historical_characters.txt";

		if (ck3ModFS.GetActualFileLocation(modifiersFilePath) is not null) {
			string dummyPath = Path.Combine(outputPath, modifiersFilePath);
			await using var output = FileOpeningHelper.OpenWriteWithRetries(dummyPath, System.Text.Encoding.UTF8);
			await output.WriteLineAsync("# Dummy file to blank out historical portrait modifiers from CK3.");
		}
	}

	private static async Task OutputCharactersDNA(string outputPath, IEnumerable<Character> charactersWithDNA) {
		Logger.Info("Outputting DNA...");

		// Dump all into one file.
		var path = Path.Combine(outputPath, "common/dna_data/IRToCK3_dna_data.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(path, System.Text.Encoding.UTF8);

		var sb = new StringBuilder();
		foreach (var character in charactersWithDNA) {
			var dna = character.DNA!;
			sb.AppendLine($"{dna.Id}={{");
			sb.AppendLine("\tportrait_info={");

			dna.WriteGenes(sb);

			sb.AppendLine("\t}");
			sb.AppendLine("\tenabled=yes");
			sb.AppendLine("}");

			await output.WriteAsync(sb.ToString());
			sb.Clear();
		}
	}

	private static async Task OutputPortraitModifiers(string outputPath, IReadOnlyCollection<Character> charactersWithDNA, Date conversionDate) {
		Logger.Debug("Outputting portrait modifiers...");
		// Enforce hairstyles and beards (otherwise CK3 they will only be used on bookmark screen).
		// https://ck3.paradoxwikis.com/Characters_modding#Changing_appearance_through_scripts
		var portraitModifiersOutputPath = Path.Combine(outputPath, "gfx/portraits/portrait_modifiers/IRToCK3_portrait_modifiers.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(portraitModifiersOutputPath, System.Text.Encoding.UTF8);

		await OutputPortraitModifiersForGene("hairstyles", charactersWithDNA, output, conversionDate);
		var malesWithBeards = charactersWithDNA
			.Where(c => !c.Female && c.DNA!.AccessoryDNAValues.ContainsKey("beards"))
			.ToImmutableList();
		await OutputPortraitModifiersForGene("beards", malesWithBeards, output, conversionDate);
	}

	private static async Task OutputPortraitModifiersForGene(
		string geneName,
		IReadOnlyCollection<Character> charactersWithDNA,
		TextWriter output,
		Date conversionDate
	) {
		var sb = new StringBuilder();

		var charactersByGeneValue = charactersWithDNA
			.Where(c => c.DNA!.AccessoryDNAValues.ContainsKey(geneName))
			.GroupBy(c => new {
				c.DNA!.AccessoryDNAValues[geneName].TemplateName,
				c.DNA!.AccessoryDNAValues[geneName].ObjectName,
			});
		sb.AppendLine($"IRToCK3_{geneName}_overrides = {{");
		sb.AppendLine("\tusage = game");
		sb.AppendLine("\tselection_behavior = max");
		foreach (var grouping in charactersByGeneValue) {
			var templateName = grouping.Key.TemplateName;
			var accessoryName = grouping.Key.ObjectName;

			var characterFlagName = $"portrait_modifier_{templateName}_obj_{accessoryName}";
			var characterEffectStr = $"{{ add_character_flag = {characterFlagName} }}";

			foreach (Character character in grouping) {
				Date effectDate = character.DeathDate ?? conversionDate;
				character.History.AddFieldValue(effectDate, "effects", "effect", characterEffectStr);
			}
			
			sb.AppendLine($"\t{templateName}_obj_{accessoryName} = {{");
			sb.AppendLine("\t\tdna_modifiers = {");
			sb.AppendLine("\t\t\taccessory = {");
			sb.AppendLine("\t\t\t\tmode = add");
			sb.AppendLine($"\t\t\t\tgene = {geneName}");
			sb.AppendLine($"\t\t\t\ttemplate = {templateName}");
			sb.AppendLine($"\t\t\t\taccessory = {accessoryName}");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			
			sb.AppendLine("\t\tweight = {");
			sb.AppendLine("\t\t\tbase = 0");
			sb.AppendLine("\t\t\tmodifier = {");
			sb.AppendLine("\t\t\t\tadd = 999");
			sb.AppendLine($"\t\t\t\thas_character_flag = {characterFlagName}");
			sb.AppendLine("\t\t\t}");
			
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t}");
		}
		sb.AppendLine("}");
		
		await output.WriteAsync(sb.ToString());
	}
}
