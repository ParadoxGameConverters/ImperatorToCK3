using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CharactersLoaderTests {
	[Fact]
	public void LoadCK3Characters_ProcessesCharactersAndFiltersInvalid() {
		var tempRoot = Path.Combine(Path.GetTempPath(), "CharactersLoaderTests", Guid.NewGuid().ToString());
		try {
			var charDir = Path.Combine(tempRoot, "history", "characters");
			Directory.CreateDirectory(charDir);

			File.WriteAllText(Path.Combine(charDir, "chars.txt"),
				"char_mother = { female = yes 900.1.1 = { birth = yes } }\n" +
				"char_father = { female = no 900.1.1 = { birth = yes } }\n" +
				"char_child = { name = \"Child\" female = yes mother = char_mother father = char_father 920.1.1 = { birth = yes death = { death_reason = death_murder_known killer = 1 } } }\n" +
				"animation_test_1 = { 1.1.1 = { birth = yes } }\n" +
				"no_birth = { female = yes }\n"
			);

			var modFS = new ModFilesystem(tempRoot, Array.Empty<Mod>());
			var characters = new CharacterCollection();
			characters.LoadCK3Characters(modFS, new Date(1000, 1, 1));

			// Characters without birth date should be ignored
			Assert.False(characters.ContainsKey("no_birth"));

			// Characters should exist
			var child = characters["char_child"];
			Assert.NotNull(child);

			// Mother/father should remain because sexes are correct
			var motherEntry = child.History.Fields["mother"].InitialEntries.Select(kvp => kvp.Value).Single();
			var fatherEntry = child.History.Fields["father"].InitialEntries.Select(kvp => kvp.Value).Single();
			Assert.Equal("char_mother", motherEntry.ToString());
			Assert.Equal("char_father", fatherEntry.ToString());

			// Birth entry should have been simplified (value becomes boolean true)
			var birthEntryValue = child.History.Fields["birth"].GetValue(new Date(920, 1, 1));
			Assert.True(birthEntryValue is bool v && v);

			// Animation test character should be killed on 2.1.1
			var animationChar = characters["animation_test_1"];
			Assert.Equal(new Date(2, 1, 1), animationChar.DeathDate);
		} finally {
			try { Directory.Delete(tempRoot, recursive: true); } catch {
				// Failure to delete the temp directory can be ignored.
			}
		}
	}
}
