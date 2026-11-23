using commonItems;
using ImperatorToCK3.Imperator.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters; 

public class CharacterNameTests {
	[Fact]
	public void NameAndCustomNameDefaultToCorrectValues() {
		var reader = new BufferedReader(string.Empty);
		var characterName = new CharacterName(reader);
		Assert.Equal(string.Empty, characterName.Name);
		Assert.Null(characterName.CustomName);
	}
	[Fact]
	public void NameAndCustomNameCanBeRead() {
		var reader = new BufferedReader("name=a custom_name=b");
		var characterName = new CharacterName(reader);
		Assert.Equal("a", characterName.Name);
		Assert.Equal("b", characterName.CustomName);
	}
	/// <summary>
	/// Mods like Reanimāta use keys with a _TEXT suffix as placeholders for characters whose names are assigned
	/// dynamically (e.g., female Romans named after their birth order among their sisters).
	/// </summary>
	[Theory]
	[InlineData("Secunda_TEXT", "Secunda")]
	[InlineData("Prima_TEXT", "Prima")]
	public void TextSuffixIsStrippedFromNames(string input, string expected) {
		var reader = new BufferedReader($"name={input}");
		var characterName = new CharacterName(reader);
		Assert.Equal(expected, characterName.Name);
	}
}