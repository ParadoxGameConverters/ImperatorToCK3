using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter;

public static class CharactersOutputter {
	public static void OutputCharacters(string outputModName, CharacterCollection characters, Date conversionDate) {
		// Portrait modifiers need to be outputted before characters themselves,
		// because while outputting the portrait modifiers we're adding character flags to character history.
		var charactersWithDNA = characters
			.Where(c => c.DNA is not null)
			.ToImmutableList();
		OutputPortraitModifiers(outputModName, charactersWithDNA, conversionDate);
		
		var charactersFromIR = characters.Where(c => c.FromImperator).ToImmutableList();
		var charactersFromCK3 = characters.Except(charactersFromIR).ToImmutableList();
		
		var pathForCharactersFromIR = $"output/{outputModName}/history/characters/IRToCK3_fromImperator.txt";
		using var output = FileOpeningHelper.OpenWriteWithRetries(pathForCharactersFromIR);
		foreach (var character in charactersFromIR) {
			CharacterOutputter.OutputCharacter(output, character, conversionDate);
		}

		var pathForCharactersFromCK3 = $"output/{outputModName}/history/characters/IRToCK3_fromCK3.txt";
		using var output2 = FileOpeningHelper.OpenWriteWithRetries(pathForCharactersFromCK3, System.Text.Encoding.UTF8);
		foreach (var character in charactersFromCK3) {
			CharacterOutputter.OutputCharacter(output2, character, conversionDate);
		}
		OutputCharactersDNA(outputModName, charactersWithDNA);
	}

	private static void OutputCharactersDNA(string outputModName, IEnumerable<Character> charactersWithDNA) {
		Logger.Info("Outputting DNA...");
		// Dump all into one file.
		var path = Path.Combine("output", outputModName, "common/dna_data/IRToCK3_dna_data.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(path, System.Text.Encoding.UTF8);
		foreach (var character in charactersWithDNA) {
			var dna = character.DNA!;
			output.WriteLine($"{dna.Id}={{");
			output.WriteLine("\tportrait_info={");

			dna.OutputGenes(output);

			output.WriteLine("\t}");
			output.WriteLine("\tenabled=yes");
			output.WriteLine("}");
		}
	}

	private static void OutputPortraitModifiers(string outputModName, IReadOnlyCollection<Character> charactersWithDNA, Date conversionDate) {
		Logger.Debug("Outputting portrait modifiers...");
		// Enforce hairstyles and beards (otherwise CK3 they will only be used on bookmark screen).
		// https://ck3.paradoxwikis.com/Characters_modding#Changing_appearance_through_scripts
		var portraitModifiersOutputPath = Path.Combine("output", outputModName, "gfx/portraits/portrait_modifiers/IRToCK3_portrait_modifiers.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(portraitModifiersOutputPath, System.Text.Encoding.UTF8);

		OutputPortraitModifiersForGene("hairstyles", charactersWithDNA, output, conversionDate);
		var malesWithBeards = charactersWithDNA
			.Where(c => !c.Female && c.DNA!.AccessoryDNAValues.ContainsKey("beards"))
			.ToImmutableList();
		OutputPortraitModifiersForGene("beards", malesWithBeards, output, conversionDate);
	}

	private static void OutputPortraitModifiersForGene(
		string geneName,
		IReadOnlyCollection<Character> charactersWithDNA,
		TextWriter output,
		Date conversionDate
	) {
		var charactersByGeneValue = charactersWithDNA
			.Where(c => c.DNA!.AccessoryDNAValues.ContainsKey(geneName))
			.GroupBy(c => new {
				c.DNA!.AccessoryDNAValues[geneName].TemplateName,
				c.DNA!.AccessoryDNAValues[geneName].IntSliderValue
			});
		output.WriteLine($"IRToCK3_{geneName}_overrides = {{");
		output.WriteLine("\tusage = game");
		output.WriteLine("\tselection_behavior = max");
		foreach (var grouping in charactersByGeneValue) {
			var templateName = grouping.Key.TemplateName;
			var intSliderValue = grouping.Key.IntSliderValue;

			var characterFlagName = $"portrait_modifier_{templateName}_{intSliderValue}";
			var characterEffectStr = $"{{ add_character_flag = {characterFlagName} }}";

			foreach (Character character in grouping) {
				character.History.AddFieldValue(conversionDate, "effects", "effect", characterEffectStr);
			}
			
			output.WriteLine($"\t{templateName}_{intSliderValue} = {{");
			output.WriteLine("\t\tdna_modifiers = {");
			output.WriteLine("\t\t\taccessory = {");
			output.WriteLine("\t\t\t\tmode = add");
			output.WriteLine($"\t\t\t\tgene = {geneName}");
			output.WriteLine($"\t\t\t\ttemplate = {templateName}");
			output.WriteLine($"\t\t\t\tvalue = {(intSliderValue / 255.0):0.###}");
			output.WriteLine("\t\t\t}");
			output.WriteLine("\t\t}");
			
			output.WriteLine("\t\tweight = {");
			output.WriteLine("\t\t\tbase = 0");
			output.WriteLine("\t\t\tmodifier = {");
			output.WriteLine("\t\t\t\tadd = 999");
			output.WriteLine($"\t\t\t\thas_character_flag = {characterFlagName}");
			output.WriteLine("\t\t\t}");
			
			output.WriteLine("\t\t}");
			output.WriteLine("\t}");
		}
		output.WriteLine("}");
	}
}
