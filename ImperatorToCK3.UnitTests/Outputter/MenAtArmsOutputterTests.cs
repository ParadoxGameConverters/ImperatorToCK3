using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class MenAtArmsOutputterTests {
	[Fact]
	public async Task MenAtArmsOutputIncludesHiddenEventsGeneratedTypesAndGuiStates() {
		var outputModName = $"MenAtArmsOutputterTests_{Guid.NewGuid():N}";
		var outputRoot = Path.Combine("output", outputModName);
		var modRoot = CreateTempModRoot(withHudTopGui: true);
		try {
			EnsureOutputDirectories(outputRoot);
			var characters = CreateCharactersWithMenAtArms();
			var menAtArmsTypes = CreateMenAtArmsTypes(characters, bookmarkDate: new Date(867, 1, 1));

			MenAtArmsOutputter.OutputMenAtArms(outputModName, new ModFilesystem(modRoot, Array.Empty<Mod>()), characters, menAtArmsTypes);

			var hiddenEventsText = await ReadText(Path.Combine(outputRoot, "events", "irtock3_hidden_events.txt"));
			Assert.Contains("name=IRToCK3_character_1 value=character:1", hiddenEventsText);
			Assert.Contains("name=IRToCK3_character_3 value=character:3", hiddenEventsText);
			Assert.DoesNotContain("IRToCK3_character_2", hiddenEventsText);

			var generatedTypesText = await ReadText(Path.Combine(outputRoot, "common", "men_at_arms_types", "IRToCK3_generated_types.txt"));
			Assert.Contains("IRToCK3_maa_1_base_type", generatedTypesText);
			Assert.Contains("IRToCK3_maa_3_base_type", generatedTypesText);
			Assert.DoesNotContain("\nbase_type=", $"\n{generatedTypesText}");

			var guiText = await ReadText(Path.Combine(outputRoot, "gui", "hud_top.gui"));
			Assert.Contains("name=\"IRToCK3_maa_toogle\"", guiText);
			Assert.Equal(3, CountOccurrences(guiText, "ExecuteConsoleCommand(Concatenate('add_maa "));
			Assert.Contains("add_maa IRToCK3_maa_1_base_type", guiText);
			Assert.Contains("add_maa IRToCK3_maa_3_base_type", guiText);
			Assert.Contains("effect remove_global_variable=IRToCK3_create_maa_flag", guiText);
		} finally {
			TryDeleteDir(outputRoot);
			TryDeleteDir(modRoot);
		}
	}

	[Fact]
	public async Task MissingHudTopGuiSkipsGuiOutputButStillWritesOtherFiles() {
		var outputModName = $"MenAtArmsOutputterTests_{Guid.NewGuid():N}";
		var outputRoot = Path.Combine("output", outputModName);
		var modRoot = CreateTempModRoot(withHudTopGui: false);
		try {
			EnsureOutputDirectories(outputRoot);
			var characters = CreateCharactersWithMenAtArms();
			var menAtArmsTypes = CreateMenAtArmsTypes(characters, bookmarkDate: new Date(867, 1, 1));

			MenAtArmsOutputter.OutputMenAtArms(outputModName, new ModFilesystem(modRoot, Array.Empty<Mod>()), characters, menAtArmsTypes);

			Assert.True(File.Exists(Path.Combine(outputRoot, "events", "irtock3_hidden_events.txt")));
			Assert.True(File.Exists(Path.Combine(outputRoot, "common", "men_at_arms_types", "IRToCK3_generated_types.txt")));
			Assert.False(File.Exists(Path.Combine(outputRoot, "gui", "hud_top.gui")));
		} finally {
			TryDeleteDir(outputRoot);
			TryDeleteDir(modRoot);
		}
	}

	private static CharacterCollection CreateCharactersWithMenAtArms() {
		var characters = new CharacterCollection();
		var characterWithTwoStacks = new Character("1", "One", new Date(840, 1, 1), characters);
		var characterWithoutMenAtArms = new Character("2", "Two", new Date(840, 1, 1), characters);
		var anotherCharacterWithMenAtArms = new Character("3", "Three", new Date(840, 1, 1), characters);

		characters.Add(characterWithTwoStacks);
		characters.Add(characterWithoutMenAtArms);
		characters.Add(anotherCharacterWithMenAtArms);

		characterWithTwoStacks.MenAtArmsStacksPerType["IRToCK3_maa_1_base_type"] = 2;
		anotherCharacterWithMenAtArms.MenAtArmsStacksPerType["IRToCK3_maa_3_base_type"] = 1;

		return characters;
	}

	private static IdObjectCollection<string, MenAtArmsType> CreateMenAtArmsTypes(CharacterCollection characters, Date bookmarkDate) {
		var baseType = new MenAtArmsType(
			"base_type",
			new BufferedReader(
				"""
				{
					stack = 100
					icon = light_footmen
				}
				"""
			),
			new ScriptValueCollection()
		);

		var firstDerivedType = new MenAtArmsType(baseType, characters["1"], 100, bookmarkDate);
		var secondDerivedType = new MenAtArmsType(baseType, characters["3"], 100, bookmarkDate);

		return [baseType, firstDerivedType, secondDerivedType];
	}

	private static string CreateTempModRoot(bool withHudTopGui) {
		var modRoot = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "MenAtArmsOutputterMod", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(modRoot);
		if (withHudTopGui) {
			Directory.CreateDirectory(Path.Combine(modRoot, "gui"));
			File.WriteAllText(
				Path.Combine(modRoot, "gui", "hud_top.gui"),
				"""
				gui = {
				}
				"""
			);
		}
		return modRoot;
	}

	private static void EnsureOutputDirectories(string outputRoot) {
		Directory.CreateDirectory(Path.Combine(outputRoot, "events"));
		Directory.CreateDirectory(Path.Combine(outputRoot, "common", "men_at_arms_types"));
		Directory.CreateDirectory(Path.Combine(outputRoot, "gui"));
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