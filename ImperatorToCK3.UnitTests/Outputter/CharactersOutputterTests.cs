using commonItems;
using commonItems.Mods;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class CharactersOutputterTests {
	private static readonly Date ConversionDate = new(867, 1, 1);
	private static readonly Date BookmarkDate = new(900, 1, 1);

	[Fact]
	public async Task OutputCharactersSplitsHistoryFilesDeletesStaleFilesDeduplicatesDnaAndWritesPortraitModifiers() {
		var tempRoot = CreateTempDir();
		try {
			var outputPath = Path.Combine(tempRoot, "outputMod");
			EnsureOutputDirectories(outputPath);
			await File.WriteAllTextAsync(Path.Combine(outputPath, "history", "characters", "stale.txt"), "obsolete", TestContext.Current.CancellationToken);
			var ck3Root = CreateCk3PortraitModRoot(tempRoot, includeHistoricalPortraitModifiers: false);
			var characters = CreateCharactersFixture();

			await CharactersOutputter.OutputCharacters(outputPath, characters, ConversionDate, BookmarkDate, new ModFilesystem(ck3Root, Array.Empty<Mod>()));

			var fromImperatorText = await ReadText(Path.Combine(outputPath, "history", "characters", "IRToCK3_fromImperator.txt"));
			Assert.Contains("1={", fromImperatorText);
			Assert.Contains("4={", fromImperatorText);
			Assert.Contains("5={", fromImperatorText);
			Assert.DoesNotContain("2={", fromImperatorText);
			Assert.DoesNotContain("3={", fromImperatorText);
			Assert.DoesNotContain("6={", fromImperatorText);

			var fromCk3Text = await ReadText(Path.Combine(outputPath, "history", "characters", "IRToCK3_fromCK3.txt"));
			Assert.Contains("2={", fromCk3Text);
			Assert.Contains("3={", fromCk3Text);
			Assert.Contains("6={", fromCk3Text);
			Assert.DoesNotContain("1={", fromCk3Text);

			Assert.False(File.Exists(Path.Combine(outputPath, "history", "characters", "stale.txt")));

			var dnaText = await ReadText(Path.Combine(outputPath, "common", "dna_data", "IRToCK3_dna_data.txt"));
			Assert.Equal(4, CountOccurrences(dnaText, "enabled=yes"));
			Assert.Contains("shared_hair_dna={", dnaText);
			Assert.Contains("slider_hair_dna={", dnaText);
			Assert.Contains("male_beard_dna={", dnaText);
			Assert.Contains("female_beard_dna={", dnaText);

			var portraitModifiersText = await ReadText(Path.Combine(outputPath, "gfx", "portraits", "portrait_modifiers", "IRToCK3_portrait_modifiers.txt"));
			Assert.Contains("IRToCK3_hairstyles_overrides = {", portraitModifiersText);
			Assert.Contains("accessory = valid_hair", portraitModifiersText);
			Assert.Contains("type = male", portraitModifiersText);
			Assert.Contains("hair_template_obj_invalid_hair_male = {", portraitModifiersText);
			Assert.Contains("value = ", portraitModifiersText);
			Assert.DoesNotContain("accessory = invalid_hair", portraitModifiersText);
			Assert.Contains("accessory = valid_beard", portraitModifiersText);
			Assert.DoesNotContain("female_beard_should_not_output", portraitModifiersText);

			AssertEffectFlagPresent(characters["1"], BookmarkDate, "irtock3_pm_hair_template_obj_valid_hair_male");
			AssertEffectFlagPresent(characters["3"], new Date(880, 1, 1), "irtock3_pm_hair_template_obj_invalid_hair_male");
			AssertEffectFlagPresent(characters["4"], BookmarkDate, "irtock3_pm_beard_template_obj_valid_beard_male");
			Assert.Empty(characters["5"].History.Fields["effects"].DateToEntriesDict);
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task BlankOutHistoricalPortraitModifiersCreatesDummyFileOnlyWhenSourceExists() {
		var tempRoot = CreateTempDir();
		try {
			var outputPath = Path.Combine(tempRoot, "outputMod");
			EnsureOutputDirectories(outputPath);
			var ck3RootWithFile = CreateCk3PortraitModRoot(Path.Combine(tempRoot, "with_file"), includeHistoricalPortraitModifiers: true);
			var ck3RootWithoutFile = CreateCk3PortraitModRoot(Path.Combine(tempRoot, "without_file"), includeHistoricalPortraitModifiers: false);

			await CharactersOutputter.BlankOutHistoricalPortraitModifiers(new ModFilesystem(ck3RootWithFile, Array.Empty<Mod>()), outputPath);
			var dummyFilePath = Path.Combine(outputPath, "gfx", "portraits", "portrait_modifiers", "02_all_historical_characters.txt");
			Assert.True(File.Exists(dummyFilePath));
			Assert.Contains("Dummy file to blank out historical portrait modifiers from CK3.", await ReadText(dummyFilePath));

			File.Delete(dummyFilePath);
			await CharactersOutputter.BlankOutHistoricalPortraitModifiers(new ModFilesystem(ck3RootWithoutFile, Array.Empty<Mod>()), outputPath);
			Assert.False(File.Exists(dummyFilePath));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task OutputEverythingRunsCharacterOutputAndBlankingTogether() {
		var tempRoot = CreateTempDir();
		try {
			var outputPath = Path.Combine(tempRoot, "outputMod");
			EnsureOutputDirectories(outputPath);
			var ck3Root = CreateCk3PortraitModRoot(tempRoot, includeHistoricalPortraitModifiers: true);
			var characters = new CharacterCollection();
			var character = new Character("1", "Combined", new Date(850, 1, 1), characters) { FromImperator = true };
			characters.Add(character);

			await CharactersOutputter.OutputEverything(outputPath, characters, ConversionDate, BookmarkDate, new ModFilesystem(ck3Root, Array.Empty<Mod>()));

			Assert.True(File.Exists(Path.Combine(outputPath, "history", "characters", "IRToCK3_fromImperator.txt")));
			Assert.True(File.Exists(Path.Combine(outputPath, "history", "characters", "IRToCK3_fromCK3.txt")));
			Assert.True(File.Exists(Path.Combine(outputPath, "gfx", "portraits", "portrait_modifiers", "02_all_historical_characters.txt")));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	private static CharacterCollection CreateCharactersFixture() {
		var characters = new CharacterCollection();
		var sharedHairDna = CreateAccessoryOnlyDna("shared_hair_dna", ("hairstyles", "hair_template", "valid_hair", new[] { "valid_hair" }));
		var sliderHairDna = CreateAccessoryOnlyDna("slider_hair_dna", ("hairstyles", "hair_template", "invalid_hair", new[] { "unused_hair", "invalid_hair" }));
		var maleBeardDna = CreateAccessoryOnlyDna("male_beard_dna", ("beards", "beard_template", "valid_beard", new[] { "valid_beard" }));
		var femaleBeardDna = CreateAccessoryOnlyDna("female_beard_dna", ("beards", "beard_template", "female_beard_should_not_output", new[] { "female_beard_should_not_output" }));

		var imperatorCharacter = new Character("1", "ImperatorOne", new Date(840, 1, 1), characters) {
			FromImperator = true,
			DNA = sharedHairDna,
		};
		var ck3Character = new Character("2", "Ck3Two", new Date(840, 1, 1), characters) {
			DNA = sharedHairDna,
		};
		var deadSliderCharacter = new Character("3", "SliderThree", new Date(840, 1, 1), characters) {
			DNA = sliderHairDna,
		};
		deadSliderCharacter.DeathDate = new Date(880, 1, 1);
		var maleBeardCharacter = new Character("4", "BeardFour", new Date(840, 1, 1), characters) {
			FromImperator = true,
			DNA = maleBeardDna,
		};
		var femaleBeardCharacter = new Character("5", "NoBeardFive", new Date(840, 1, 1), characters) {
			FromImperator = true,
			Female = true,
			DNA = femaleBeardDna,
		};
		var noDnaCharacter = new Character("6", "NoDnaSix", new Date(840, 1, 1), characters);

		characters.Add(imperatorCharacter);
		characters.Add(ck3Character);
		characters.Add(deadSliderCharacter);
		characters.Add(maleBeardCharacter);
		characters.Add(femaleBeardCharacter);
		characters.Add(noDnaCharacter);

		return characters;
	}

	private static DNA CreateAccessoryOnlyDna(string id, params (string GeneName, string TemplateName, string ObjectName, string[] WeightOrder)[] genes) {
		var accessoryGenes = new Dictionary<string, DNAAccessoryGeneValue>();
		foreach (var (geneName, templateName, objectName, weightOrder) in genes) {
			var weightBlock = new WeightBlock();
			foreach (var weightObject in weightOrder) {
				weightBlock.AddObject(weightObject, 1);
			}
			accessoryGenes[geneName] = new DNAAccessoryGeneValue(templateName, objectName, weightBlock);
		}

		return new DNA(id, [], [], accessoryGenes);
	}

	private static void AssertEffectFlagPresent(Character character, Date expectedDate, string expectedFlag) {
		var effectsField = character.History.Fields["effects"];
		var effectEntry = Assert.Single(effectsField.DateToEntriesDict);
		Assert.Equal(expectedDate, effectEntry.Key);
		var effectValue = Assert.Single(effectEntry.Value).Value;
		var effectString = effectValue switch {
			StringOfItem stringOfItem => stringOfItem.ToString(),
			string str => str,
			_ => throw new InvalidOperationException($"Unexpected effect value type: {effectValue.GetType()}")
		};
		Assert.Contains($"add_character_flag = {expectedFlag}", effectString);
		Assert.Contains("add_character_flag = has_scripted_appearance", effectString);
	}

	private static string CreateCk3PortraitModRoot(string rootPath, bool includeHistoricalPortraitModifiers) {
		var ck3Root = Path.Combine(rootPath, "ck3");
		var accessoriesDir = Path.Combine(ck3Root, "gfx", "portraits", "accessories");
		Directory.CreateDirectory(accessoriesDir);
		File.WriteAllText(
			Path.Combine(accessoriesDir, "test_accessories.txt"),
			"""
			valid_hair = {}
			valid_beard = {}
			"""
		);

		if (includeHistoricalPortraitModifiers) {
			var portraitModifiersDir = Path.Combine(ck3Root, "gfx", "portraits", "portrait_modifiers");
			Directory.CreateDirectory(portraitModifiersDir);
			File.WriteAllText(Path.Combine(portraitModifiersDir, "02_all_historical_characters.txt"), "source file");
		}

		return ck3Root;
	}

	private static void EnsureOutputDirectories(string outputPath) {
		Directory.CreateDirectory(Path.Combine(outputPath, "history", "characters"));
		Directory.CreateDirectory(Path.Combine(outputPath, "common", "dna_data"));
		Directory.CreateDirectory(Path.Combine(outputPath, "gfx", "portraits", "portrait_modifiers"));
	}

	private static async Task<string> ReadText(string path) {
		var text = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
		return TextTestUtils.NormalizeNewlines(text);
	}

	private static int CountOccurrences(string text, string value) {
		var count = 0;
		var index = 0;
		while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0) {
			count++;
			index += value.Length;
		}
		return count;
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "CharactersOutputter", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void TryDeleteDir(string dir) {
		try {
			if (Directory.Exists(dir)) {
				Directory.Delete(dir, recursive: true);
			}
		} catch {
			// Best-effort cleanup only.
		}
	}
}